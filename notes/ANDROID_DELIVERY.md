# Android Delivery

## Current Firebase setup

- Firebase Android App ID: `1:617785418190:android:350b89516e471dccbe0b9f`
- Android package name: `Dimbeacon.spacecrawler`
- Godot export preset: `project/export_presets.cfg`
- Current APK output: `builds/SpaceCrawler.apk`

The Firebase App ID is not a private key, but deployment scripts should read it from the local ignored `.env` file when possible.

## Local ignored environment file

Create or update `.env` at the repository root:

```env
FIREBASE_ANDROID_APP_ID=1:617785418190:android:350b89516e471dccbe0b9f
FIREBASE_ANDROID_PACKAGE=Dimbeacon.spacecrawler
FIREBASE_TESTERS=your-email@example.com
```

`.env` is ignored by git and should not be committed.

## Manual Firebase upload

After exporting the APK from Godot:

```cmd
firebase appdistribution:distribute "C:\repos\SpaceCrawler\builds\SpaceCrawler.apk" ^
  --app "1:617785418190:android:350b89516e471dccbe0b9f" ^
  --release-notes "SpaceCrawler Android test build" ^
  --testers "your-email@example.com"
```

The package name in Firebase and Godot must match exactly:

```text
Dimbeacon.spacecrawler
```

