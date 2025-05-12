# ===== configurable =====
## アプリ名
APP        := calctus
APP_WIN    := Calctus
# MSBuild ソリューション
SOLUTION   := Calctus.sln
# ビルド構成 (Debug/Release)
CONFIG     ?= Release
# デフォルトのインストール先
PREFIX     ?= $(HOME)/.local
# パッケージ用仮ルート
DESTDIR    ?=
# MSBuild コマンド
MSBUILD    := $(if $(shell command -v msbuild 2>/dev/null),msbuild,xbuild)

# ===== install dirs =====
BINDIR     = $(DESTDIR)$(PREFIX)/bin
SHAREDIR   = $(DESTDIR)$(PREFIX)/share/$(APP)
DATADIR    = $(DESTDIR)$(PREFIX)/share
DESKTOPDIR = $(DATADIR)/applications
ICON_DIR   = $(DATADIR)/icons/hicolor
ICON48DIR  = $(ICON_DIR)/48x48/apps
ICON256DIR = $(ICON_DIR)/256x256/apps

# ===== targets =====
.PHONY: all build install uninstall

all: build

# msbuild/xbuild にすべて任せるビルド
build:
	@echo "Building $(APP) ($(CONFIG))..."
	$(MSBUILD) $(SOLUTION) /p:Configuration=$(CONFIG)

install: all
	# ディレクトリ作成
	install -d $(BINDIR) $(SHAREDIR) $(DESKTOPDIR) $(ICON48DIR) $(ICON256DIR)

	# 実行ファイル本体を share 配下へ
	install -m 755 bin/$(CONFIG)/$(APP_WIN).exe $(SHAREDIR)/$(APP_WIN).exe

	# ラッパースクリプトを bin へ
	printf '%s\n' \
	  '#!/usr/bin/env sh' \
	  'exec mono "$(PREFIX)/share/$(APP)/$(APP_WIN).exe" "$$@"' \
	> $(BINDIR)/$(APP)
	chmod 755 $(BINDIR)/$(APP)

	# .desktop ファイル
	sed 's|@APP@|$(APP)|g; s|@PREFIX@|$(PREFIX)|g' < $(APP).desktop.template \
	  > $(DESKTOPDIR)/$(APP).desktop
	chmod 644 $(DESKTOPDIR)/$(APP).desktop

	# アイコン (48x48 と 256x256)
	if [ -f flatpak/icons/hicolor/48x48/apps/jp.ponzu840w.Calctus.png ]; then \
	  install -m 644 flatpak/icons/hicolor/48x48/apps/jp.ponzu840w.Calctus.png $(ICON48DIR)/$(APP).png; \
	fi
	if [ -f flatpak/icons/hicolor/256x256/apps/jp.ponzu840w.Calctus.png ]; then \
	  install -m 644 flatpak/icons/hicolor/256x256/apps/jp.ponzu840w.Calctus.png $(ICON256DIR)/$(APP).png; \
	fi

	# キャッシュ更新
	- update-desktop-database -q $(DATADIR)/applications || true
	- gtk-update-icon-cache -q $(DATADIR)/icons/hicolor    || true

uninstall:
	@echo "Removing $(APP) from $(PREFIX)..."
	rm -f $(BINDIR)/$(APP)
	rm -f $(SHAREDIR)/$(APP_WIN).exe
	rm -f $(DESKTOPDIR)/$(APP).desktop
	rm -f $(ICON48DIR)/$(APP).png
	rm -f $(ICON256DIR)/$(APP).png
	- update-desktop-database -q $(DATADIR)/applications || true
	- gtk-update-icon-cache -q $(DATADIR)/icons/hicolor    || true
