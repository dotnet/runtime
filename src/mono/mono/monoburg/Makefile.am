INCLUDES = $(BUILD_GLIB_CFLAGS) 
CC=$(CC_FOR_BUILD)

#noinst_PROGRAMS = sample

#sample_SOURCES = sample.c
bdir=$(top_builddir)/$(subdir)

$(bdir)/parser.c:  $(srcdir)/monoburg.y
	bison $(srcdir)/monoburg.y -o $(bdir)/parser.c

monoburg$(BUILD_EXEEXT): $(srcdir)/monoburg.c $(srcdir)/monoburg.h $(bdir)/parser.c
	$(CC_FOR_BUILD) -o $@ $(srcdir)/monoburg.c $(bdir)/parser.c $(INCLUDES) $(BUILD_GLIB_LIBS)

$(bdir)/sample.c: monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg
	./monoburg$(BUILD_EXEEXT) $(srcdir)/sample.brg > $(bdir)/sample.c

#sample_LDADD = $(BUILD_GLIB_LIBS)

BUILT_SOURCES = $(bdir)/parser.c $(bdir)/sample.c

CLEANFILES = $(BUILT_SOURCES) monoburg$(BUILD_EXEEXT)

man_MANS = monoburg.1

EXTRA_DIST = ChangeLog monoburg.c monoburg.h monoburg.y sample.brg $(man_MANS)

