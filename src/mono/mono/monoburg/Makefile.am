INCLUDES = $(BUILD_GLIB_CFLAGS) -I$(srcdir)
CC=$(CC_FOR_BUILD)

#noinst_PROGRAMS = sample

#sample_SOURCES = sample.c

all: monoburg$(BUILD_EXEEXT)

parser.c:  $(srcdir)/monoburg.y
	bison $(srcdir)/monoburg.y -o parser.c

monoburg$(BUILD_EXEEXT): $(srcdir)/monoburg.c $(srcdir)/monoburg.h parser.c
	$(CC_FOR_BUILD) -o $@ $(srcdir)/monoburg.c parser.c $(INCLUDES) $(BUILD_GLIB_LIBS)

sample.c: monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg
	./monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg > sample.c

#sample_LDADD = $(BUILD_GLIB_LIBS)

BUILT_SOURCES = parser.c sample.c

CLEANFILES = $(BUILT_SOURCES) monoburg$(BUILD_EXEEXT)

man_MANS = monoburg.1

EXTRA_DIST = ChangeLog monoburg.c monoburg.h monoburg.y sample.brg $(man_MANS)

