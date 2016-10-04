BTLS_STATIC_LIST = build-static/mono-btls-static-lo.txt
BTLS_SHARED_LIST = build-shared/mono-btls-shared-lo.txt

EXTRA_DIST = btls-android-utils.c \
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
	CMakeLists.txt \
	create-object-library.sh

CMAKE_VERBOSE=$(if $(V),VERBOSE=1,)

CMAKE_ARGS = -D CMAKE_INSTALL_PREFIX:PATH=$(prefix) -D BTLS_ROOT:PATH=$(BTLS_ROOT) \
	-D SRC_DIR:PATH=$(abs_top_srcdir)/mono/btls -D BTLS_CFLAGS:STRING="$(BTLS_CFLAGS)"

all-local: $(BTLS_STATIC_LIST) $(BTLS_SHARED_LIST)

build-shared/Makefile:
	-mkdir -p build-shared
	(cd build-shared && $(CMAKE) $(CMAKE_ARGS) $(BTLS_CMAKE_ARGS) -DBUILD_SHARED_LIBS=1 $(abs_top_srcdir)/mono/btls)

build-static/Makefile:
	-mkdir -p build-static
	(cd build-static && $(CMAKE) $(CMAKE_ARGS) $(BTLS_CMAKE_ARGS) $(abs_top_srcdir)/mono/btls)

$(BTLS_STATIC_LIST): build-static/Makefile
	$(MAKE) -C build-static $(CMAKE_VERBOSE)

$(BTLS_SHARED_LIST): build-shared/Makefile
	$(MAKE) -C build-shared $(CMAKE_VERBOSE)

clean-local:
	-rm -rf build-static
	-rm -rf build-shared

