MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

#
# Conditional submodule for jemalloc
#
# make reset-jemalloc will checkout a version of jemalloc which is suitable for this version of mono
# into $top_srcdir/jemalloc/jemalloc.
#

JEMALLOC_PATH=jemalloc

SUBMODULES_CONFIG_FILE = $(top_srcdir)/mono/utils/jemalloc/SUBMODULES.json
include $(top_srcdir)/scripts/submodules/versions.mk

$(eval $(call ValidateVersionTemplate,jemalloc,JEMALLOC))

# Bump the given submodule to the revision given by the REV make variable
# If COMMIT is 1, commit the change
bump-jemalloc: __bump-version-jemalloc

# Bump the given submodule to the branch given by the BRANCH/REMOTE_BRANCH make variables
# If COMMIT is 1, commit the change
bump-branch-jemalloc: __bump-branch-jemalloc

# Bump the given submodule to its current GIT version
# If COMMIT is 1, commit the change
bump-current-jemalloc: __bump-current-version-jemalloc

clean-local:
	$(RM) -r $(JEMALLOC_PATH)

EXTRA_DIST=SUBMODULES.json

if MONO_JEMALLOC_ASSERT
ASSERT_OPT=--enable-debug
endif

jemalloc:
	$(MAKE) reset-jemalloc

# Set a prefix to enable access to allocation functions with a prefix, ie so mono_jemalloc isn't named malloc by default
# Disable zone allocator, otherwise malloc uses jemalloc for things it's not set for
# We call autoconf ourselves so we can call configure and not autogen. Autogen script is broken, minor bash issues around quote escaping
jemalloc/lib/libjemalloc.a: jemalloc Makefile
	cd jemalloc && autoconf && ./configure --with-jemalloc-prefix=mono_je --prefix=`pwd` --enable-debug $(JEMALLOC_AUTOCONF_FLAGS) --disable-zone-allocator EXTRA_CFLAGS="-I $(top_srcdir) $(GLIB_CFLAGS) $(CFLAGS) $(PLATFORM_CFLAGS) $(ARCH_CFLAGS) $(SHARED_CFLAGS) -Wno-error" CC="$(CC)" CXX="$(CXX)" CPPFLAGS="$(CPPFLAGS) $(JEMALLOC_CPPFLAGS) " CXXFLAGS="$(CXXFLAGS)" LDFLAGS="$(LDFLAGS)"

	cd jemalloc && $(MAKE) build_lib_static 

all-local: jemalloc/lib/libjemalloc.a


