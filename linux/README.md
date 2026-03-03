# Linux Download and Install

Bu klasor Linux kullanicilari icin hizli indirme ve kurulum noktasi olarak eklendi.

## Quick Download

- Latest release sayfasi:  
  https://github.com/davutari/DevARISetup/releases/latest

Latest release ekranindan su dosyalari indirebilirsin:

- `devari-manager-<version>-x86_64.AppImage`
- `devari-manager_<version>_amd64.deb`

## Install AppImage

```bash
chmod +x devari-manager-*-x86_64.AppImage
./devari-manager-*-x86_64.AppImage
```

Opsiyonel kalici konum:

```bash
mkdir -p ~/Applications/DevARIManager/linux
mv devari-manager-*-x86_64.AppImage ~/Applications/DevARIManager/linux/devari-manager.AppImage
chmod +x ~/Applications/DevARIManager/linux/devari-manager.AppImage
```

## Install DEB

```bash
sudo dpkg -i devari-manager_*_amd64.deb
sudo apt-get install -f -y
```

## Build Locally (Optional)

```bash
./packaging/linux/build-linux.sh 0.1.0
```

Build ciktilari:

- `dist/linux/devari-manager_<version>_amd64.deb`
- `dist/linux/devari-manager-<version>-x86_64.AppImage`
