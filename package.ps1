# Thunderstore Package Builder for Riverheim Config
# Run this script to create the package zip file

$packageName = "Riverheim_Config"
# Read version from manifest to avoid manual sync
$manifestPath = Join-Path "ThunderstorePackage" "manifest.json"
if (-not (Test-Path $manifestPath)) { throw "Missing manifest.json at $manifestPath" }
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$version = $manifest.version_number
$releasesDir = "Releases"
$outputZip = Join-Path $releasesDir "$packageName-$version.zip"
$packageDir = "ThunderstorePackage"
$sourceDir = "SourceCode"

# Ensure Releases folder exists
if (-not (Test-Path $releasesDir)) {
    New-Item -ItemType Directory -Path $releasesDir | Out-Null
}

Write-Host "Building Thunderstore package: $outputZip" -ForegroundColor Cyan

# Check required files
$requiredFiles = @(
    "$packageDir\manifest.json",
    "$packageDir\README.md",
    "$packageDir\icon.png",
    "bin\Release\RiverheimConfigTest.dll"
)

$missing = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missing += $file
    }
}

if ($missing.Count -gt 0) {
    Write-Host "ERROR: Missing required files:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "  1. You have built the project (dotnet msbuild /t:Rebuild /p:Configuration=Release)" -ForegroundColor Yellow
    Write-Host "  2. You have created icon.png in ThunderstorePackage folder (256x256 PNG)" -ForegroundColor Yellow
    Write-Host "  3. Run setup_package.ps1 first to organize files" -ForegroundColor Yellow
    exit 1
}

# Create temp directory for package contents
$tempDir = "package_temp"
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy files to temp directory
Write-Host "Copying files..." -ForegroundColor Green
Copy-Item "$packageDir\manifest.json" $tempDir
Copy-Item "$packageDir\README.md" $tempDir
Copy-Item "$packageDir\CHANGELOG.md" $tempDir -ErrorAction SilentlyContinue
Copy-Item "$packageDir\icon.png" $tempDir

# Place plugin DLL under BepInEx/plugins to support auto-install by mod managers
$pluginsDir = Join-Path $tempDir "BepInEx\plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
Copy-Item "bin\Release\RiverheimConfigTest.dll" (Join-Path $pluginsDir "RiverheimConfigTest.dll")

# Include source code
Write-Host "Including source code..." -ForegroundColor Green
$sourceZipDir = "$tempDir\SourceCode"
New-Item -ItemType Directory -Path $sourceZipDir | Out-Null
Copy-Item "$sourceDir\*" $sourceZipDir -Recurse

# Create zip file
Write-Host "Creating zip file..." -ForegroundColor Green
if (Test-Path $outputZip) {
    Remove-Item $outputZip -Force
}

Compress-Archive -Path "$tempDir\*" -DestinationPath $outputZip

# Cleanup
Remove-Item $tempDir -Recurse -Force

Write-Host ""
Write-Host "SUCCESS! Package created: $outputZip" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Test the package locally" -ForegroundColor White
Write-Host "  2. Upload to https://thunderstore.io/c/valheim/create/" -ForegroundColor White
Write-Host ""
Write-Host "Package contents:" -ForegroundColor Cyan
Get-ChildItem $outputZip | ForEach-Object { 
    Write-Host "  Size: $($_.Length) bytes" -ForegroundColor White
}
