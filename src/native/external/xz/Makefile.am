## SPDX-License-Identifier: 0BSD
## Author: Lasse Collin

# Use -n to prevent gzip from adding a timestamp to the .gz headers.
GZIP_ENV = -9n

DIST_SUBDIRS = lib src po tests debug
SUBDIRS =

if COND_GNULIB
SUBDIRS += lib
endif

SUBDIRS += src po tests

if COND_DOC
dist_doc_DATA = \
	AUTHORS \
	COPYING \
	COPYING.0BSD \
	COPYING.GPLv2 \
	NEWS \
	README \
	THANKS \
	doc/faq.txt \
	doc/history.txt \
	doc/xz-file-format.txt \
	doc/lzma-file-format.txt

examplesdir = $(docdir)/examples
dist_examples_DATA = \
	doc/examples/00_README.txt \
	doc/examples/01_compress_easy.c \
	doc/examples/02_decompress.c \
	doc/examples/03_compress_custom.c \
	doc/examples/04_compress_easy_mt.c \
	doc/examples/11_file_info.c \
	doc/examples/Makefile
endif

EXTRA_DIST = \
	cmake \
	dos \
	doxygen \
	extra \
	po4a \
	windows \
	CMakeLists.txt \
	COPYING.GPLv2 \
	COPYING.GPLv3 \
	COPYING.LGPLv2.1 \
	INSTALL.generic \
	PACKAGERS \
	TODO \
	autogen.sh \
	build-aux/license-check.sh \
	build-aux/manconv.sh \
	build-aux/version.sh \
	po/xz.pot-header

ACLOCAL_AMFLAGS = -I m4

# List of man pages to convert to plain text in the dist-hook target
# or to PDF in the pdf-local target.
manfiles = \
	src/xz/xz.1 \
	src/xzdec/xzdec.1 \
	src/lzmainfo/lzmainfo.1 \
	src/scripts/xzdiff.1 \
	src/scripts/xzgrep.1 \
	src/scripts/xzless.1 \
	src/scripts/xzmore.1

# Create ChangeLog using "git log".
# Convert the man pages to plain text (ASCII only) format.
dist-hook:
	if test -d "$(srcdir)/.git" && type git > /dev/null 2>&1; then \
		( cd "$(srcdir)" && git log --pretty=medium --date=iso --stat \
			b69da6d4bb6bb11fc0cf066920791990d2b22a06^..HEAD ) \
			> "$(distdir)/ChangeLog"; \
	fi
	if type groff > /dev/null 2>&1; then \
		dest="$(distdir)/doc/man" && \
		$(MKDIR_P) "$$dest/txt" && \
		for FILE in $(manfiles); do \
			BASE=`basename $$FILE .1` && \
			$(SHELL) "$(srcdir)/build-aux/manconv.sh" ascii \
				< "$(srcdir)/$$FILE" \
				> "$$dest/txt/$$BASE.txt"; \
		done; \
	fi
	cd "$(distdir)" && $(SHELL) "build-aux/license-check.sh"

# This works with GNU tar and gives cleaner package than normal 'make dist'.
# This also ensures that the translations are up to date (dist-hook
# would be too late for that).
mydist:
	$(SHELL) "$(srcdir)/src/liblzma/validate_map.sh"
	cd po && $(MAKE) xz.pot-update
	cd "$(srcdir)/po4a" && $(SHELL) update-po
	VERSION=$(VERSION); \
	if test -d "$(srcdir)/.git" && type git > /dev/null 2>&1; then \
		$(SHELL) "$(srcdir)/build-aux/license-check.sh" || exit 1; \
		SNAPSHOT=`cd "$(srcdir)" && git describe --abbrev=8 | cut -b2-`; \
		test -n "$$SNAPSHOT" && VERSION=$$SNAPSHOT; \
	fi; \
	TAR_OPTIONS='--owner=0 --group=0 --numeric-owner --mode=u+rw,go+r-w --sort=name' \
		LC_COLLATE=C \
		$(MAKE) VERSION="$$VERSION" dist-gzip

# NOTE: This only creates the PDFs. The install rules are missing.
pdf-local:
	dest="doc/man" && \
	$(MKDIR_P) "$$dest/pdf-a4" "$$dest/pdf-letter" && \
	for FILE in $(manfiles); do \
		BASE=`basename $$FILE .1` && \
		$(SHELL) "$(srcdir)/build-aux/manconv.sh" pdf a4 \
			< "$(srcdir)/$$FILE" \
			> "$$dest/pdf-a4/$$BASE-a4.pdf" && \
		$(SHELL) "$(srcdir)/build-aux/manconv.sh" pdf letter \
			< "$(srcdir)/$$FILE" \
			> "$$dest/pdf-letter/$$BASE-letter.pdf"; \
	done
