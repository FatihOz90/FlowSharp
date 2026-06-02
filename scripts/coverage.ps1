#!/usr/bin/env pwsh
<#
.SYNOPSIS
    FlowSharp testlerini kod kapsamiyla calistirir ve gezilebilir bir HTML raporu uretir.

.DESCRIPTION
    1. Eski TestResults klasorunu temizler (eski cobertura dosyalari raporu kirletmesin).
    2. Yerel .NET araclarini (reportgenerator) geri yukler.
    3. Testleri "XPlat Code Coverage" toplayicisiyla calistirir.
    4. cobertura XML'i HTML rapora donusturur (tests/FlowSharp.Tests/CoverageReport).

.PARAMETER NoOpen
    Rapor uretildikten sonra tarayicida acmaz.

.EXAMPLE
    ./scripts/coverage.ps1
    Testleri calistirir, raporu uretir ve tarayicida acar.

.EXAMPLE
    pwsh scripts/coverage.ps1 -NoOpen
    Raporu uretir ama acmaz (CI icin uygun).
#>
[CmdletBinding()]
param(
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'

# Script'in bulundugu yere gore repo kokune gec (nereden cagrilirsa cagrilsin calissin).
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    $testProject = 'tests/FlowSharp.Tests/FlowSharp.Tests.csproj'
    $resultsDir  = 'tests/FlowSharp.Tests/TestResults'
    $reportDir   = 'tests/FlowSharp.Tests/CoverageReport'

    Write-Host '==> Eski sonuclar temizleniyor...' -ForegroundColor Cyan
    Remove-Item $resultsDir, $reportDir -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host '==> Yerel araclar geri yukleniyor (reportgenerator)...' -ForegroundColor Cyan
    dotnet tool restore

    Write-Host '==> Testler kod kapsamiyla calistiriliyor...' -ForegroundColor Cyan
    dotnet test $testProject --collect:"XPlat Code Coverage" --results-directory $resultsDir
    if ($LASTEXITCODE -ne 0) { throw "Testler basarisiz oldu (exit $LASTEXITCODE)." }

    Write-Host '==> HTML kapsama raporu uretiliyor...' -ForegroundColor Cyan
    dotnet reportgenerator `
        "-reports:$resultsDir/**/coverage.cobertura.xml" `
        "-targetdir:$reportDir" `
        "-reporttypes:Html;TextSummary"

    $summary = Join-Path $reportDir 'Summary.txt'
    if (Test-Path $summary) {
        Write-Host ''
        Get-Content $summary | Select-Object -First 20 | Write-Host
    }

    $index = Join-Path $reportDir 'index.html'
    Write-Host ''
    Write-Host "==> Rapor hazir: $index" -ForegroundColor Green

    if (-not $NoOpen -and (Test-Path $index)) {
        if ($IsWindows -or $null -eq $IsWindows) { Start-Process $index }
        elseif ($IsMacOS) { & open $index }
        else { & xdg-open $index }
    }
}
finally {
    Pop-Location
}
