# Registra cambios del agente en la API y, al cerrar el turno, ejecuta la suite.
# mark: afterFileEdit deja una marca si el archivo afecta a src/ o tests/.
# verify: stop ejecuta `dotnet test CursoCursor.slnx` solo cuando esa marca existe.
# Sin param(): Cursor envia el JSON por la tuberia y un param() lo rechazaria.
$Mode = if ($args.Count -gt 0) { [string]$args[0] } else { '' }

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

function Write-HookJson {
    param([string]$Json)
    Write-Output $Json
}

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

function Get-MarkerPath {
    $root = (Get-RepoRoot).ToLowerInvariant()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($root))
    }
    finally {
        $sha.Dispose()
    }

    $hash = [System.BitConverter]::ToString($hashBytes).Replace('-', '').Substring(0, 16).ToLowerInvariant()
    return Join-Path ([System.IO.Path]::GetTempPath()) "entegas-api-tests-$hash.pending"
}

function Test-ApiChangePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $rootFull = [System.IO.Path]::GetFullPath((Get-RepoRoot))
    $full = [System.IO.Path]::GetFullPath($Path)
    if (-not $rootFull.EndsWith('\')) {
        $rootFull = $rootFull + '\'
    }

    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    $relative = $full.Substring($rootFull.Length).Replace('\', '/')
    if ($relative -match '(^|/)(bin|obj)/') {
        return $false
    }

    if ($relative -match '^(?i)(CursoCursor\.slnx|Directory\.Build\.props|Directory\.Build\.targets|global\.json|coverage\.runsettings)$') {
        return $true
    }

    return $relative -match '^(?i)(src|tests)/.+\.(cs|csproj)$'
}

function Get-OutputTail {
    param(
        [string]$Text,
        [int]$MaxLines = 40,
        [int]$MaxChars = 3500
    )

    $lines = @($Text -split "`r?`n")
    if ($lines.Count -gt $MaxLines) {
        $lines = @($lines | Select-Object -Last $MaxLines)
    }

    $tail = ($lines -join "`n").Trim()
    if ($tail.Length -gt $MaxChars) {
        $tail = $tail.Substring($tail.Length - $MaxChars)
    }

    return $tail
}

$hookRaw = @($input) -join "`n"
if ([string]::IsNullOrWhiteSpace($hookRaw)) {
    $redirected = $false
    try {
        $redirected = [Console]::IsInputRedirected
    }
    catch {
        $redirected = $false
    }

    if ($redirected) {
        $hookRaw = [Console]::In.ReadToEnd()
    }
}

$payload = $null
if (-not [string]::IsNullOrWhiteSpace($hookRaw)) {
    try {
        $payload = $hookRaw | ConvertFrom-Json
    }
    catch {
        [Console]::Error.WriteLine('Hook de pruebas: el JSON de entrada no es valido.')
        Write-HookJson '{}'
        exit 0
    }
}

$resolvedMode = $Mode
if ([string]::IsNullOrWhiteSpace($resolvedMode) -and $null -ne $payload -and $payload.PSObject.Properties.Name -contains 'hook_event_name') {
    if ($payload.hook_event_name -eq 'afterFileEdit') {
        $resolvedMode = 'mark'
    }
    elseif ($payload.hook_event_name -eq 'stop') {
        $resolvedMode = 'verify'
    }
}

try {
    if ($resolvedMode -eq 'mark') {
        $editedPath = ''
        if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'file_path') {
            $editedPath = [string]$payload.file_path
        }

        if (Test-ApiChangePath $editedPath) {
            Set-Content -LiteralPath (Get-MarkerPath) -Value (Get-Date).ToString('o') -Encoding ascii
        }

        Write-HookJson '{}'
        exit 0
    }

    if ($resolvedMode -ne 'verify') {
        Write-HookJson '{}'
        exit 0
    }

    $status = 'completed'
    if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'status' -and -not [string]::IsNullOrWhiteSpace([string]$payload.status)) {
        $status = [string]$payload.status
    }

    $loopCount = 0
    if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'loop_count' -and $null -ne $payload.loop_count) {
        $loopCount = [int]$payload.loop_count
    }

    $marker = Get-MarkerPath
    if ($status -eq 'aborted' -or -not (Test-Path -LiteralPath $marker)) {
        Write-HookJson '{}'
        exit 0
    }

    if ($loopCount -ge 2) {
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        [Console]::Error.WriteLine('Hook de pruebas: se alcanzo el limite de reintentos y no se vuelve a lanzar la suite.')
        Write-HookJson '{}'
        exit 0
    }

    $repoRoot = Get-RepoRoot
    $solution = Join-Path $repoRoot 'CursoCursor.slnx'
    if (-not (Test-Path -LiteralPath $solution)) {
        [Console]::Error.WriteLine('Hook de pruebas: no esta CursoCursor.slnx.')
        Write-HookJson '{}'
        exit 0
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        [Console]::Error.WriteLine('Hook de pruebas: dotnet no esta en PATH.')
        Write-HookJson '{}'
        exit 0
    }

    Push-Location $repoRoot
    try {
        $testOutput = & dotnet test $solution --nologo 2>&1 | Out-String
        $testCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($testCode -eq 0) {
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        Write-HookJson '{}'
        exit 0
    }

    $tail = Get-OutputTail -Text $testOutput
    if ([string]::IsNullOrWhiteSpace($tail)) {
        $tail = '(sin salida)'
    }

    $outputLocked = $testOutput -match 'MSB3027|MSB3021|being used by another process|porque está siendo utilizado por otro proceso'
    if ($outputLocked) {
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        $followup = @"
La verificacion automatica no ejecuto la suite. ``dotnet test CursoCursor.slnx`` no pudo compilar porque el ejecutable de la API esta en uso por otro proceso.

No modifiques archivos. Informa que las pruebas no llegaron a ejecutarse y que hay que cerrar el host antes de volver a verificar.
"@
    }
    else {
        $followup = @"
La verificacion automatica de la API no paso. ``dotnet test CursoCursor.slnx`` termino con codigo $testCode.

Corrige la causa en la API o en las pruebas. No cambies una prueba para ocultar un fallo. Conserva las rutas y los campos JSON salvo que el contrato pida cambiarlos. Al terminar, deja el resultado real de esa misma suite.

Salida relevante:
$tail
"@
    }

    $json = @{ followup_message = $followup.Trim() } | ConvertTo-Json -Compress
    Write-HookJson $json
    exit 0
}
catch {
    [Console]::Error.WriteLine("Hook de pruebas: $($_.Exception.Message)")
    Write-HookJson '{}'
    exit 0
}
