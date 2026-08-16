# Publishes a portable self-contained-ish folder (framework-dependent) for Windows x64.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "dist\CSGOConfigManager"
$project = Join-Path $root "src\CSGOConfigManager\CSGOConfigManager.csproj"

Write-Host "Publishing to $out ..."
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

dotnet publish $project -c Release -r win-x64 --self-contained false -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Ensure runtime folders exist
New-Item -ItemType Directory -Force -Path (Join-Path $out "Config"), (Join-Path $out "Backups"), (Join-Path $out "Logs") | Out-Null

Write-Host ""
Write-Host "Done. Portable app folder:"
Write-Host "  $out\CSGOConfigManager.exe"
Write-Host "Copy the entire 'CSGOConfigManager' folder to run on another PC (requires .NET 8 Desktop Runtime)."
