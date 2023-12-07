# Unity Steam to MacOS Deployer

Simplify the process of bringing your game to Steam for macOS with our user-friendly deployer tool. This tool streamlines the upload process using the Steamworks SDK, ensuring a seamless experience for macOS users while maintaining consistency across different platforms. Easily publish your game on Steam hassle-free!

## Overview

While Windows users have the luxury of SteamPipeGUI for deploying builds effortlessly, macOS users face the challenge of using Steam commands in their Command Line Interface (CLI). Steam's documentation can be unclear for first-timers. To address this, I created this repository, providing a tool to publish new builds on Steam directly from your Unity Editor on macOS. The best part? It reduces the publishing time from 30 minutes to under 1 minute.

## Getting Started with Steamworks

Follow these steps to set up your game on Steam:

1. **Create a Steamworks Account:**
    - Visit the Steamworks website and follow the registration process to set up your developer account.

2. **Access the Steamworks Dashboard:**
    - Log in and navigate to the Steamworks dashboard.

3. **Create Your Application:**
    - Initiate the process of creating a new application for your game within the Steamworks dashboard.

4. **Obtain Your App ID:**
    - Steam will assign a unique App ID to your game during the application creation process.

## Configuring Launch Options

To configure launch options:

1. Go to Steamworks Dashboard.
2. Click on Dashboard and select your app.
3. Go to **Edit Steamworks Settings** > Installation > General Installation.
4. Add your Launch Options for each platform.

Example
![Launch Options](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/cfe16859-8175-46be-9071-7a45aad71d09)

## Installing Depots

Depots organize game content into categories. Create depots for each platform:

1. Go to Steamworks dashboard.
2. Select your app and edit Steamworks settings.
3. Select SteamPipe > Depots and click Add new Depot.

![Depots](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/8dc3edb3-9076-4b94-be92-494a16be2f0a)

## Installing Deployer in Unity

1. Open your Unity project.
2. Open Package Manager (**Window > Package Manager**).
3. Click on **+** and choose **Add package from Git url**.
4. Paste [git@github.com:tomicz/unity-steam-macos-deployer.git](git@github.com:tomicz/unity-steam-macos-deployer.git) and click **Add**.

## Deploying Builds from Unity Editor

1. Right-click inside your Unity Project tab and go to Create > Tomicz > Steam > Deployment Target.
2. Create targets for each platform, e.g., DeploymentTargetMacOS, DeploymentTargetWindows.
3. Enter your game name (must match Launch Options).
4. Enter description, Steam username, app ID, and depot ID.
5. Select your Steam SDK path.

### Examples
#### MacOS
![MacOS Target](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/104edc81-dc88-4637-af3c-331cfdc30f7b)

#### Windows
![Windows Target](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/7f6f939a-1822-4662-9979-c87bd57bd01a)

6. Click **Build Target** to build and upload your game. Follow the on-screen prompts.

After the upload is complete, go to your app in the Steamworks dashboard and click SteamPipe > Builds to see your newly uploaded builds. Keep a custom description for each build for easy identification, such as "Target: StandaloneOSX 0.0.x" or "Target: StandaloneWindows 0.0.x."
