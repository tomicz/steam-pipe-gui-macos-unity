# Changelog

All notable changes to this package are documented here.

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
