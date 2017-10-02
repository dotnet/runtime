MONO_BTLS_SOURCES_FILES = \
	btls-bio.c \
	btls-bio.h \
	btls-error.c \
	btls-error.h \
	btls-key.c \
	btls-key.h \
	btls-pkcs12.c \
	btls-pkcs12.h \
	btls-ssl.c \
	btls-ssl-ctx.c \
	btls-ssl-ctx.h \
	btls-ssl.h \
	btls-time64.c \
	btls-util.c \
	btls-util.h \
	btls-x509.c \
	btls-x509-chain.c \
	btls-x509-chain.h \
	btls-x509-crl.c \
	btls-x509-crl.h \
	btls-x509.h \
	btls-x509-lookup.c \
	btls-x509-lookup.h \
	btls-x509-lookup-mono.c \
	btls-x509-lookup-mono.h \
	btls-x509-name.c \
	btls-x509-name.h \
	btls-x509-revoked.c \
	btls-x509-revoked.h \
	btls-x509-store.c \
	btls-x509-store-ctx.c \
	btls-x509-store-ctx.h \
	btls-x509-store.h \
	btls-x509-verify-param.c \
	btls-x509-verify-param.h \
	CMakeLists.txt

EXTRA_DIST = $(MONO_BTLS_SOURCES_FILES)

CMAKE_VERBOSE=$(if $(V),VERBOSE=1,)
NINJA_VERBOSE=$(if ($V),-v,)

if NINJA
NINJA_ARGS = -G Ninja
BUILDFILE = build.ninja
else
NINJA_ARGS =
BUILDFILE = Makefile
endif

CMAKE_ARGS = -D CMAKE_INSTALL_PREFIX:PATH=$(prefix) -D BTLS_ROOT:PATH=$(BTLS_ROOT) \
	-D SRC_DIR:PATH=$(abs_top_srcdir)/mono/btls -D BTLS_CFLAGS:STRING="$(BTLS_CFLAGS)" $(NINJA_ARGS)

all-local: build-shared/libmono-btls-shared$(libsuffix)

build-shared/$(BUILDFILE):
	-mkdir -p build-shared
	(cd build-shared && CC="$(CC)" CXX="$(CXX)" $(CMAKE) $(CMAKE_ARGS) $(BTLS_CMAKE_ARGS) -DBUILD_SHARED_LIBS=1 $(abs_top_srcdir)/mono/btls)

if NINJA
build-shared/libmono-btls-shared$(libsuffix): build-shared/$(BUILDFILE) $(MONO_BTLS_SOURCES_FILES)
	ninja -C build-shared $(NINJA_VERBOSE)
else
build-shared/libmono-btls-shared$(libsuffix): build-shared/$(BUILDFILE) $(MONO_BTLS_SOURCES_FILES)
	$(MAKE) -C build-shared $(CMAKE_VERBOSE)
endif

clean-local:
	-rm -rf build-shared

install-exec-local:
	$(mkinstalldirs) "$(DESTDIR)$(libdir)"
	$(install_sh) build-shared/libmono-btls-shared.* "$(DESTDIR)$(libdir)"
