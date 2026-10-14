param(
    [string]$GamePath = 'E:\SteamLibrary\steamapps\common\RimWorld',
    [string]$HarmonyPath = 'E:\SteamLibrary\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll',
    [switch]$Test
)
$ErrorActionPreference = 'Stop'
# 게임과 Harmony는 설치된 파일을 참조할 뿐 배포 폴더에 복사하지 않습니다.
if (-not (Test-Path -LiteralPath (Join-Path $GamePath 'Assembly-CSharp.dll'))) { throw 'Game assembly not found.' }
if (-not (Test-Path -LiteralPath $HarmonyPath)) { throw 'Harmony assembly not found.' }
$project = if ($Test) { 'Tests\CanMoreImplant.Tests.csproj' } else { 'Source\CanMoreImplant.csproj' }
& dotnet build (Join-Path $PSScriptRoot $project) -c Release --configfile (Join-Path $PSScriptRoot 'NuGet.Config') "-p:GamePath=$GamePath" "-p:HarmonyPath=$HarmonyPath"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
if ($Test) {
    & dotnet (Join-Path $PSScriptRoot 'Tests\bin\Release\net10.0\CanMoreImplant.Tests.dll') $GamePath $HarmonyPath
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
