#Requires -Version 7.0
# Publica la comprobacion HTTP de la API ya compilada y deja registro aunque falle.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ApiDirectory,
    [Parameter(Mandatory = $true)]
    [string] $LogDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$null = New-Item -ItemType Directory -Path $LogDirectory -Force
$logFile = Join-Path $LogDirectory 'smoke.log'

function Write-Log {
    param([Parameter(Mandatory = $true)][string] $Message)
    $line = '{0} {1}' -f (Get-Date -Format 'o'), $Message
    Add-Content -LiteralPath $logFile -Value $line -Encoding utf8
}

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

$apiRoot = (Resolve-Path -LiteralPath $ApiDirectory).Path
$dll = Get-ChildItem -LiteralPath $apiRoot -Filter 'CursoCursor.Api.dll' -File | Select-Object -First 1
if ($null -eq $dll) {
    Write-Log "No se encontro CursoCursor.Api.dll en $apiRoot."
    throw "No se encontro CursoCursor.Api.dll en $apiRoot."
}

$port = Get-FreePort
$baseUrl = "http://127.0.0.1:$port"
Write-Log "Arranque de $($dll.FullName) en $baseUrl."

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = 'dotnet'
$startInfo.WorkingDirectory = $dll.DirectoryName
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.ArgumentList.Add($dll.FullName)
$startInfo.Environment['ASPNETCORE_URLS'] = $baseUrl
$startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Production'

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$stdoutTask = $null
$stderrTask = $null
$failure = $null

try {
    if (-not $process.Start()) {
        throw 'No se pudo arrancar la API publicada.'
    }

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    $healthy = $false
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) {
            throw "La API termino antes de responder. Codigo $($process.ExitCode)."
        }

        try {
            $probe = Invoke-WebRequest -Uri "$baseUrl/health" -SkipHttpErrorCheck -TimeoutSec 2
            if ($probe.StatusCode -eq 200) {
                $healthy = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
            continue
        }

        Start-Sleep -Milliseconds 250
    }

    if (-not $healthy) {
        throw 'La API no respondio en /health dentro del tiempo de espera.'
    }

    $checks = @(
        @{ Name = 'health'; Path = '/health'; Expected = 200; BodyLike = '"ok"' },
        @{ Name = 'openapi'; Path = '/openapi/v1.json'; Expected = 404; BodyLike = $null },
        @{ Name = 'catalogo'; Path = '/api/workshops'; Expected = 200; BodyLike = '11111111-1111-1111-1111-111111111111' }
    )

    foreach ($check in $checks) {
        $response = Invoke-WebRequest -Uri ($baseUrl + $check.Path) -SkipHttpErrorCheck -TimeoutSec 10
        $bodyPath = Join-Path $LogDirectory ($check.Name + '.body.txt')
        Set-Content -LiteralPath $bodyPath -Value $response.Content -Encoding utf8
        Write-Log "$($check.Name) $($check.Path) -> $([int]$response.StatusCode)"

        if ([int]$response.StatusCode -ne [int]$check.Expected) {
            throw "Smoke $($check.Name): se esperaba $($check.Expected) y llego $($response.StatusCode)."
        }

        if ($check.BodyLike -and $response.Content -notlike "*$($check.BodyLike)*") {
            throw "Smoke $($check.Name): la respuesta no contiene $($check.BodyLike)."
        }
    }

    Write-Log 'Smoke HTTP aprobado.'
}
catch {
    $failure = $_
    Write-Log "Fallo: $($_.Exception.Message)"
}
finally {
    if (-not $process.HasExited) {
        try { $process.Kill($true) } catch { Write-Log "No se pudo cerrar la API: $($_.Exception.Message)" }
        $null = $process.WaitForExit(10000)
    }

    if ($stdoutTask) {
        Set-Content -LiteralPath (Join-Path $LogDirectory 'api-stdout.log') -Value $stdoutTask.GetAwaiter().GetResult() -Encoding utf8
    }

    if ($stderrTask) {
        Set-Content -LiteralPath (Join-Path $LogDirectory 'api-stderr.log') -Value $stderrTask.GetAwaiter().GetResult() -Encoding utf8
    }

    $process.Dispose()
}

if ($failure) {
    throw $failure
}
