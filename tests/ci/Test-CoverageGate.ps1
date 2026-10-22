#Requires -Version 5.1
# Comprueba las fronteras y errores del control sin instalar un framework adicional.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checker = Join-Path $repoRoot 'scripts/check-coverage.ps1'
$testRoot = Join-Path $repoRoot ('artifacts/coverage-gate-tests/' + [Guid]::NewGuid().ToString('N'))
$originalSummary = $env:GITHUB_STEP_SUMMARY
$env:GITHUB_STEP_SUMMARY = $null
$cases = @(
    @{ Name = 'en el umbral'; Lines = 80; Branches = 80; Pass = $true },
    @{ Name = 'por encima'; Lines = 90; Branches = 90; Pass = $true },
    @{ Name = 'lineas insuficientes'; Lines = 79; Branches = 100; Error = 'Cobertura insuficiente' },
    @{ Name = 'ramas insuficientes'; Lines = 100; Branches = 79; Error = 'Cobertura insuficiente' },
    @{ Name = 'ambas insuficientes'; Lines = 79; Branches = 79; Error = 'Cobertura insuficiente' },
    @{ Name = 'sin redondear'; Lines = 79999; Total = 100000; Branches = 100; Error = 'Cobertura insuficiente' },
    @{ Name = 'sin ramas'; Lines = 100; Branches = 0; BranchTotal = 0; Pass = $true },
    @{ Name = 'sin lineas medibles'; Lines = 0; Total = 0; Branches = 0; Error = 'Reporte invalido' },
    @{ Name = 'contadores inconsistentes'; Lines = 101; Branches = 100; Error = 'Reporte invalido' },
    @{ Name = 'ramas inconsistentes'; Lines = 100; Branches = 101; Error = 'Reporte invalido' },
    @{ Name = 'atributo ausente'; Xml = '<coverage lines-valid="100" branches-covered="80" branches-valid="100" />'; Error = 'Reporte invalido' },
    @{ Name = 'atributo no numerico'; Lines = 'NaN'; Branches = 100; Error = 'Reporte invalido' },
    @{ Name = 'reporte ausente'; Missing = $true; Error = 'exactamente un reporte' },
    @{ Name = 'copia identica del logger TRX'; Lines = 100; Branches = 100; Duplicate = $true; Pass = $true },
    @{ Name = 'reportes diferentes'; Lines = 100; Branches = 100; Duplicate = $true; Different = $true; Error = 'exactamente un reporte' }
)

try {
    for ($index = 0; $index -lt $cases.Count; $index++) {
        $case = $cases[$index]
        $directory = Join-Path $testRoot $index
        $null = New-Item -ItemType Directory -Path $directory -Force
        if (-not $case.ContainsKey('Missing')) {
            $total = if ($case.ContainsKey('Total')) { $case.Total } else { 100 }
            $branchTotal = if ($case.ContainsKey('BranchTotal')) { $case.BranchTotal } else { 100 }
            $xml = if ($case.ContainsKey('Xml')) { $case.Xml } else {
                '<coverage lines-covered="{0}" lines-valid="{1}" branches-covered="{2}" branches-valid="{3}" />' -f $case.Lines, $total, $case.Branches, $branchTotal
            }
            $xml | Set-Content -LiteralPath (Join-Path $directory 'coverage.cobertura.xml') -Encoding utf8
            if ($case.ContainsKey('Duplicate')) {
                $second = Join-Path $directory 'old-run'
                $null = New-Item -ItemType Directory -Path $second
                $secondXml = if ($case.ContainsKey('Different')) {
                    '<coverage lines-covered="50" lines-valid="100" branches-covered="50" branches-valid="100" />'
                } else { $xml }
                $secondXml | Set-Content -LiteralPath (Join-Path $second 'coverage.cobertura.xml') -Encoding utf8
            }
        }
        $failure = $null
        try { & $checker -ResultsDirectory $directory 6>$null }
        catch { $failure = $_.Exception.Message }

        if ($case.ContainsKey('Pass')) {
            if ($failure) { throw "Caso '$($case.Name)' debia aprobar: $failure" }
            if (-not (Test-Path -LiteralPath (Join-Path $directory 'coverage-summary.md'))) {
                throw "Caso '$($case.Name)' no genero el resumen."
            }
        }
        elseif (-not $failure -or $failure -notlike "*$($case.Error)*") {
            throw "Caso '$($case.Name)' debia fallar con '$($case.Error)'; resultado: $failure"
        }
        Write-Host "OK $($case.Name)"
    }
    Write-Host "Control de cobertura: $($cases.Count) casos correctos."
}
finally { $env:GITHUB_STEP_SUMMARY = $originalSummary }
