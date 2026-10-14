param([string]$GamePath = 'E:\SteamLibrary\steamapps\common\RimWorld')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
# 모든 XML의 문법, 번역 키, 게임 정의 참조와 필수 배포 파일을 검사합니다.
$xmlFiles = Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter '*.xml' -File
foreach ($file in $xmlFiles) { [xml](Get-Content -LiteralPath $file.FullName -Raw) | Out-Null }
$english = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Languages\English\Keyed\CanMoreImplant.xml') -Raw)
$korean = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Languages\Korean\Keyed\CanMoreImplant.xml') -Raw)
$keys = @($english.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name)
$koreanKeys = @($korean.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name)
if (Compare-Object $keys $koreanKeys) { throw 'English/Korean translation keys differ.' }
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Source') -Filter '*.cs') {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($text, '"(CMI\.[A-Za-z]+)"')) {
        if ($match.Groups[1].Value -notin $keys) { throw ('Missing translation: ' + $match.Groups[1].Value) }
    }
}
$defs = @{}
Get-ChildItem -LiteralPath (Join-Path $GamePath 'Data\Core\Defs\HediffDefs\BodyParts') -Filter '*.xml' | ForEach-Object {
    $doc = [xml](Get-Content -LiteralPath $_.FullName -Raw)
    foreach ($node in $doc.SelectNodes('/Defs/HediffDef[defName]')) { $defs[[string]$node.defName] = $true }
}
$rules = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Defs\ImplantComparisons.xml') -Raw)
foreach ($rule in $rules.Defs.ChildNodes | Where-Object NodeType -eq Element) {
    if (-not $defs.ContainsKey([string]$rule.lower) -or -not $defs.ContainsKey([string]$rule.higher)) { throw 'Unknown HediffDef in explicit comparison rule.' }
}
foreach ($relative in @('About\About.xml','About\Preview.png','LoadFolders.xml','1.7\Assemblies\CanMoreImplant.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $relative) -PathType Leaf)) { throw "Missing: $relative" }
}
$binaries = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot '1.7\Assemblies') -Filter '*.dll')
if ($binaries.Count -ne 1 -or $binaries[0].Name -ne 'CanMoreImplant.dll') { throw 'Unexpected runtime DLL in package.' }
[xml]$about = Get-Content -LiteralPath (Join-Path $projectRoot 'About\About.xml') -Raw
if ($about.ModMetaData.packageId -ne 'canmoreimplant.healthtab') { throw 'Unexpected package ID.' }
if ((Get-Item -LiteralPath (Join-Path $projectRoot 'About\Preview.png')).Length -gt 1MB) { throw 'Preview exceeds 1 MB.' }
Write-Output "PASS: XML syntax, $($keys.Count) bilingual keys, vanilla hierarchy references, package metadata and runtime files."
