#Requires -Version 5.1
# Exige al menos 80% de lineas y de ramas, sin redondear hacia arriba.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ResultsDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$MinimumPercent = 80L

function Get-RequiredCount {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlElement] $Node,
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $raw = $Node.GetAttribute($Name)
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Reporte invalido: falta el atributo $Name."
    }

    $parsed = [long]0
    $style = [System.Globalization.NumberStyles]::Integer
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    if (-not [long]::TryParse($raw, $style, $culture, [ref]$parsed) -or $parsed -lt 0) {
        throw "Reporte invalido: el atributo $Name no es un entero valido."
    }

    return $parsed
}

function Test-MeetsThreshold {
    param(
        [Parameter(Mandatory = $true)]
        [long] $Covered,
        [Parameter(Mandatory = $true)]
        [long] $Valid
    )

    return ($Covered * 100L) -ge ($Valid * $MinimumPercent)
}

if (-not (Test-Path -LiteralPath $ResultsDirectory -PathType Container)) {
    throw "Se esperaba exactamente un reporte de cobertura."
}

$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter 'coverage.cobertura.xml' -File)
if ($reports.Count -eq 0) {
    throw "Se esperaba exactamente un reporte de cobertura."
}

$distinct = @($reports | Group-Object -Property { (Get-Content -LiteralPath $_.FullName -Raw).Trim() })
if ($distinct.Count -ne 1) {
    throw "Se esperaba exactamente un reporte de cobertura y llegaron $($distinct.Count)."
}

$reportPath = @($distinct[0].Group)[0].FullName
$document = New-Object System.Xml.XmlDocument
try {
    $document.Load($reportPath)
}
catch {
    throw "Reporte invalido: no se pudo leer el XML."
}

$coverage = $document.DocumentElement
if ($null -eq $coverage) {
    throw "Reporte invalido: el XML no tiene raiz."
}

$linesCovered = Get-RequiredCount -Node $coverage -Name 'lines-covered'
$linesValid = Get-RequiredCount -Node $coverage -Name 'lines-valid'
$branchesCovered = Get-RequiredCount -Node $coverage -Name 'branches-covered'
$branchesValid = Get-RequiredCount -Node $coverage -Name 'branches-valid'

if ($linesValid -eq 0 -or $linesCovered -gt $linesValid -or $branchesCovered -gt $branchesValid) {
    throw "Reporte invalido: los contadores de lineas o ramas no son coherentes."
}

$linesPass = Test-MeetsThreshold -Covered $linesCovered -Valid $linesValid
$branchesPass = $branchesValid -eq 0 -or (Test-MeetsThreshold -Covered $branchesCovered -Valid $branchesValid)
if (-not $linesPass -or -not $branchesPass) {
    throw "Cobertura insuficiente: lineas $linesCovered/$linesValid, ramas $branchesCovered/$branchesValid. El minimo es $MinimumPercent% sin redondear."
}

$linePercent = [decimal]$linesCovered * 100 / [decimal]$linesValid
$branchPercent = if ($branchesValid -eq 0) { [decimal]100 } else { [decimal]$branchesCovered * 100 / [decimal]$branchesValid }
$summary = @"
# Cobertura

- Lineas: $linesCovered / $linesValid ($linePercent%)
- Ramas: $branchesCovered / $branchesValid ($branchPercent%)
- Minimo: $MinimumPercent% en lineas y en ramas
"@

Set-Content -LiteralPath (Join-Path $ResultsDirectory 'coverage-summary.md') -Value $summary -Encoding utf8
if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary -Encoding utf8
}

Write-Host "Cobertura aceptada: lineas $linePercent%, ramas $branchPercent%."
