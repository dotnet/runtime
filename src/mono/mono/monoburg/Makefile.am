INCLUDES = $(BUILD_GLIB_CFLAGS) 
CC=$(CC_FOR_BUILD)
LD=ld

noinst_PROGRAMS = monoburg sample

monoburg_SOURCES = parser.c monoburg.c monoburg.h

sample_SOURCES = sample.c

parser.c:  monoburg.y
	bison monoburg.y -o parser.c

sample.c: monoburg$(BUILD_EXEEXT) sample.brg
	./monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg >sample.c

monoburg_LDADD = $(BUILD_GLIB_LIBS)

sample_LDADD = $(BUILD_GLIB_LIBS)

BUILT_SOURCES = parser.c sample.c

CLEANFILES = $(BUILT_SOURCES)

man_MANS = monoburg.1

EXTRA_DIST = ChangeLog monoburg.y sample.brg $(man_MANS)

