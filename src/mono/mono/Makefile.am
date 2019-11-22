include $(top_srcdir)/mk/common.mk

if SUPPORT_SGEN
sgen_dirs = sgen
endif

if BTLS
btls_dirs = btls
endif

if MONO_NATIVE
native_dirs = native
endif

if ENABLE_NETCORE
btls_dirs = 
endif

if ENABLE_NETCORE
SUBDIRS = eglib arch utils sgen zlib metadata mini profiler
else

if CROSS_COMPILING
SUBDIRS = $(btls_dirs) eglib arch utils cil zlib $(sgen_dirs) metadata mini dis profiler $(native_dirs)
else
if INSTALL_MONOTOUCH
SUBDIRS = $(btls_dirs) eglib arch utils zlib $(sgen_dirs) metadata mini profiler $(native_dirs)
else
SUBDIRS = $(btls_dirs) eglib arch utils cil zlib $(sgen_dirs) metadata mini dis tests unit-tests benchmark profiler $(native_dirs)
endif
endif
endif

DIST_SUBDIRS = btls native eglib arch utils cil zlib $(sgen_dirs) metadata mini dis tests unit-tests benchmark profiler
