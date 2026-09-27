#!/bin/bash

set -e

PROJECT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

VERSION="1.0.0"
PACKAGE_NAME="PrivateAI-${VERSION}.deb"

BUILD_DIR="$SCRIPT_DIR/build"
PACKAGE_ROOT="$BUILD_DIR/private-ai"

rm -rf "$BUILD_DIR"

mkdir -p "$PACKAGE_ROOT/DEBIAN"
mkdir -p "$PACKAGE_ROOT/opt/private-ai"
mkdir -p "$PACKAGE_ROOT/usr/share/applications"

cp -a "$PROJECT_DIR/publish/linux-x64/." \
      "$PACKAGE_ROOT/opt/private-ai/"

cp "$SCRIPT_DIR/DEBIAN/control" \
   "$PACKAGE_ROOT/DEBIAN/control"

cp "$SCRIPT_DIR/usr/share/applications/private-ai.desktop" \
   "$PACKAGE_ROOT/usr/share/applications/private-ai.desktop"

chmod 755 "$PACKAGE_ROOT/opt/private-ai/PrivateAI"

dpkg-deb --build \
    "$PACKAGE_ROOT" \
    "$SCRIPT_DIR/$PACKAGE_NAME"

echo ""
echo "======================================"
echo " Debian package created successfully"
echo "======================================"
echo "$SCRIPT_DIR/$PACKAGE_NAME"