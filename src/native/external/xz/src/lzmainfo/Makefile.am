## SPDX-License-Identifier: 0BSD
## Author: Lasse Collin

bin_PROGRAMS = lzmainfo

lzmainfo_SOURCES = \
	lzmainfo.c \
	../common/tuklib_progname.c \
	../common/tuklib_mbstr_nonprint.c \
	../common/tuklib_mbstr_width.c \
	../common/tuklib_mbstr_wrap.c \
	../common/tuklib_exit.c

if COND_W32
lzmainfo_SOURCES += lzmainfo_w32res.rc
endif

lzmainfo_CPPFLAGS = \
	-DLOCALEDIR=\"$(localedir)\" \
	-I$(top_srcdir)/src/common \
	-I$(top_srcdir)/src/liblzma/api

lzmainfo_LDADD = $(top_builddir)/src/liblzma/liblzma.la

if COND_GNULIB
lzmainfo_CPPFLAGS += \
	-I$(top_builddir)/lib \
	-I$(top_srcdir)/lib
lzmainfo_LDADD += $(top_builddir)/lib/libgnu.a
endif

lzmainfo_LDADD += $(LTLIBINTL)


dist_man_MANS = lzmainfo.1

# Windows resource compiler support
.rc.o:
	$(RC) $(DEFS) $(DEFAULT_INCLUDES) $(INCLUDES) \
		$(lzmainfo_CPPFLAGS) $(CPPFLAGS) $(RCFLAGS) -i $< -o $@

# The installation of translated man pages abuses Automake internals
# by calling "install-man" with redefined dist_man_MANS and man_MANS.
# If this breaks some day, don't blame Automake developers.
install-data-hook:
	languages= ; \
	if test "$(USE_NLS)" = yes && test -d "$(top_srcdir)/po4a/man"; then \
		languages=`ls "$(top_srcdir)/po4a/man"`; \
	fi; \
	for lang in . $$languages; do \
		man="$(top_srcdir)/po4a/man/$$lang/lzmainfo.1" ; \
		if test -f "$$man"; then \
			$(MAKE) dist_man_MANS="$$man" man_MANS= \
				mandir="$(mandir)/$$lang" install-man; \
		fi; \
	done

uninstall-hook:
	languages= ; \
	if test "$(USE_NLS)" = yes && test -d "$(top_srcdir)/po4a/man"; then \
		languages=`ls "$(top_srcdir)/po4a/man"`; \
	fi; \
	for lang in . $$languages; do \
		name=`echo lzmainfo | sed '$(transform)'` && \
		rm -f "$(DESTDIR)$(mandir)/$$lang/man1/$$name.1"; \
	done
