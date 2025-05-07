# install
```
flatpak install flathub org.freedesktop.Sdk//24.08
flatpak install flathub org.freedesktop.Platform//24.08
flatpak install flathub org.freedesktop.Sdk.Extension.mono6//24.08
```
# buildinit
```
flatpak build-init build-dir com.example.CalctusLinux org.freedesktop.Sdk/x86_64/24.08 org.freedesktop.Platform/x86_64/24.08
```
# build
```
flatpak-builder --disable-rofiles-fuse --user --install --force-clean build-dir flatpak/com.example.CalctusLinux.yaml
```
# run
```
flatpak run com.example.CalctusLinux.yaml
```
