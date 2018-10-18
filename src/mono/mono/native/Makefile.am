if MONO_NATIVE_COMPAT
lib_LTLIBRARIES = libmono-native-compat.la libmono-native-unified.la
else
lib_LTLIBRARIES = libmono-native.la
endif

CC = $(MONO_NATIVE_CC)
CCLD = $(MONO_NATIVE_CC)
CCAS = $(MONO_NATIVE_CC)
CPP = $(MONO_NATIVE_CC) -E
CXX = $(MONO_NATIVE_CXX)
CXXCPP = $(MONO_NATIVE_CXX) -E
CC_FOR_BUILD = $(MONO_NATIVE_CC)
CPPFLAGS = $(MONO_NATIVE_CPPFLAGS)
CXXFLAGS = $(MONO_NATIVE_CXXFLAGS)
LDFLAGS = $(MONO_NATIVE_LDFLAGS)

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS)

common_sources = \
	pal_config.h \
	mono-native-platform.h \
	mono-native-platform.c \
	../../external/corefx/src/Native/Unix/Common/pal_compiler.h \
	../../external/corefx/src/Native/Unix/Common/pal_safecrt.h \
	../../external/corefx/src/Native/Unix/Common/pal_types.h \
	../../external/corefx/src/Native/Unix/Common/pal_utilities.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_errno.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_errno.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_maphardwaretype.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_maphardwaretype.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_memory.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_memory.h

macos_sources = $(unix_sources)

ios_sources = \
	pal-icalls.h \
	pal-icalls.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_networking.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_networking.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_tcpstate.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_tcpstate.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.h

linux_sources = $(unix_sources)

unix_sources = \
	pal-icalls.h \
	pal-icalls.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_networking.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_networking.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_networkstatistics.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_networkstatistics.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_tcpstate.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_tcpstate.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.h

gss_sources = \
	../../external/corefx/src/Native/Unix/System.Net.Security.Native/pal_gssapi.c \
	../../external/corefx/src/Native/Unix/System.Net.Security.Native/pal_gssapi.h

if ENABLE_GSS
macos_sources += $(gss_sources)

ios_sources += $(gss_sources)
endif

EXTRA_libmono_native_la_SOURCES = $(common_sources) $(macos_sources) $(ios_sources) $(linux_sources) $(unix_sources) $(gss_sources)

if MONO_NATIVE_PLATFORM_MACOS
platform_sources = $(macos_sources)
else
if MONO_NATIVE_PLATFORM_IOS
platform_sources = $(ios_sources)
else
if MONO_NATIVE_PLATFORM_LINUX
platform_sources = $(linux_sources)
endif
endif
endif

common_cflags = \
	-I$(abs_top_srcdir)/external/corefx/src/Native/Unix/Common \
	-I$(abs_top_srcdir)/external/corefx/src/Native/Unix/System.Native \
	-Wno-typedef-redefinition

libmono_native_la_SOURCES = $(common_sources) $(platform_sources) platform-type.c

libmono_native_la_CFLAGS = $(MONO_NATIVE_CFLAGS) $(common_cflags)

libmono_native_la_LDFLAGS = $(MONO_NATIVE_LDFLAGS)

libmono_native_la_LIBADD = $(MONO_NATIVE_LIBADD)

libmono_native_compat_la_SOURCES = $(common_sources) $(platform_sources) platform-type-compat.c

libmono_native_compat_la_CFLAGS = $(MONO_NATIVE_COMPAT_CFLAGS) $(common_cflags)

libmono_native_compat_la_LDFLAGS = $(MONO_NATIVE_COMPAT_LDFLAGS)

libmono_native_compat_la_LIBADD = $(MONO_NATIVE_LIBADD)

libmono_native_unified_la_SOURCES = $(common_sources) $(platform_sources) platform-type-unified.c

libmono_native_unified_la_CFLAGS = $(MONO_NATIVE_UNIFIED_CFLAGS) $(common_cflags)

libmono_native_unified_la_LDFLAGS = $(MONO_NATIVE_UNIFIED_LDFLAGS)

libmono_native_unified_la_LIBADD = $(MONO_NATIVE_LIBADD)


