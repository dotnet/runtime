# Currently the jit needs some work before it will build on windows
if PLATFORM_WIN32
SUBDIRS = io-layer monoburg metadata cil dis \
	arch monograph interpreter wrapper tests
else
SUBDIRS = io-layer monoburg metadata cil dis \
	arch monograph interpreter jit wrapper tests
endif

