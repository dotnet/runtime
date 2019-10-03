if !ENABLE_MSVC_ONLY
if MONO_NATIVE_COMPAT
lib_LTLIBRARIES = libmono-native-compat.la libmono-native-unified.la
else
lib_LTLIBRARIES = libmono-native.la
endif
endif # !ENABLE_MSVC_ONLY

CC = $(MONO_NATIVE_CC)
CCLD = $(MONO_NATIVE_CC)
CCAS = $(MONO_NATIVE_CC)
CPP = $(MONO_NATIVE_CC) -E
CXX = $(MONO_NATIVE_CXX)
CXXCPP = $(MONO_NATIVE_CXX) -E
CC_FOR_BUILD = $(MONO_NATIVE_CC)

if !ENABLE_MSVC_ONLY

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
	../../external/corefx/src/Native/Unix/System.Native/pal_memory.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_uid.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_uid.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_time.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_time.h \
	../../external/corefx/src/Native/AnyOS/brotli/enc/dictionary_hash.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/backward_references_hq.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/histogram.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/memory.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/entropy_encode.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/compress_fragment_two_pass.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/block_splitter.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/encode.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/cluster.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/backward_references.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/utf8_util.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/compress_fragment.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/brotli_bit_stream.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/bit_cost.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/static_dict.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/literal_cost.c \
	../../external/corefx/src/Native/AnyOS/brotli/enc/metablock.c \
	../../external/corefx/src/Native/AnyOS/brotli/dec/state.c \
	../../external/corefx/src/Native/AnyOS/brotli/dec/decode.c \
	../../external/corefx/src/Native/AnyOS/brotli/dec/huffman.c \
	../../external/corefx/src/Native/AnyOS/brotli/dec/bit_reader.c \
	../../external/corefx/src/Native/AnyOS/brotli/common/dictionary.c

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

aix_sources = $(unix_sources)

freebsd_sources = $(unix_sources)

netbsd_sources = $(unix_sources)

haiku_sources = $(unix_sources)

android_sources = \
	pal-android.h \
	pal-android.c \
	pal-icalls.h \
	pal-icalls.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_io.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_networkstatistics.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_networkstatistics.h \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.c \
	../../external/corefx/src/Native/Unix/System.Native/pal_random.h


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

EXTRA_libmono_native_la_SOURCES = $(common_sources) $(macos_sources) $(ios_sources) $(linux_sources) $(aix_sources) $(freebsd_sources) $(netbsd_sources) $(haiku_sources) $(unix_sources) $(gss_sources)

if MONO_NATIVE_PLATFORM_MACOS
platform_sources = $(macos_sources)
else
if MONO_NATIVE_PLATFORM_IOS
platform_sources = $(ios_sources)
else
if MONO_NATIVE_PLATFORM_LINUX
platform_sources = $(linux_sources)
else
if MONO_NATIVE_PLATFORM_AIX
platform_sources = $(aix_sources)
else
if MONO_NATIVE_PLATFORM_ANDROID
platform_sources = $(android_sources)
else
if MONO_NATIVE_PLATFORM_FREEBSD
platform_sources = $(freebsd_sources)
else
if MONO_NATIVE_PLATFORM_NETBSD
platform_sources = $(netbsd_sources)
else
if MONO_NATIVE_PLATFORM_HAIKU
platform_sources = $(haiku_sources)
endif
endif
endif
endif
endif
endif
endif
endif

common_cppflags = -I$(top_srcdir) -I$(top_srcdir)/mono

common_cflags = \
	-I$(abs_top_srcdir)/external/corefx/src/Native/Unix/Common \
	-I$(abs_top_srcdir)/external/corefx/src/Native/Unix/System.Native \
	-I$(abs_top_srcdir)/external/corefx/src/Native/AnyOS/brotli/include \
	$(GLIB_CFLAGS) \
	$(SHARED_CFLAGS) \
	-DBROTLI_BUILD_NO_RBIT \
	-Wno-typedef-redefinition

endif # !ENABLE_MSVC_ONLY

# clear these flags as we set values for each target separately and automake always appends CFLAGS
CFLAGS=
CPPFLAGS=
CXXFLAGS=
LDFLAGS=
CCASFLAGS=

if !ENABLE_MSVC_ONLY

libmono_native_la_SOURCES = $(common_sources) $(platform_sources) platform-type.c

libmono_native_la_CFLAGS = $(MONO_NATIVE_CFLAGS) $(common_cflags)

libmono_native_la_CPPFLAGS = $(MONO_NATIVE_CPPFLAGS) $(common_cppflags)

libmono_native_la_CXXFLAGS = $(MONO_NATIVE_CXXFLAGS)

libmono_native_la_LDFLAGS = $(MONO_NATIVE_LDFLAGS)

libmono_native_la_CCASFLAGS = $(MONO_NATIVE_CCASFLAGS)

libmono_native_la_LIBADD = $(MONO_NATIVE_LIBADD)

libmono_native_compat_la_SOURCES = $(common_sources) $(platform_sources) platform-type-compat.c

libmono_native_compat_la_CFLAGS = $(MONO_NATIVE_COMPAT_CFLAGS) $(common_cflags)

libmono_native_compat_la_CPPFLAGS = $(MONO_NATIVE_COMPAT_CPPFLAGS) $(common_cppflags)

libmono_native_compat_la_CXXFLAGS = $(MONO_NATIVE_COMPAT_CXXFLAGS)

libmono_native_compat_la_LDFLAGS = $(MONO_NATIVE_COMPAT_LDFLAGS)

libmono_native_compat_la_CCASFLAGS = $(MONO_NATIVE_COMPAT_CCASFLAGS)

libmono_native_compat_la_LIBADD = $(MONO_NATIVE_LIBADD)

libmono_native_unified_la_SOURCES = $(common_sources) $(platform_sources) platform-type-unified.c

libmono_native_unified_la_CFLAGS = $(MONO_NATIVE_UNIFIED_CFLAGS) $(common_cflags)

libmono_native_unified_la_CPPFLAGS = $(MONO_NATIVE_UNIFIED_CPPFLAGS) $(common_cppflags)

libmono_native_unified_la_CXXFLAGS = $(MONO_NATIVE_UNIFIED_CXXFLAGS)

libmono_native_unified_la_LDFLAGS = $(MONO_NATIVE_UNIFIED_LDFLAGS)

libmono_native_unified_la_CCASFLAGS = $(MONO_NATIVE_UNIFIED_CCASFLAGS)

libmono_native_unified_la_LIBADD = $(MONO_NATIVE_LIBADD)

test-bundle-local:
	mkdir -p $(TEST_BUNDLE_PATH)
	cp -L .libs/libmono-native$(libsuffix) $(TEST_BUNDLE_PATH)/

endif # !ENABLE_MSVC_ONLY
