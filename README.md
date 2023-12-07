# Unity Steam to MacOS Deployer

Simplify the process of getting your game on Steam for macOS with our easy-to-use deployer tool. This tool makes it simple to upload your game using the Steamworks SDK, creating a smooth experience for macOS users and ensuring consistency across different platforms. Easily publish your game on Steam!

While Windows users have SteamPipeGUI, a user-friendly version, to deploy builds flawlessly, macOS users face a bit of a challenge. They have to use Steam commands in their Command Line Interface (CLI) for deployment, and Steam's documentation isn't very clear for first-timers. After doing some research, I created this repository specifically to help with that. Our tool lets you publish new builds on Steam directly from your Unity Editor on macOS. Plus, you can publish for both Windows and macOS. The best part? It cuts down my publishing time from 30 minutes to under 1 minute.

## Steamworks - Getting Started

1. Create a Steamworks Account:

    Begin by creating an account on Steamworks, which is the development portal for Steam.
    Visit the Steamworks website and follow the registration process to set up your developer account.

2. Access the Steamworks Dashboard:

    Once your Steamworks account is set up, log in and navigate to the Steamworks dashboard.

3. Create Your Application:

    Within the Steamworks dashboard, initiate the process of creating a new application for your game.
    Provide the necessary details and information about your game during the application setup.

4. Obtain Your App ID:

    As part of the application creation process, Steam will assign a unique App ID to your game.
    This App ID is crucial for identifying and managing your game on the Steam platform. 

## Launch Options

1. Go to Steamworks
2. Click on Dashboard
3. Select your app
4. Go to **Edit Steamworks Settings**
5. Go to Installation > General Installation
6. Now add your Launch Options. In my example I added launch options for both Windows and MacOS. You can make launch options for the platforms of your wish. In the image below you can see an example of how I did it.

Instead of AppName use your actual app name with spaces (if your game name has spaces).

![Screenshot 2023-12-07 at 12 30 02](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/cfe16859-8175-46be-9071-7a45aad71d09)

## Installing Depots

Depots help organize game content into distinct categories based on the type of content. Common types of depots include executables, assets (graphics, audio, etc.), and localization files.

1. You need to create depots for each platform that you are publishing on.
2. Go to Steamworks dashboard.
3. Select your app.
4. Edit Steamworks Settings.
5. Select SteamPipe > Depots.
6. Click Add new Depot.

In the image below is an example of what it is suppose to look like. In the read area is my app name that I've hidden. On the left side you will see depot ID. Each platform (e.g Windows, MacOS) has its own ID.

![Screenshot 2023-12-07 at 12 41 03](https://github.com/tomicz/unity-steam-macos-deployer/assets/7763133/8dc3edb3-9076-4b94-be92-494a16be2f0a)