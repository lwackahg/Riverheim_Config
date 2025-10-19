# Setup script to organize files for Thunderstore packaging

Write-Host "Setting up Thunderstore package structure..." -ForegroundColor Cyan

$packageDir = "ThunderstorePackage"
$sourceDir = "SourceCode"

# Ensure directories exist
if (-not (Test-Path $packageDir)) {
    New-Item -ItemType Directory -Path $packageDir | Out-Null
}
if (-not (Test-Path $sourceDir)) {
    New-Item -ItemType Directory -Path $sourceDir | Out-Null
}

Write-Host ""
Write-Host "Moving Thunderstore files..." -ForegroundColor Green

# Move package files
if (Test-Path "manifest.json") {
    Move-Item "manifest.json" $packageDir -Force
    Write-Host "  Moved manifest.json" -ForegroundColor White
}
if (Test-Path "README.md") {
    Move-Item "README.md" $packageDir -Force
    Write-Host "  Moved README.md" -ForegroundColor White
}
if (Test-Path "CHANGELOG.md") {
    Move-Item "CHANGELOG.md" $packageDir -Force
    Write-Host "  Moved CHANGELOG.md" -ForegroundColor White
}
if (Test-Path "icon_instructions.txt") {
    $iconDest = Join-Path $packageDir "icon_instructions.txt"
    Copy-Item "icon_instructions.txt" $iconDest -Force
    Write-Host "  Copied icon_instructions.txt" -ForegroundColor White
}

Write-Host ""
Write-Host "Copying source files..." -ForegroundColor Green

# Copy source files
if (Test-Path "RiverheimConfigTest.cs") {
    Copy-Item "RiverheimConfigTest.cs" $sourceDir -Force
    Write-Host "  Copied RiverheimConfigTest.cs" -ForegroundColor White
}
if (Test-Path "RiverheimConfigTest.csproj") {
    Copy-Item "RiverheimConfigTest.csproj" $sourceDir -Force
    Write-Host "  Copied RiverheimConfigTest.csproj" -ForegroundColor White
}
if (Test-Path "ILRepack.targets") {
    Copy-Item "ILRepack.targets" $sourceDir -Force
    Write-Host "  Copied ILRepack.targets" -ForegroundColor White
}
if (Test-Path "ServerSync.dll") {
    Copy-Item "ServerSync.dll" $sourceDir -Force
    Write-Host "  Copied ServerSync.dll" -ForegroundColor White
}

# Create source README
$readmePath = Join-Path $sourceDir "README.md"
$readmeContent = @"
# Riverheim Config - Source Code

Complete source code for the Riverheim Config mod.

## Build Command
``````
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
``````

Output: bin\Release\RiverheimConfigTest.dll

## Files
- RiverheimConfigTest.cs - Main plugin code
- RiverheimConfigTest.csproj - Project file
- ILRepack.targets - Merges ServerSync into output DLL
- ServerSync.dll - Config synchronization library
"@

Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "  Created source README.md" -ForegroundColor White

Write-Host ""
Write-Host "SUCCESS!" -ForegroundColor Green
Write-Host ""
Write-Host "Structure created:" -ForegroundColor Cyan
Write-Host "  ThunderstorePackage/ - Package files" -ForegroundColor White
Write-Host "  SourceCode/ - Source code" -ForegroundColor White
Write-Host ""
Write-Host "Next: Create icon.png (256x256) in ThunderstorePackage folder" -ForegroundColor Yellow
Write-Host ""
