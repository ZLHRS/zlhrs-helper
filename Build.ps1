param([switch]$Test,[string]$OutputDirectory="")
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$outputRoot = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { Join-Path $taskRoot 'build' } else { [IO.Path]::GetFullPath($OutputDirectory) }
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$outputExe = Join-Path $outputRoot 'NVIDIAHelper.exe'
$fx = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $fx 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Requires 64-bit Windows 10/11 and .NET Framework 4.8.' }
$refs = @('/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Net.Http.dll', '/r:System.Web.Extensions.dll', '/r:System.Security.dll')
$files = Get-ChildItem (Join-Path $taskRoot 'source') -Filter '*.cs' | ForEach-Object { $_.FullName }
$wallpapers = Get-ChildItem (Join-Path $taskRoot 'source\wallpapers') -Filter '*.png' | ForEach-Object { '/resource:' + $_.FullName + ',Wallpaper.' + $_.BaseName }
$cursors = Get-ChildItem (Join-Path $taskRoot 'source\cursors') -Filter '*.png' | ForEach-Object { '/resource:' + $_.FullName + ',Cursor.' + $_.BaseName }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /utf8output ('/win32manifest:' + (Join-Path $taskRoot 'source\app.manifest')) ('/win32icon:' + (Join-Path $taskRoot 'source\app.ico')) ('/resource:' + (Join-Path $taskRoot 'source\app.ico') + ',AppIcon') ('/out:' + $outputExe) @cursors @wallpapers @refs @files
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output 'Built NVIDIAHelper.exe'
if ($outputRoot -ne $taskRoot) { Copy-Item -LiteralPath (Join-Path $taskRoot 'NVIDIAHelper.exe.config') -Destination (Join-Path $outputRoot 'NVIDIAHelper.exe.config') -Force }
if ($Test) {
    $report = Join-Path $outputRoot 'test-report.txt'
    $process = Start-Process -FilePath $outputExe -ArgumentList @('--self-test', ('"' + $report + '"')) -WindowStyle Hidden -Wait -PassThru
    Get-Content -LiteralPath $report
    if ($process.ExitCode -ne 0) { throw 'Self-tests failed.' }
}
