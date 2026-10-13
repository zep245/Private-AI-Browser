# Build Private AI for Windows and create the installer

The repository includes a GitHub Actions workflow at `.github/workflows/windows-installer.yml` that publishes the Windows x64 application and compiles `PrivateAI.iss` using Inno Setup on a Windows runner.

## Run the Windows build

1. Push this repository to GitHub.
2. Open **Actions** and select **Build Windows app and Inno Setup installer**.
3. Select **Run workflow** (or push a change under `PrivateAI.Windows/`).
4. Open the completed run and download these artifacts:
   - `PrivateAI-Setup` — the Inno Setup installer (`PrivateAI-Setup.exe`).
   - `PrivateAI-Windows-x64` — the published app files, useful for troubleshooting.

The build uses the .NET 10 SDK and produces a self-contained `win-x64` publish. WebView2 Runtime may still need to be installed on a target PC if it is not already present; the installer does not bundle Microsoft's WebView2 Runtime.

## Cursor change

The app injects a small script into each WebView2 page. It sets the cursor to the standard arrow on controls whose accessible label/title identifies dictation or microphone input. Website markup can change, so verify the microphone hover on ChatGPT after building. This changes only the cursor appearance; it does not disable microphone functionality.
