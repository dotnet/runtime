
if PLATFORM_WIN32
export HOST_CC
endif

bin_PROGRAMS = monograph

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

monograph_LDADD = \
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

GRAPHS=System.Object System.Enum System.Attribute System.ValueType System.Reflection.MemberInfo
OUT=$(GRAPHS:=.jpeg)

graphs: $(OUT)

%.jpeg: monograph
	./monograph -n -o $*.png corlib.dll $*
	convert -geometry '480x360>' $*.png $*.jpeg

EXTRA_DIST = ChangeLog

