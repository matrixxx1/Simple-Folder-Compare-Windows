# Simple Folder Compare for Windows

Compare two folders and spot changed files.

This is the initial Microsoft Store-oriented Windows desktop app scaffold for $(System.Collections.Hashtable.Title). It uses .NET 8 and WPF, keeps the first implementation local-first, and includes a repo-root Store-Assets folder for listing and privacy handoff material.

## Initial scope

- Side-by-side folder setup
- Missing and changed file summary
- Comparison notes
- Local-only review

## Build

``powershell
dotnet build .\SimpleFolderCompare\SimpleFolderCompare.csproj -c Release
``

## Store notes

Before final packaging, reserve the exact Microsoft Store product name in Partner Center and update package identity values to match that reservation.