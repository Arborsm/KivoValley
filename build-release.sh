#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"

if ! command -v "$DOTNET_BIN" >/dev/null 2>&1; then
    if [[ -x "$HOME/.dotnet/dotnet" ]]; then
        DOTNET_BIN="$HOME/.dotnet/dotnet"
    else
        echo "dotnet not found. Set DOTNET_BIN or install the .NET SDK." >&2
        exit 1
    fi
fi

cd "$ROOT_DIR"

"$DOTNET_BIN" build KivoValley.sln --configuration Release

rm -rf "$ROOT_DIR/Release"
mkdir -p "$ROOT_DIR/Release"

cp "$ROOT_DIR/KivoValley/bin/Release/net6.0/KivoValley.dll" "$ROOT_DIR/Release/"
cp "$ROOT_DIR/KivoValleyFonts/bin/Release/net6.0/KivoValleyFonts.dll" "$ROOT_DIR/Release/"

echo "Release DLLs copied to $ROOT_DIR/Release:"
ls -lh "$ROOT_DIR/Release"/*.dll
