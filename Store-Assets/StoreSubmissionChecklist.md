# Store Submission Checklist

- Confirm Partner Center reservation is exactly **Simple Folder Compare**.
- Confirm package identity matches:
  - Name: `m3Coding.SimpleFolderCompare`
  - Publisher: `CN=AFF85DD5-3D92-42A5-BA39-3AF6D41B1837`
  - Publisher display name: `m3 Coding`
  - PFN: `m3Coding.SimpleFolderCompare_8srffngrg4x08`
  - Store ID: `9PLJSQS6NXWD`
- Generate final app icon and Store artwork.
- Capture screenshots from the running app.
- Ensure `Package.appxmanifest` is present at repo root and uses the values above.
- Run `pwsh .\\Store-Assets\\package-msix.ps1` to create `dist/msix/` artifacts.
- Open `Store-Assets\\StoreDeployment.html` for a submission-ready deployment handoff.
- Build and verify the MSIX or MSIXUPLOAD package.
- Upload package, screenshots, listing text, and privacy policy in Partner Center.
- Confirm age rating and additional testing notes.
