
INCLUDES = \
	-I$(top_srcdir) 	\
	$(IKVM_JNI_CFLAGS)  \
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if JIT_SUPPORTED

lib_LTLIBRARIES = libmono-ikvm-jni.la
endif

libmono_ikvm_jni_la_SOURCES = mono-jni.c
libmono_ikvm_jni_la_LIBADD = $(top_builddir)/mono/mini/libmono.la

