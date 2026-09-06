#!/bin/sh
# Runs one Unity batch-mode command against a project and prints only the interesting log lines.
# usage:   unity-batch.sh <project path> <Unity arguments...>
# example: unity-batch.sh . -executeMethod Tomicz.Deployer.CommandLine.Build -deploymentTarget Assets/Deployment/MacOS.asset
# The full Unity log is kept in <project>/Logs/steam-deploy-<timestamp>.log. Set UNITY_PATH to use a specific editor binary.
set -u

if [ $# -lt 2 ]; then
    echo "usage: unity-batch.sh <project path> <Unity arguments...>" >&2
    exit 2
fi

project=$(cd "$1" && pwd) || exit 1
shift

if [ ! -f "$project/ProjectSettings/ProjectVersion.txt" ]; then
    echo "$project is not a Unity project (no ProjectSettings/ProjectVersion.txt)." >&2
    exit 1
fi

version=$(sed -n 's/^m_EditorVersion: //p' "$project/ProjectSettings/ProjectVersion.txt")
unity="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity}"

if [ ! -x "$unity" ]; then
    echo "Unity $version not found at $unity. Install it with Unity Hub or set UNITY_PATH." >&2
    exit 1
fi

# Match only editor processes that have this project open, not Unity Hub helpers that mention the path.
if pgrep -f "Unity\.app/Contents/MacOS/Unity .*-(projectPath|createproject) $project( |$)" >/dev/null 2>&1; then
    echo "$project is open in the Unity editor. Close it and run again." >&2
    exit 1
fi

mkdir -p "$project/Logs"
log="$project/Logs/steam-deploy-$(date +%Y%m%d-%H%M%S).log"

"$unity" -batchmode -nographics -quit -projectPath "$project" -logFile "$log" "$@"
status=$?

grep -E '\[SteamDeployer\]|error CS[0-9]+|Build (succeeded|Failed|Cancelled)|^  [A-Z][A-Za-z ]*: |Deployment target .*: ' "$log" | grep -v 'UnityEngine.Debug:'
echo "exit code: $status (full log: $log)"
exit $status
