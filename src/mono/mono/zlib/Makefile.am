MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

ZLIB_SOURCE = \
	adler32.c	\
	compress.c	\
	crc32.c		\
	uncompr.c	\
	deflate.c	\
	gzguts.h	\
	trees.c		\
	zutil.c		\
	inflate.c	\
	infback.c	\
	inftrees.c	\
	inffast.c	\
	crc32.h		\
	deflate.h  	\
	inffast.h  	\
	inffixed.h  	\
	inflate.h  	\
	inftrees.h  	\
	trees.h  	\
	zconf.h  	\
	zlib.h  	\
	zutil.h

if !HAVE_SYS_ZLIB
if !HAVE_STATIC_ZLIB
noinst_LTLIBRARIES = libz.la
endif
endif

libz_la_SOURCES = \
	$(ZLIB_SOURCE)
