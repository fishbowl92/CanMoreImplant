param([string]$OutputFile)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputFile) { $OutputFile = Join-Path $projectRoot 'Dist\CanMoreImplant-0.3.2.zip' }
$OutputFile = [IO.Path]::GetFullPath($OutputFile)
# 원본과 기존 배포본을 덮어쓰지 않습니다. 다시 만들 때는 다른 OutputFile 이름을 지정하십시오.
if (Test-Path -LiteralPath $OutputFile) { throw "Output already exists: $OutputFile" }
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($OutputFile)) -Force | Out-Null
$relativeFiles = [Collections.Generic.List[string]]::new()
foreach ($folder in @('About','Defs','Languages','Docs')) {
    Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -File -Recurse | ForEach-Object {
        $relativeFiles.Add([IO.Path]::GetRelativePath($projectRoot, $_.FullName))
    }
}
foreach ($name in @('LoadFolders.xml','README.md','CHANGELOG.md','1.7\Assemblies\CanMoreImplant.dll')) { $relativeFiles.Add($name) }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($OutputFile, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($relative in $relativeFiles) {
        $source = Join-Path $projectRoot $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing package file: $relative" }
        $entry = 'CanMoreImplant/' + $relative.Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $source, $entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
Write-Output "Package: $OutputFile"
