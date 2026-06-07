param(
    [string]$Configuration = "Release",
    [string]$BuildOutput = "bin\Release\net8.0-windows",
    [string]$Platform = "x64",
    [string]$OutputRoot = "dist",
    [string]$Publisher = "CN=AFF85DD5-3D92-42A5-BA39-3AF6D41B1837",
    [string]$PackageName = "m3Coding.SimpleFolderCompare",
    [string]$Version = "0.2.0.0",
    [string]$ManifestPath = "Package.appxmanifest",
    [string]$SignCertificateThumbprint = "",
    [string]$SignTimestamp = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "SimpleFolderCompare\SimpleFolderCompare.csproj"
$projectDir = Split-Path $projectPath -Parent
$storeAssets = Join-Path $repoRoot "Store-Assets"
$outputDir = Join-Path $repoRoot $OutputRoot
$publishDir = Join-Path $projectDir $BuildOutput
$packageDir = Join-Path $outputDir "msix"
$stagingRoot = Join-Path $packageDir "SimpleFolderCompare"
$assetsStaging = Join-Path $stagingRoot "Store-Assets"
$manifestOut = Join-Path $stagingRoot "Package.appxmanifest"

Write-Host "Building app..."
dotnet publish $projectPath -c $Configuration -p:Platform=$Platform -p:PublishDir="$publishDir\"

New-Item -ItemType Directory -Path $packageDir, $stagingRoot, $assetsStaging -Force | Out-Null

Copy-Item -Path (Join-Path $publishDir "*") -Destination $stagingRoot -Recurse -Force
Copy-Item -Path $ManifestPath -Destination $manifestOut -Force
Copy-Item -Path (Join-Path $storeAssets "*") -Destination $assetsStaging -Recurse -Force

function Resolve-ToolPath {
    param([string]$ToolName)
    $tool = Get-Command "$ToolName.exe" -ErrorAction SilentlyContinue
    if ($tool) { return $tool.Source }
    $sdkBase = Join-Path $env:ProgramFiles "Windows Kits\10\bin"
    $toolFile = Get-ChildItem -Path $sdkBase -Filter "$ToolName.exe" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object FullName |
        Select-Object -First 1
    if ($toolFile) { return $toolFile.FullName }
    return $null
}

$makeAppxPath = Resolve-ToolPath "makeappx"
if (-not $makeAppxPath) { throw "makeappx.exe not found. Install Windows 10/11 SDK and retry." }

$msixName = "${PackageName}_${Version}_$Platform.msix"
$msixPath = Join-Path $packageDir $msixName

$signtoolPath = Resolve-ToolPath "signtool"

& $makeAppxPath "pack" "/d" $stagingRoot "/p" $msixPath /l

if ($SignCertificateThumbprint) {
    if (-not $SignTimestamp) {
        $SignTimestamp = "http://timestamp.digicert.com"
    }

    if (-not $signtoolPath) {
        Write-Warning "signtool.exe not found. Package generated unsigned."
    }
    else {
        $args = @(
            "sign", "/a",
            "/sha1", $SignCertificateThumbprint,
            "/fd", "sha256",
            "/tr", $SignTimestamp,
            "/td", "sha256",
            $msixPath
        )
        $args = $args | Where-Object { $_ -ne "" }
        & $signtoolPath @args
    }
}

Write-Host "MSIX created: $msixPath"

$uploadPackage = Join-Path $packageDir "SimpleFolderCompare.msixupload"
if (Test-Path $uploadPackage) { Remove-Item $uploadPackage -Force }
Compress-Archive -Path $msixPath -DestinationPath $uploadPackage
Write-Host "Upload package prepared: $uploadPackage"
Write-Host "Pair this with StoreListing.md, PrivacyPolicy.txt, and screenshots in Partner Center."
