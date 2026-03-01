## SPDX-License-Identifier: 0BSD
## Author: Lasse Collin

nobase_include_HEADERS = \
	lzma.h \
	lzma/base.h \
	lzma/bcj.h \
	lzma/block.h \
	lzma/check.h \
	lzma/container.h \
	lzma/delta.h \
	lzma/filter.h \
	lzma/hardware.h \
	lzma/index.h \
	lzma/index_hash.h \
	lzma/lzma12.h \
	lzma/stream_flags.h \
	lzma/version.h \
	lzma/vli.h

if COND_DOXYGEN
$(top_builddir)/doc/api/index.html: $(top_srcdir)/doxygen/update-doxygen $(top_srcdir)/doxygen/Doxyfile $(nobase_include_HEADERS)
	$(MKDIR_P) "$(top_builddir)/doc"
	"$(top_srcdir)/doxygen/update-doxygen" api \
		"$(top_srcdir)" "$(top_builddir)/doc"

all-local: $(top_builddir)/doc/api/index.html

install-data-local:
	$(MKDIR_P) "$(DESTDIR)$(docdir)/api"
	$(INSTALL_DATA) "$(top_builddir)"/doc/api/* "$(DESTDIR)$(docdir)/api"

uninstall-local:
	rm -rf "$(DESTDIR)$(docdir)/api"

clean-local:
	rm -rf "$(top_builddir)/doc/api"
endif
