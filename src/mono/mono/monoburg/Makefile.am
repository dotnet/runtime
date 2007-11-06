if CROSS_COMPILING

CC=$(CC_FOR_BUILD)

am_CFLAGS = $(BUILD_GLIB_CFLAGS) -I$(srcdir) -I$(top_builddir) $(CFLAGS_FOR_BUILD)

all: monoburg$(BUILD_EXEEXT)

monoburg$(BUILD_EXEEXT): $(srcdir)/monoburg.c $(srcdir)/monoburg.h parser.c
	$(CC_FOR_BUILD) -o $@ $(srcdir)/monoburg.c parser.c $(am_CFLAGS) $(LDFLAGS) $(BUILD_GLIB_LIBS)

else

noinst_SOURCES = sample.c
noinst_PROGRAMS = monoburg

monoburg_CFLAGS =       \
        $(GLIB_CFLAGS)

monoburg_SOURCES =      \
        monoburg.c      \
        parser.c

monoburg_LDFLAGS =      \ 
        $(GLIB_LIBS)    \
        -lm
endif

parser.c:  $(srcdir)/monoburg.y
	bison $(srcdir)/monoburg.y -o parser.c
    
sample.c: monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg
	./monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg > sample.c
    
BUILT_SOURCES = parser.c sample.c
    
CLEANFILES = $(BUILT_SOURCES) monoburg$(BUILD_EXEEXT)
    
man_MANS = monoburg.1
    
EXTRA_DIST = ChangeLog monoburg.c monoburg.h monoburg.y sample.brg $(man_MANS)
