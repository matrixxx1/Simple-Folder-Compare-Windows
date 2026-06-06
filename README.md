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
