BTLS_STATIC_LIST = build-static/mono-btls-static-lo.txt
BTLS_SHARED_LIST = build-shared/mono-btls-shared-lo.txt

BTLS_DEPS = $(BTLS_LIBS) build-shared/Makefile build-static/Makefile

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

