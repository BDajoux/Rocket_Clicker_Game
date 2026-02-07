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

log_dir="$script_dir/logs"
mkdir -p "$log_dir"

pids=()

start_backend() {
  if command -v dotnet >/dev/null 2>&1 && [ -d "$backend_proj_dir" ]; then
    echo "Applying EF migrations (dotnet ef database update) in $backend_proj_dir ..."
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
  echo "Stopping processes..."
  for pid in "${pids[@]:-}"; do
    if [ -n "$pid" ]; then
      kill "$pid" >/dev/null 2>&1 || true
    fi
  done
  exit 0
}

trap cleanup INT TERM EXIT

start_backend
start_front

echo "Started. Backend: http://localhost:$backend_port  Front: http://localhost:$front_port"
echo "Logs: $log_dir/backend.log and $log_dir/front.log"

# wait for background processes so the script keeps running until interrupted
wait
