INCLUDES = $(BUILD_GLIB_CFLAGS) 
CC=$(CC_FOR_BUILD)

#noinst_PROGRAMS = sample

#sample_SOURCES = sample.c

parser.c:  monoburg.y
	bison monoburg.y -o parser.c

monoburg$(BUILD_EXEEXT): monoburg.c monoburg.h parser.c
	$(CC_FOR_BUILD) -o $@ monoburg.c parser.c $(INCLUDES) $(BUILD_GLIB_LIBS)

sample.c: monoburg$(BUILD_EXEEXT) sample.brg
	./monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg >sample.c

#sample_LDADD = $(BUILD_GLIB_LIBS)

BUILT_SOURCES = parser.c sample.c

CLEANFILES = $(BUILT_SOURCES)

man_MANS = monoburg.1

EXTRA_DIST = ChangeLog monoburg.y sample.brg $(man_MANS)

