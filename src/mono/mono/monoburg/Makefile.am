INCLUDES = $(GLIB_CFLAGS) 

noinst_PROGRAMS = monoburg sample

monoburg_SOURCES = parser.c monoburg.c

sample_SOURCES = sample.c

parser.c:  monoburg.y
	bison monoburg.y -o parser.c

sample.c: monoburg sample.brg
	./monoburg sample.brg >sample.c

monoburg_LDADD = $(GLIB_LIBS)

sample_LDADD = $(GLIB_LIBS)

BUILT_SOURCES = parser.c sample.c

CLEANFILES = $(BUILT_SOURCES)

