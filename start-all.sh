#!/usr/bin/env bash
set -euo pipefail

# start-all.sh — démarre le backend (GameServerApi) et sert le dossier front
# Usage: chmod +x start-all.sh && ./start-all.sh

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
back_dir="$script_dir/back"
backend_proj_dir="$back_dir/GameServerApi"
front_dir="$script_dir/front"
backend_port=5000
front_port=8000

# If user-local dotnet was installed (~/.dotnet), make sure this script's PATH
# and DOTNET_ROOT include it so non-interactive runs can find dotnet and dotnet tools.
if [ -d "$HOME/.dotnet" ]; then
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
fi

log_dir="$script_dir/logs"
mkdir -p "$log_dir"

pids=()

start_backend() {
  if command -v dotnet >/dev/null 2>&1 && [ -d "$backend_proj_dir" ]; then
    echo "Applying EF migrations (dotnet ef database update) in $backend_proj_dir ..."

    # Ensure dotnet-ef global tool is available for SDK 9 if needed
    if ! command -v dotnet-ef >/dev/null 2>&1; then
      echo "dotnet-ef not found in PATH. Attempting to install dotnet-ef global tool (channel 9)..."
      if command -v dotnet >/dev/null 2>&1; then
        # prefer specific channel 9 tool matching installed SDK
        if ! dotnet tool install --global dotnet-ef --version 9.* >/dev/null 2>&1; then
          echo "Failed to install dotnet-ef global tool. Please install it manually with:\n  dotnet tool install --global dotnet-ef --version 9.*" >&2
          echo "Continuing but 'dotnet ef' may fail." >&2
        else
          echo "dotnet-ef installed to $HOME/.dotnet/tools"
          export PATH="$HOME/.dotnet/tools:$PATH"
        fi
      fi
    fi

    # If there is no Migrations folder, create an initial migration first.
    if [ ! -d "$backend_proj_dir/Migrations" ]; then
      echo "Migrations folder not found. Creating initial migration 'Initial_migrations'..."
      (cd "$backend_proj_dir" && dotnet ef migrations add "Initial_migrations" ) || echo "Could not create initial migration (it may already exist or dotnet-ef is missing)."
    fi

    (cd "$backend_proj_dir" && dotnet ef database update) || { echo "dotnet ef failed. Fix migrations and retry." >&2; exit 1; }

    echo "Starting backend (GameServerApi) on http://localhost:$backend_port ..."
    (cd "$backend_proj_dir" && ASPNETCORE_URLS="http://localhost:$backend_port" dotnet run) >"$log_dir/backend.log" 2>&1 &
    pids+=("$!")
  elif [ -d "$script_dir/mock-server" ]; then
    mock_dir="$script_dir/mock-server"
    if [ -f "$mock_dir/package.json" ] && [ ! -d "$mock_dir/node_modules" ]; then
      echo "Installing Node deps for mock backend..."
      (cd "$mock_dir" && npm install)
    fi
    echo "Starting mock backend on http://localhost:$backend_port ..."
    (cd "$mock_dir" && node server.js) >"$log_dir/backend.log" 2>&1 &
    pids+=("$!")
  else
    echo "No backend found (dotnet or mock-server). Start backend manually." >&2
  fi
}

start_front() {
  echo "Starting static server for front on http://localhost:$front_port ..."
  if [ -d "$front_dir" ]; then
    if command -v python3 >/dev/null 2>&1; then
      (cd "$front_dir" && python3 -m http.server "$front_port") >"$log_dir/front.log" 2>&1 &
      pids+=("$!")
    elif command -v python >/dev/null 2>&1; then
      (cd "$front_dir" && python -m http.server "$front_port") >"$log_dir/front.log" 2>&1 &
      pids+=("$!")
    elif command -v npx >/dev/null 2>&1; then
      (cd "$front_dir" && npx serve -l "$front_port") >"$log_dir/front.log" 2>&1 &
      pids+=("$!")
    else
      echo "No python or npx available; serve front manually." >&2
    fi
  else
    echo "Front folder not found: $front_dir" >&2
  fi
}

cleanup() {
  # Disable EXIT trap to avoid recursive calls when we call exit from here.
  trap - EXIT
  echo "Stopping processes..."
  for pid in "${pids[@]:-}"; do
    if [ -n "$pid" ]; then
      kill "$pid" >/dev/null 2>&1 || true
    fi
  done
  # Exit with success after cleanup.
  exit 0
}

trap cleanup INT TERM EXIT

start_backend
start_front

echo "Started. Backend: http://localhost:$backend_port  Front: http://localhost:$front_port"
echo "Logs: $log_dir/backend.log and $log_dir/front.log"

# wait for background processes so the script keeps running until interrupted
wait
