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
MSBUILD    := xbuild

# ===== install dirs =====
BINDIR     = $(DESTDIR)$(PREFIX)/bin
SHAREDIR   = $(DESTDIR)$(PREFIX)/share/$(APP)
DATADIR    = $(DESTDIR)$(PREFIX)/share
DESKTOPDIR = $(DATADIR)/applications
ICONDSTDIR = $(DATADIR)/icons/hicolor/256x256/apps

# ===== targets =====
.PHONY: all build install uninstall

all: build

# msbuild にすべて任せるビルド
build:
	@echo "Building $(APP) ($(CONFIG))..."
	$(MSBUILD) $(SOLUTION) /p:Configuration=$(CONFIG)

install: all
	# ディレクトリ作成
	install -d $(BINDIR) $(SHAREDIR) $(DESKTOPDIR) $(ICONDSTDIR)

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

	# アイコン
	if [ -f flatpak/icons/256x256.png ]; then \
	  install -m 644 flatpak/icons/256x256.png $(ICONDSTDIR)/$(APP).png; \
	fi

	# キャッシュ更新
	- update-desktop-database -q $(DATADIR)/applications || true
	- gtk-update-icon-cache -q $(DATADIR)/icons/hicolor    || true

uninstall:
	@echo "Removing $(APP) from $(PREFIX)..."
	rm -f $(BINDIR)/$(APP)
	rm -f $(SHAREDIR)/$(APP_WIN).exe
	rm -f $(DESKTOPDIR)/$(APP).desktop
	rm -f $(ICONDSTDIR)/$(APP).png
	- update-desktop-database -q $(DATADIR)/applications || true
	- gtk-update-icon-cache -q $(DATADIR)/icons/hicolor    || true
