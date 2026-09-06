# Changelog

All notable changes to this package are documented here.

## [1.3.0] - 2026-09-06

### Added
- Batch-mode entry points under `Tomicz.Deployer.CommandLine`: `CreateTarget`, `Build`, `PrepareUpload` and `BuildAndPrepareUpload`. A whole deployment can be set up, built and prepared for upload from a terminal, with exit code 1 on any failure.
- `Upload` and `PrepareUpload` write `upload_build_<AppId>_<BuildTarget>.sh` next to the VDF scripts. Running it uploads the prepared build from any shell.
- `steam-deploy` Claude Code skill in `Skills~/steam-deploy`, with a `unity-batch.sh` runner, so an AI agent can drive the whole flow without the Unity GUI.
- Inspector screenshots of the current target in `Documentation~/images`, referenced from the README.

### Fixed
- Building with no scenes enabled in Build Settings is reported up front instead of failing on an untitled scene.

### Changed
- Build and upload logic moved from the Inspector into a static `Deployer` class shared by the buttons and the command line.
- Every message the package logs starts with `[SteamDeployer]`.

## [1.2.0] - 2026-09-06

### Added
- **Depots** list replaces the single Depot ID. Each depot has a Depot ID and a Local Path pattern relative to the build folder, so DLC or shared-content depots can take a subfolder while the main depot takes everything. Existing targets are migrated automatically.
- **Build and Upload** button that runs both steps and stops if the build fails.
- **Development Build** toggle.
- **Append Version To Description** toggle that adds the Player Settings version to the build description.
- EditMode tests for the VDF generation under `Tests/Editor`.

### Fixed
- Upload refuses to run when no build exists for the target and tells you to generate one first.
- Double quotes in the description or branch, and slashes in the app name, are rejected before they can break the generated VDF or the build path.
- Only standalone build targets are accepted.
- Builds and uploads run after the inspector GUI pass, which avoids "Invalid GUILayout state" errors after a build.

### Changed
- The app VDF is named `app_build_<AppId>_<BuildTarget>.vdf` and each depot script `depot_build_<DepotId>.vdf`, so two targets for the same app no longer overwrite each other's scripts. Old `app_<DepotId>.vdf` and `depot_<DepotId>.vdf` files in the SDK scripts folder are no longer used and can be deleted.
- VDF text generation moved into a static `VdfGenerator` class.
- Assembly definition renamed from `Tomicz.Deployment.Editor` to `Tomicz.Deployer.Editor` to match the namespace.

## [1.1.0] - 2026-09-06

### Fixed
- The IL2CPP `<AppName>_BackUpThisFolder_ButDontShipItWithYourGame` folder is deleted only when you click **Upload**. It used to be deleted as soon as the inspector was drawn after a build, so it could never be backed up.
- The SDK folder path is saved on the deployment target asset. It used to reset on every script reload and editor restart.
- Uploading works when the Steamworks SDK path contains spaces.
- Build failures are logged as errors with the error count instead of "Target successfully built".
- Only scenes that are enabled in Build Settings are built.
- Fixed the "Depoloyement Target" typo in the Create menu.

### Added
- **Set Live Branch** field on the deployment target. Defaults to `beta`, which was previously hardcoded. Leave it empty to upload without setting the build live.
- Required fields and the SDK path are validated before building or uploading, with one error per missing value.
- The SDK path can be typed or pasted, not only browsed.
- Linux builds get the `.x86_64` extension.

### Changed
- VDF scripts are regenerated on **Upload** as well as on **Generate Build**, so a description or branch changed after the build is used.
- Terminal is opened through AppleScript instead of a hardcoded `/System/Applications/Utilities/Terminal.app` path.
- Removed leftover template comments from the generated `app_<DepotId>.vdf`.
- Removed the unused `UpdateBuildDescription` method.

## [1.0.1] - 2023-12-13

### Added
- Separate **Generate Build** and **Upload** buttons. A build can be generated without uploading it.
- Option to delete the IL2CPP do-not-ship folder before upload.

## [1.0.0] - 2023-12-06

- Initial release. Builds the selected target into the Steamworks SDK content folder, generates the app and depot VDF scripts, and runs steamcmd in Terminal.
