#!/bin/bash

set -e

PROJECT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

VERSION="1.0.0"

RPM_ROOT="$SCRIPT_DIR/rpmbuild"

rm -rf "$RPM_ROOT"

mkdir -p "$RPM_ROOT/BUILD"
mkdir -p "$RPM_ROOT/RPMS"
mkdir -p "$RPM_ROOT/SOURCES"
mkdir -p "$RPM_ROOT/SPECS"
mkdir -p "$RPM_ROOT/SRPMS"

mkdir -p "$RPM_ROOT/BUILD/PrivateAI"

cp -a "$PROJECT_DIR/publish/linux-x64/." \
      "$RPM_ROOT/BUILD/PrivateAI/"

cp "$SCRIPT_DIR/private-ai.spec" \
   "$RPM_ROOT/SPECS/private-ai.spec"

rpmbuild \
    --define "_topdir $RPM_ROOT" \
    -bb "$RPM_ROOT/SPECS/private-ai.spec"

echo ""
echo "======================================"
echo " RPM package created successfully"
echo "======================================"

find "$RPM_ROOT/RPMS" -name "*.rpm" -print