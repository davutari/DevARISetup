#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VERSION="${1:-0.1.0}"
APP_NAME="devari-manager"
APP_DISPLAY_NAME="DevARI Manager (Linux)"
RID="linux-x64"
OUT_DIR="$ROOT_DIR/dist/linux"
PUBLISH_DIR="$OUT_DIR/publish"
DEB_ROOT="$OUT_DIR/debroot"
APPDIR="$OUT_DIR/AppDir"
DESKTOP_FILE="$ROOT_DIR/packaging/linux/assets/devari-manager.desktop"
SVG_ICON="$ROOT_DIR/packaging/linux/assets/devari-manager.svg"
APPIMAGE_TOOL="$ROOT_DIR/.local-tools/appimagetool-x86_64.AppImage"

rm -rf "$OUT_DIR"
mkdir -p "$PUBLISH_DIR"

echo "[1/4] Publishing $APP_NAME for $RID..."
dotnet publish "$ROOT_DIR/src/DevARIManager.Linux.Gui/DevARIManager.Linux.Gui.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -o "$PUBLISH_DIR"

BIN_PATH="$PUBLISH_DIR/$APP_NAME"
if [[ ! -x "$BIN_PATH" ]]; then
  echo "Published binary not found: $BIN_PATH" >&2
  exit 1
fi

echo "[2/4] Building .deb package..."
mkdir -p "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/usr/local/bin"
mkdir -p "$DEB_ROOT/opt/$APP_NAME"
mkdir -p "$DEB_ROOT/usr/share/applications"
mkdir -p "$DEB_ROOT/usr/share/icons/hicolor/scalable/apps"

cat > "$DEB_ROOT/DEBIAN/control" <<EOF_CONTROL
Package: $APP_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: DevARI Team <devari@example.com>
Depends: libc6 (>= 2.31), libgcc-s1, libstdc++6
Description: Linux port of DevARI Setup with desktop GUI
EOF_CONTROL

cp -a "$PUBLISH_DIR/." "$DEB_ROOT/opt/$APP_NAME/"
ln -sf "/opt/$APP_NAME/$APP_NAME" "$DEB_ROOT/usr/local/bin/$APP_NAME"
install -m 0644 "$DESKTOP_FILE" "$DEB_ROOT/usr/share/applications/$APP_NAME.desktop"
install -m 0644 "$SVG_ICON" "$DEB_ROOT/usr/share/icons/hicolor/scalable/apps/$APP_NAME.svg"

DEB_OUT="$OUT_DIR/${APP_NAME}_${VERSION}_amd64.deb"
dpkg-deb --build "$DEB_ROOT" "$DEB_OUT"

echo "[3/4] Building AppImage..."
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/lib/$APP_NAME"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/scalable/apps"

cp -a "$PUBLISH_DIR/." "$APPDIR/usr/lib/$APP_NAME/"
cat > "$APPDIR/usr/bin/$APP_NAME" <<EOF_RUN
#!/usr/bin/env bash
HERE="\$(dirname "\$(readlink -f "\${BASH_SOURCE[0]}")")"
exec "\$HERE/../lib/$APP_NAME/$APP_NAME" "\$@"
EOF_RUN
chmod +x "$APPDIR/usr/bin/$APP_NAME"
install -m 0644 "$DESKTOP_FILE" "$APPDIR/usr/share/applications/$APP_NAME.desktop"
install -m 0644 "$SVG_ICON" "$APPDIR/usr/share/icons/hicolor/scalable/apps/$APP_NAME.svg"
install -m 0644 "$DESKTOP_FILE" "$APPDIR/$APP_NAME.desktop"
install -m 0644 "$SVG_ICON" "$APPDIR/$APP_NAME.svg"

cp "$SVG_ICON" "$APPDIR/.DirIcon"
cat > "$APPDIR/AppRun" <<'EOF_APPRUN'
#!/usr/bin/env bash
HERE="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
exec "$HERE/usr/bin/devari-manager" "$@"
EOF_APPRUN
chmod +x "$APPDIR/AppRun"

if [[ ! -f "$APPIMAGE_TOOL" ]]; then
  if command -v appimagetool >/dev/null 2>&1; then
    appimagetool "$APPDIR" "$OUT_DIR/${APP_NAME}-${VERSION}-x86_64.AppImage"
  else
    echo "appimagetool bulunamadi. AppImage uretilmedi." >&2
    echo "AppImage icin: https://github.com/AppImage/AppImageKit/releases adresinden appimagetool indirin ve PATH'e ekleyin." >&2
  fi
else
  chmod +x "$APPIMAGE_TOOL"
  "$APPIMAGE_TOOL" "$APPDIR" "$OUT_DIR/${APP_NAME}-${VERSION}-x86_64.AppImage"
fi

echo "[4/4] Done."
echo "Publish:  $PUBLISH_DIR"
echo "Deb:      $DEB_OUT"
echo "AppImage: $OUT_DIR/${APP_NAME}-${VERSION}-x86_64.AppImage (eger appimagetool varsa)"
echo "App:      $APP_DISPLAY_NAME"
