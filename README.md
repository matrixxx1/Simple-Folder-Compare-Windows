# Simple Folder Compare for Windows

Compare two folders and spot changed files.

This is a local WPF desktop utility for fast folder diff workflows. It supports rich comparison and small sync actions right in a single window.

## Features

- Side-by-side folder selection
- Recursive scan toggle
- Size and optional SHA-256 content comparison
- Status filtering and text search
- Missing / added / modified / unchanged reporting
- One-click copy selected files from one side to the other
- CSV export of current filtered results
- Startup-safe activity log and summary panel
- App icon and starter Store-Assets workflow

## Build

```powershell
dotnet build .\SimpleFolderCompare\SimpleFolderCompare.csproj -c Release
```

## Test / verify

```powershell
Start-Process .\SimpleFolderCompare\bin\Release\net8.0-windows\SimpleFolderCompare.exe
```

The app is now wired with `StartupUri` so it opens immediately and stays running.

## Store notes

For publishing, reserve the exact Microsoft Store product name in Partner Center and update package identity values to match that reservation.

## Store deployment prep

- Reserved app name: **Simple Folder Compare**
- Store product ID: 9PLJSQS6NXWD`r
- Package identity: m3Coding.SimpleFolderCompare`r
- Publisher: CN=AFF85DD5-3D92-42A5-BA39-3AF6D41B1837`r
- PFN: m3Coding.SimpleFolderCompare_8srffngrg4x08`r

Package.appxmanifest has been added at the repo root with these identity values and is ready for your packaging project.
## Store deploy command

Use this single command to create the full Store handoff artifacts:

```powershell
pwsh -File .\Store-Assets\package-msix.ps1
```

Result files are created in:

- dist/msix/SimpleFolderCompare_0.2.0.0_x64.msix
- dist/msix/SimpleFolderCompare.msixupload

Open the deployment handoff:

- Store-Assets\StoreDeployment.html
