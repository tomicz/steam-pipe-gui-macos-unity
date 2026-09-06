---
name: steam-deploy
description: Build a Unity game and upload it to Steam from the terminal on macOS with the SteamPipeGUI for macOS package, without opening the Unity editor. Use when asked to build for Steam, upload or publish a build to Steam, create or edit a Steam deployment target, or set a build live on a Steam branch.
---

# Steam deploy without the Unity GUI

The package `com.tomicz.unity-steam-macos-deployer` adds batch-mode entry points under `Tomicz.Deployer.CommandLine`. Everything below runs from the shell. `scripts/unity-batch.sh`, next to this file, launches the right Unity version for the project, keeps the full log under `Logs/`, and prints only the `[SteamDeployer]` lines, compiler errors and the exit code.

## Rules

- Never type, read or store the user's Steam password or Steam Guard code. steamcmd caches the login after the user has logged in once themselves (step 4).
- Unity batch mode cannot open a project that is already open in the Unity editor. The script refuses to run in that case. Ask the user to close the editor; do not kill it.
- The upload sets the build live on the target's branch. Before running the upload script, tell the user the app ID, depots and branch it will affect and wait for their go-ahead.
- Every Unity launch takes 30 seconds to a few minutes. Prefer `BuildAndPrepareUpload` over separate `Build` and `PrepareUpload` runs.

## 1. Locate the project

Run from the Unity project root, the folder with `Assets/` and `ProjectSettings/`. Confirm the package is installed:

    grep -n "com.tomicz.unity-steam-macos-deployer" Packages/manifest.json

`ProjectSettings/ProjectVersion.txt` names the Unity version. The script expects it under `/Applications/Unity/Hub/Editor/<version>/`; set `UNITY_PATH` to override.

## 2. Find or create a deployment target

Targets are `.asset` files. List them and read one; they are plain YAML:

    grep -rl "_sdkPath:" Assets --include='*.asset'
    cat Assets/Deployment/MacOS.asset

Create a target, or change fields on an existing one. Only the arguments you pass are changed:

    scripts/unity-batch.sh . -executeMethod Tomicz.Deployer.CommandLine.CreateTarget \
      -deploymentTarget Assets/Deployment/MacOS.asset \
      -buildTarget StandaloneOSX -appName MyGame -description "Release candidate" -appendVersion true \
      -steamUsername USERNAME -appId 1000 -depot 1001 \
      -setLiveBranch beta -sdkPath /path/to/steamworks_sdk

Arguments: `-buildTarget` (StandaloneOSX, StandaloneWindows, StandaloneWindows64, StandaloneLinux64), `-appName`, `-description`, `-appendVersion true|false`, `-developmentBuild true|false`, `-steamUsername`, `-appId`, `-depot ID` or `-depot ID:LocalPath` (repeat for more depots), `-setLiveBranch` (pass `""` to upload without setting live), `-deleteDoNotShipFolder true|false`, `-sdkPath`.

Ask the user for the App ID, Depot ID, Steam username and SDK path when no existing target has them. The output ends with "Target is complete and ready to build" or lists what is missing.

## 3. Build

    scripts/unity-batch.sh . -executeMethod Tomicz.Deployer.CommandLine.Build -deploymentTarget Assets/Deployment/MacOS.asset

Success prints `Build succeeded: <path>`. The player lands in `<sdk>/tools/ContentBuilder/content/<BuildTarget>/`.

## 4. Prepare and run the upload

    scripts/unity-batch.sh . -executeMethod Tomicz.Deployer.CommandLine.PrepareUpload -deploymentTarget Assets/Deployment/MacOS.asset

Or build and prepare in one Unity session with `Tomicz.Deployer.CommandLine.BuildAndPrepareUpload`.

This checks that the build exists, rewrites the VDF scripts, deletes the IL2CPP do-not-ship folder and writes `<sdk>/tools/ContentBuilder/scripts/upload_build_<AppId>_<BuildTarget>.sh`. The output names the file.

First upload on this machine: the user must log in once themselves so steamcmd caches the credentials. Give them this command to run in their own terminal and wait until they confirm it succeeded:

    '<sdk>/tools/ContentBuilder/builder_osx/steamcmd.sh' +login USERNAME +quit

Then, after the user's go-ahead, run the upload script and watch its output:

    sh '<sdk>/tools/ContentBuilder/scripts/upload_build_<AppId>_<BuildTarget>.sh'

Success ends with `Successfully finished AppID <id> build (BuildID <id>)`. If it asks for a password or a Steam Guard code, stop: the cached login is missing or expired, so repeat the login step.

## 5. Verify

Tell the user to check Steamworks > SteamPipe > Builds for the new build ID and description. If `Set Live Branch` was set, the build is already live on that branch.

## Troubleshooting

| Output | Meaning |
|---|---|
| `Unity <version> not found` | Install that version with Unity Hub, or set `UNITY_PATH`. |
| `open in the Unity editor` | Ask the user to close the project in Unity. |
| `error CS...` | The project does not compile. Fix the errors first; batch mode cannot run methods otherwise. |
| `is not a standalone platform`, `No build found`, `SDK folder path is not set` | Fix the target with `CreateTarget` and the matching argument. |
| `Build target ... not supported`, `No build module` | The platform support module for that target is not installed in Unity Hub. |
| steamcmd `Login Failure` or `Invalid Password` | Cached login missing. Repeat the user login step. |
| steamcmd `Failed to load depot config` | The depot ID is wrong or belongs to another app. |
