
if HOST_WIN32
export HOST_CC
endif

if JIT_SUPPORTED
runtime_lib=../mini/libmono-$(API_VER).la
else
runtime_lib=../interpreter/libmint.la
endif

bin_PROGRAMS = monograph

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GLIB_CFLAGS)

monograph_LDADD = \
	$(runtime_lib)			\
	$(GLIB_LIBS)			\
	$(LLVM_LIBS)			\
	-lm

GRAPHS=System.Object System.Enum System.Attribute System.ValueType System.Reflection.MemberInfo
OUT=$(GRAPHS:=.jpeg)

graphs: $(OUT)

%.jpeg: monograph
	./monograph -n -o $*.png corlib.dll $*
	convert -geometry '480x360>' $*.png $*.jpeg

EXTRA_DIST = ChangeLog

