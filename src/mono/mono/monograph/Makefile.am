
if HOST_WIN32
export HOST_CC
endif

if JIT_SUPPORTED
if DISABLE_EXECUTABLES
runtime_lib=../mini/$(LIBMONO_LA) $(static_libs)
else
if !SHARED_MONO
static_libs=	\
	$(top_builddir)/mono/metadata/libmonoruntime-static.la	\
	$(top_builddir)/mono/io-layer/libwapi.la	\
	$(top_builddir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV) \
	$(LIBGC_STATIC_LIBS)

runtime_lib=../mini/$(LIBMONO_LA) $(static_libs)
else
runtime_lib=../mini/$(LIBMONO_LA)
endif
endif
else
runtime_lib=../interpreter/libmint.la
endif

if DISABLE_LIBRARIES
else
bin_PROGRAMS = monograph
endif

AM_CPPFLAGS = 				\
	-I$(top_srcdir)			\
	$(GLIB_CFLAGS)

monograph_LDADD = \
	$(runtime_lib)			\
	$(GLIB_LIBS)			\
	$(LLVM_LIBS)			\
	$(LIBICONV)			\
	-lm

if PLATFORM_DARWIN
monograph_LDFLAGS=-framework CoreFoundation
endif

GRAPHS=System.Object System.Enum System.Attribute System.ValueType System.Reflection.MemberInfo
OUT=$(GRAPHS:=.jpeg)

graphs: $(OUT)

%.jpeg: monograph
	./monograph -n -o $*.png corlib.dll $*
	convert -geometry '480x360>' $*.png $*.jpeg

EXTRA_DIST = ChangeLog

