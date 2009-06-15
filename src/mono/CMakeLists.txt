cmake_minimum_required(VERSION 2.6.4)

# FIXME: Sanitize the variables, no need for the duplicate am conditionals
# - It would be nice to rename 'CMakeFiles' to something like '.cmake'
# - It would be nice to have a per-target VERBOSE setting
# - or a way to change the setting name to 'V' and the output to CC <src>
#   to be similar to the current build output
# - Add a cache to the manual checks

# We use lowercase commands as advocated by the kde cmake coding style

include(CheckIncludeFile)
include(CheckCSourceCompiles)

# Implementation of AC_CHECK_HEADERS
# In addition, it also records the list of variables in the variable 
# 'autoheader_vars', and for each variable, a documentation string in the
# variable ${var}_doc
function(ac_check_headers headers)
  foreach (header ${ARGV})
	string(TOUPPER ${header} header_var)
	string(REPLACE "." "_" header_var ${header_var})
	string(REPLACE "/" "_" header_var ${header_var})
	set(header_var "HAVE_${header_var}")
	check_include_file (${header} ${header_var})
	set("${header_var}_doc" "Define to 1 if you have the <${header}> header file." PARENT_SCOPE)
	if (${header_var})
	  set("${header_var}_defined" "1" PARENT_SCOPE)
	endif()
	set("${header_var}_val" "1" PARENT_SCOPE)
	set (autoheader_vars ${autoheader_vars} ${header_var})
  endforeach()
  set (autoheader_vars ${autoheader_vars} PARENT_SCOPE)
endfunction()

function(ac_check_funcs funcs)
  foreach (func ${ARGV})
	string(TOUPPER ${func} var)
	set(var "HAVE_${var}")
	set(${var})
	check_function_exists (${func} ${var})
	set("${var}_doc" "Define to 1 if you have the '${func}' function." PARENT_SCOPE)
	if (${var})
	  set("${var}_defined" "1" PARENT_SCOPE)
	  set(${var} yes PARENT_SCOPE)
	endif()
	set("${var}_val" "1" PARENT_SCOPE)
	set (autoheader_vars ${autoheader_vars} ${var})
  endforeach()
  set (autoheader_vars ${autoheader_vars} PARENT_SCOPE)
endfunction()

# Implementation of AC_DEFINE
function(ac_define varname value doc)
  if (${varname} MATCHES ",")
	message(FATAL_ERROR ",")
  endif()
  set("${varname}_doc" ${doc} PARENT_SCOPE)
  set("${varname}_defined" 1 PARENT_SCOPE)
  set("${varname}_val" ${value} PARENT_SCOPE)
  set (autoheader_vars ${autoheader_vars} ${varname} PARENT_SCOPE)
endfunction()

# Implementation of AC_DEFINE_UNQUOTED
function(ac_define_unquoted varname value doc)
  if (${varname} MATCHES ",")
	message(FATAL_ERROR ",")
  endif()
  set("${varname}_doc" ${doc} PARENT_SCOPE)
  set("${varname}_defined" 1 PARENT_SCOPE)
  set("${varname}_val" \"${value}\" PARENT_SCOPE)
  set (autoheader_vars ${autoheader_vars} ${varname} PARENT_SCOPE)
endfunction()

Include(CheckTypeSize)

# Implementation of AC_CHECK_SIZEOF
# FIXME: cross compiling
function(ac_check_sizeof type)
  check_type_size(${type} size)
  if (HAVE_size STREQUAL "TRUE")
  else()
	message(FATAL_ERROR "Type ${type} not found.")
  endif()
  string(TOUPPER "SIZEOF_${type}" varname)
  string(REPLACE " " "_" varname ${varname})
  string(REPLACE "*" "P" varname ${varname})

  set("${varname}_doc" "The size of '${type}' as computed by sizeof" PARENT_SCOPE)
  set("${varname}_defined" 1 PARENT_SCOPE)
  set("${varname}_val" ${size} PARENT_SCOPE)
  set (autoheader_vars ${autoheader_vars} ${varname} PARENT_SCOPE)
endfunction()

# Implementation of autoheader
function(AUTOHEADER filename variables)
  set(tmp_filename "${filename}.tmp")
  file(WRITE ${tmp_filename} "")
  foreach(var ${${variables}})
	file(APPEND ${tmp_filename} "\n/* ${${var}_doc} */\n")
	if(${${var}_defined})
	  file(APPEND ${tmp_filename} "#define ${var} ${${var}_val}\n")
	else()
	  file(APPEND ${tmp_filename} "/* #undef ${var} */\n")
	endif()
  endforeach()
  # FIXME: This is unix specific
  execute_process(COMMAND diff ${filename} ${filename}.tmp RESULT_VARIABLE diff_res OUTPUT_QUIET)
  if (NOT diff_res STREQUAL 0)
	message(STATUS "generating ${filename}.")
	execute_process(COMMAND mv ${filename}.tmp ${filename})
  else()
	message(STATUS "${filename} is unchanged.")
  endif()
endfunction()

function(ac_msg_checking)
  message(STATUS "checking ${ARGV}...")
  set(last_msg_checking ${ARGV} PARENT_SCOPE)
endfunction()

function(ac_msg_result)
  message(STATUS "checking ${last_msg_checking}... ${ARGV}")
endfunction()

function(ac_msg_error)
  message(FATAL_ERROR "${ARGV}")
endfunction()

function(ac_msg_warn)
  message(STATUS "WARNING: ${ARGV}")
endfunction()

# The lines commented out using ### are the stuff from configure.in which still 
# need to be ported to cmake
# The svn revision of the configure.in used is r132691

set(VERSION 2.5)
ac_define_unquoted(VERSION ${VERSION} "Version number of package")
set(API_VER 1.0)
###
###AC_PROG_LN_S
###
#### In case of cygwin, override LN_S, irrespective of what it determines.
#### The build uses cygwin, but the actual runtime doesn't.
###case $host_os in
###*cygwin* ) set(LN_S 'cp -p';;)
###esac
###
###

#
# These variables are the CPPFLAGS/CFLAGS passed to libgc's configure
# libgc should inherit the original CFLAGS/CPPFLAGS passed to configure, i.e. -O0
#
set(CPPFLAGS_FOR_LIBGC ${CPPFLAGS})
set(CFLAGS_FOR_LIBGC ${CFLAGS})

#
# These are the flags that need to be stored in the mono.pc file for 
# compiling code that will embed Mono
#
set(libmono_cflags "")
set(libmono_ldflags "")

###AC_SUBST(libmono_cflags)
###AC_SUBST(libmono_ldflags)
###
#### Variable to have relocatable .pc files (lib, or lib64)
###set(reloc_libdir `basename ${libdir}`)
###AC_SUBST(reloc_libdir)

# if linker handles the version script
set(no_version_script no)

# Set to yes if Unix sockets cannot be created in an anonymous namespace
set(need_link_unlink no)

#
# Platform support
#

# Obtain the GNU style target
# From GetTargetTriple.cmake in LLVM
function( get_target_triple var )
  if( MSVC )
    set( ${var} "i686-pc-win32" PARENT_SCOPE )
  else( MSVC )
    set(config_guess config.guess)
    execute_process(COMMAND sh ${config_guess}
      RESULT_VARIABLE TT_RV
      OUTPUT_VARIABLE TT_OUT
      OUTPUT_STRIP_TRAILING_WHITESPACE)
    if( NOT TT_RV EQUAL 0 )
      message(FATAL_ERROR "Failed to execute ${config_guess}")
    endif( NOT TT_RV EQUAL 0 )
    set( ${var} ${TT_OUT} PARENT_SCOPE )
  endif( MSVC )
endfunction( get_target_triple var )

get_target_triple(host)

message(STATUS "checking host platform characteristics...")

set(libgc_threads no)
set(has_dtrace no)
set(parallel_mark yes)

if(host MATCHES .*-.*-linux.*)
  set(platform_win32 no)
  set(CPPFLAGS "${CPPFLAGS} -DHAVE_CONFIG_H -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP -DUSE_MUNMAP")
  set(libmono_cflags "-D_REENTRANT")
  set(libmono_ldflags "-lpthread")
  set(libdl "-ldl")
  set(libgc_threads pthreads)
  set(AOT_SUPPORTED "yes")
  set(use_sigposix yes)
else()
  message(FATAL_ERROR "The cmake build doesn't yet support host '${host}'.")
endif()

#### Thread configuration inspired by sleepycat's db
###case "$host" in
###	*-*-mingw*|*-*-cygwin*)
###		set(platform_win32 yes)
###		ac_define(PLATFORM_WIN32,1,[Platform is Win32])
###		ac_define(DISABLE_PORTABILITY,1,[Disable the io-portability layer])
###		ac_define(PLATFORM_NO_SYMLINKS,1,[This platform does not support symlinks])
###		if test "x$cross_compiling" = "xno"; then
###			set(CC "gcc -mno-cygwin -g")
###			# So libgc configure gets -mno-cygwin
###			export CC
###		fi
###		set(HOST_CC "gcc")
###		# Windows 2000 is required that includes Internet Explorer 5.01
###		set(CPPFLAGS "$CPPFLAGS -DWINVER=0x0500 -D_WIN32_WINNT=0x0500 -D_WIN32_IE=0x0501 -D_UNICODE -DUNICODE -DWIN32_THREADS -DFD_SETSIZE=1024")
###		set(libmono_cflags "-mno-cygwin -mms-bitfields -mwindows")
###		set(libmono_ldflags "-mno-cygwin -mms-bitfields -mwindows")
###		set(libdl )
###		set(libgc_threads win32)
###		set(gc_default included)
###		set(with_sigaltstack no)
###		set(LN_S cp)
###		# This forces libgc to use the DllMain based thread registration code on win32
###		set(libgc_configure_args "$libgc_configure_args --enable-win32-dllmain=yes")
###		;;
###	*-*-*netbsd*)
###		set(platform_win32 no)
###		set(CPPFLAGS "$CPPFLAGS -D_REENTRANT -DGC_NETBSD_THREADS -D_GNU_SOURCE")
###		set(libmono_cflags "-D_REENTRANT")
###		set(LDFLAGS "$LDFLAGS -pthread")
###		set(CPPFLAGS "$CPPFLAGS -DPLATFORM_BSD")
###		set(libmono_ldflags "-pthread")
###		set(need_link_unlink yes)
###		set(libdl "-ldl")
###		set(libgc_threads pthreads)
###		set(with_sigaltstack no)
###		set(use_sigposix yes)
###		;;
###	*-*-*freebsd*)
###		set(platform_win32 no)
###		if test "x$PTHREAD_CFLAGS" = "x"; then
###			set(CPPFLAGS "$CPPFLAGS -DGC_FREEBSD_THREADS")
###			set(libmono_cflags )
###		else
###			set(CPPFLAGS "$CPPFLAGS $PTHREAD_CFLAGS -DGC_FREEBSD_THREADS")
###			set(libmono_cflags "$PTHREAD_CFLAGS")
###		fi
###		if test "x$PTHREAD_LIBS" = "x"; then
###			set(LDFLAGS "$LDFLAGS -pthread")
###			set(libmono_ldflags "-pthread")
###		else
###			set(LDFLAGS "$LDFLAGS $PTHREAD_LIBS")
###			set(libmono_ldflags "$PTHREAD_LIBS")
###		fi
###		set(CPPFLAGS "$CPPFLAGS -DPLATFORM_BSD")
###		set(need_link_unlink yes)
###		ac_define(PTHREAD_POINTER_ID, 1, [pthread is a pointer])
###		set(libdl )
###		set(libgc_threads pthreads)
###		# This doesn't seem to work as of 7.0 on amd64
###		set(with_sigaltstack no)
#### TLS is only partially implemented on -CURRENT (compiler support
#### but NOT library support)
####
###		set(with_tls pthread)
###		set(use_sigposix yes)
###		;;
###	*-*-*openbsd*)
###		set(platform_win32 no)
###		set(CPPFLAGS "$CPPFLAGS -D_THREAD_SAFE -DGC_FREEBSD_THREADS -DPLATFORM_BSD")
###		set(libmono_cflags "-D_THREAD_SAFE")
###		set(LDFLAGS "$LDFLAGS -pthread")
###		set(libmono_ldflags "-pthread")
###		set(need_link_unlink yes)
###		ac_define(PTHREAD_POINTER_ID)
###		set(libdl )
###		set(libgc_threads pthreads)
###		set(use_sigposix yes)
###		;;
###	*-*-linux*)
###		set(platform_win32 no)
###		set(CPPFLAGS "$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP -DUSE_MUNMAP")
###		set(libmono_cflags "-D_REENTRANT")
###		set(libmono_ldflags "-lpthread")
###		set(libdl "-ldl")
###		set(libgc_threads pthreads)
###		set(AOT_SUPPORTED "yes")
###		set(use_sigposix yes)
###		;;
###	*-*-hpux*)
###	        set(platform_win32 no)
###		set(CPPFLAGS "$CPPFLAGS -DGC_HPUX_THREADS -D_HPUX_SOURCE -D_XOPEN_SOURCE_EXTENDED -D_REENTRANT")
###		# +ESdbgasm only valid on bundled cc on RISC
###		# silently ignored for ia64
###		if test $GCC != "yes"; then
###			set(CFLAGS "$CFLAGS +ESdbgasm")
###			# Arrange for run-time dereferencing of null
###			# pointers to produce a SIGSEGV signal.
###			set(LDFLAGS "$LDFLAGS -z")
###		fi
###		set(CFLAGS "$CFLAGS +ESdbgasm")
###		set(LDFLAGS "$LDFLAGS -z")
###		set(libmono_cflags "-D_REENTRANT")
###		set(libmono_ldflags "-lpthread")
###		set(libgc_threads pthreads)
###		set(need_link_unlink yes)
###		set(use_sigposix yes)
###		;;
###	*-*-solaris*)
###		set(platform_win32 no)
###		set(CPPFLAGS "$CPPFLAGS -DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DPLATFORM_SOLARIS")
###		set(need_link_unlink yes)
###		set(libmono_cflags "-D_REENTRANT")
###		set(libgc_threads pthreads)
###		# This doesn't seem to work on solaris/x86, but the configure test runs
###		set(with_tls pthread)
###		set(has_dtrace yes)
###		set(use_sigposix yes)
###		;;
###	*-*-darwin*)
###		set(parallel_mark "Disabled_Currently_Hangs_On_MacOSX")
###		set(platform_win32 no)
###		set(platform_darwin yes)
###		set(CPPFLAGS "$CPPFLAGS -no-cpp-precomp -D_THREAD_SAFE -DGC_MACOSX_THREADS -DPLATFORM_MACOSX -DUSE_MMAP -DUSE_MUNMAP")
###		set(CPPFLAGS "$CPPFLAGS -DGetCurrentProcess=MonoGetCurrentProcess -DGetCurrentThread=MonoGetCurrentThread -DCreateEvent=MonoCreateEvent")
###		set(libmono_cflags "-D_THREAD_SAFE")
###		set(LDFLAGS "$LDFLAGS -pthread")
###		set(libmono_ldflags "-pthread")
###		set(need_link_unlink yes)
###		ac_define(PTHREAD_POINTER_ID)
###		ac_define(USE_MACH_SEMA, 1, [...])
###		set(no_version_script yes)
###		set(libdl )
###		set(libgc_threads pthreads)
###		set(has_dtrace yes)
###		if test "x$cross_compiling" = "xyes"; then
###			set(has_broken_apple_cpp yes)
###		fi
###		;;
###	*)
###		AC_MSG_WARN([*** Please add $host to configure.in checks!])
###		set(platform_win32 no)
###		set(libdl "-ldl")
###		;;
###esac
###AC_MSG_RESULT(ok)
###
###if test x$need_link_unlink = xyes; then
###   ac_define(NEED_LINK_UNLINK, 1, [Define if Unix sockets cannot be created in an anonymous namespace])
###fi
###

if(use_sigposix)
  set(PLATFORM_SIGPOSIX 1)
endif()
if(platform_win32)
  set(PLATFORM_WIN32 yes)
endif()
if(${target_os} MATCHES "*linux*")
  set(PLATFORM_LINUX yes)
endif()
if(platform_darwin)
  set(PLATFORM_DARWIN yes)
endif()

include(CMakeDetermineASM-ATTCompiler)

find_program(BISON NAMES bison)

if(BISON STREQUAL "BISON-NOTFOUND")
  message(FATAL_ERROR "You need to install bison")
else()
  message(STATUS "Found bison: ${BISON}")
endif()

###AC_PROG_INSTALL
###AC_PROG_AWK
#### We should use AM_PROG_AS, but it's not available on automake/aclocal 1.4
###: ${set(CCAS '$(CC)'})
#### Set ASFLAGS if not already set.
###: ${set(CCASFLAGS '$(CFLAGS)'})
###AC_SUBST(CCAS)
###AC_SUBST(CCASFLAGS)
###
#### may require a specific autoconf version
#### AC_PROG_CC_FOR_BUILD
#### CC_FOR_BUILD not automatically detected
###set(CC_FOR_BUILD $CC)
###set(CFLAGS_FOR_BUILD $CFLAGS)
###set(BUILD_EXEEXT )
###if test "x$cross_compiling" = "xyes"; then
###	set(CC_FOR_BUILD cc)
###	set(CFLAGS_FOR_BUILD )
###	set(BUILD_EXEEXT "")
###fi
###AC_SUBST(CC_FOR_BUILD)
###AC_SUBST(CFLAGS_FOR_BUILD)
###AC_SUBST(HOST_CC)
###AC_SUBST(BUILD_EXEEXT)
###
###AM_CONDITIONAL(CROSS_COMPILING, [test x$cross_compiling = xyes])
###AM_CONDITIONAL(USE_BATCH_FILES, [test x$platform_win32 = xyes -a x$cross_compiling = xyes])
###
#### Set STDC_HEADERS
###AC_HEADER_STDC
###AC_LIBTOOL_WIN32_DLL
#### This causes monodis to not link correctly
####AC_DISABLE_FAST_INSTALL
###set(export_ldflags `(./libtool --config; echo eval echo \\$export_dynamic_flag_spec) | sh`)
###AC_SUBST(export_ldflags)
###
#### Test whenever ld supports -version-script
###AC_PROG_LD
###AC_PROG_LD_GNU
###if test "x$lt_cv_prog_gnu_ld" = "xno"; then
###   set(no_version_script yes)
###fi
###
###AM_CONDITIONAL(NO_VERSION_SCRIPT, test x$no_version_script = xyes)
###

ac_check_headers(unistd.h stdint.h sys/types.h)
ac_check_headers(sys/filio.h sys/sockio.h netdb.h utime.h sys/utime.h semaphore.h sys/un.h linux/rtc.h sys/syscall.h sys/mkdev.h)
ac_check_headers(sys/user.h sys/socket.h sys/ipc.h sys/sem.h sys/utsname.h alloca.h ucontext.h pwd.h)

ac_check_headers(zlib.h)
set(have_zlib ${HAVE_ZLIB_H})
if(have_zlib)
  set(compiles)
  check_c_source_compiles("
#include <zlib.h>
void main () {
#if defined(ZLIB_VERNUM) && (ZLIB_VERNUM >= 0x1230)
}
#else
#error No good zlib found
#endif
" compiles)
  if(compiles)
	ac_msg_result("Using system zlib")
	set(zlib_msg "system zlib")
  set(HAVE_ZLIB yes)
  else()
	ac_msg_result("Using embedded zlib")
	set(have_zlib no)
	set(zlib_msg "bundled zlib")
  endif()
endif()

if(have_zlib)
  set(HAVE_ZLIB yes)
endif()
ac_define(HAVE_ZLIB 1 "Have system zlib")

# for mono/metadata/debug-symfile.c
ac_check_headers(elf.h)

# for support
ac_check_headers(poll.h)
ac_check_headers(sys/poll.h)
ac_check_headers(sys/wait.h)
ac_check_headers(grp.h)
ac_check_headers(syslog.h)

# for mono/dis
ac_check_headers(wchar.h)
ac_check_headers(ieeefp.h)

# Check whenever using GCC
include(CheckCSourceCompiles)
include(CheckCCompilerFlag)
check_c_source_compiles("
#ifdef __GNUC__
#else
#error 1
#endif
void main () {}
" GCC)

if(NOT HAVE_ISINF)
  ac_msg_checking("for isinf")
  set(compiles)
  check_c_source_compiles("
#include <math.h>
void main () {
int f = isinf (1);
}
" compiles)
  if(compiles)
	ac_msg_result(yes)
	ac_define(HAVE_ISINF 1 "isinf available")
	set(HAVE_ISINF 1 CACHE BOOL "Have the isinf function")
  else()
	ac_msg_result(no)
  endif()
endif()

# not 64 bit clean in cross-compile
ac_check_sizeof("void *" 4)

set(WARN '')

if(GCC)
  set(WARN "-Wall -Wunused -Wmissing-prototypes -Wmissing-declarations -Wstrict-prototypes  -Wmissing-prototypes -Wnested-externs -Wpointer-arith -Wno-cast-qual -Wwrite-strings")
  # The runtime code does not respect ANSI C strict aliasing rules
  set(CFLAGS "${CFLAGS} -fno-strict-aliasing")
  CHECK_C_COMPILER_FLAG("-Wdeclaration-after-statement" HAS_WDECL_AFTER_STATEMENT)
  if(HAS_WDECL_AFTER_STATEMENT)
	set(WARN "${WARN} -Wdeclaration-after-statement")
  endif()
else()
	# The Sun Forte compiler complains about inline functions that access static variables
	# so disable all inlining.
###	case "$host" in
###	*-*-solaris*)
###		set(CFLAGS "$CFLAGS -Dinline=")
###		;;
###	esac
###fi
endif()

set(CFLAGS "${CFLAGS} -g ${WARN}")

###set(CFLAGS_FOR_LIBGC "$CFLAGS_FOR_LIBGC -g")
###

set(srcdir ${CMAKE_SOURCE_DIR})
set(top_srcdir ${CMAKE_SOURCE_DIR})
set(abs_top_srcdir ${top_srcdir})

# FIXME:
set(top_builddir ${CMAKE_BINARY_DIR})

# Where's the 'mcs' source tree?
if(EXISTS ${srcdir}/mcs)
  set(mcsdir mcs)
else()
  set(mcsdir ../mcs)
endif()

#
# A sanity check to catch cases where the package was unpacked
# with an ancient tar program (Solaris)
#
set(solaris-tar-check yes CACHE BOOL "Enable/disable solaris tar check")

if(solaris-tar-check)
  ac_msg_checking("integrity of package")
  if(EXISTS ${srcdir}/${mcsdir}/class/System.Runtime.Serialization.Formatters.Soap/System.Runtime.Serialization.Formatters.Soap/SoapTypeMapper.cs)
	ac_msg_result(ok)
  else()
	set(errorm "Your mono distribution is incomplete;  if unpacking from a tar file, make sure you use GNU tar;  see http://www.mono-project.com/IncompletePackage for more details")
	ac_msg_error(${errorm})
  endif()
endif()

set(mcs_topdir ${top_srcdir}/${mcsdir})
set(mcs_topdir_from_srcdir ${top_builddir}/${mcsdir})
###
##### Maybe should also disable if mcsdir is invalid.  Let's punt the issue for now.
###AM_CONDITIONAL(BUILD_MCS, [test x$cross_compiling = xno && test x$enable_mcs_build != xno])
###
###AC_SUBST([mcs_topdir])
###AC_SUBST([mcs_topdir_from_srcdir])
###
#### Where's the 'olive' source tree?
###if test -d $srcdir/olive; then
###  set(olivedir olive)
###else
###  set(olivedir ../olive)
###fi
###
###if test -d $srcdir/$olivedir; then
###set(olive_topdir '$(top_srcdir)/'$olivedir)
###fi
###
#### gettext: prepare the translation directories. 
#### we do not configure the full gettext, as we consume it dynamically from C#
###AM_PO_SUBDIRS
###
###if test "x$USE_NLS" = "xyes"; then
###   AC_CHECK_PROG(HAVE_MSGFMT, msgfmt,yes,no)
###
###   if test "x$HAVE_MSGFMT" = "xno"; then
###	  ac_msg_error([msgfmt not found. You need to install the 'gettext' package, or pass --enable-set(nls no to configure.]))
###   fi
###fi
###
set(libgdiplus installed CACHE STRING "=installed|sibling|<path> Override the libgdiplus used for System.Drawing tests (defaults to installed)")
set(with_libgdiplus ${libgdiplus})

###case $with_libgdiplus in
###no|installed) set(libgdiplus_loc  ;;)
###yes|sibling) set(libgdiplus_loc `cd ../libgdiplus && pwd`/src/libgdiplus.la ;;)
###/*) set(libgdiplus_loc $with_libgdiplus ;;)
###*) set(libgdiplus_loc `pwd`/$with_libgdiplus ;;)
###esac
###AC_SUBST([libgdiplus_loc])
###
###
###set(pkg_config_path )
###set(crosspkgdir, [  --with-set(crosspkgdir /path/to/pkg-config/dir      Change pkg-config dir to custom dir],)
###	if test x$with_crosspkgdir = "x"; then
###		if test -s $PKG_CONFIG_PATH; then
###			set(pkg_config_path $PKG_CONFIG_PATH)
###		fi
###	else
###		set(pkg_config_path $with_crosspkgdir)
###		set(PKG_CONFIG_PATH $pkg_config_path)
###		export PKG_CONFIG_PATH
###	fi
###)
###
###set([glib],
###	[  --with-set(glib embedded|system    Choose glib API: system or embedded (default to system)],)
###	[], [set(with_glib system]))
###
###set(eglib_dir )
###

include(FindPkgConfig)

# FIXME:
set(with_glib "system")
if (${with_glib} STREQUAL "system")
  ###  if test "x$cross_compiling" = "xyes"; then
  ###    set(pkg_config_path "$PKG_CONFIG_PATH")
  ###    unset PKG_CONFIG_PATH
  ###  fi
  PKG_CHECK_MODULES(GLIB2 REQUIRED glib-2.0 gthread-2.0)
  set(BUILD_GLIB_CFLAGS ${GLIB2_CFLAGS})
  set(BUILD_GLIB_LIBS ${GLIB2_LIBRARIES})

  ###  if test "x$cross_compiling" = "xyes"; then
  ###    set(PKG_CONFIG_PATH $pkg_config_path)
  ###    export PKG_CONFIG_PATH
  ###  fi
  ###  
  ## Versions of dependencies
  set(GLIB_REQUIRED_VERSION 2.4.0)

  PKG_CHECK_MODULES(GLIB2 REQUIRED glib-2.0 >= ${GLIB_REQUIRED_VERSION} gthread-2.0)
  set(GLIB_CFLAGS ${GLIB2_CFLAGS})
  set(GLIB_LIBS ${GLIB2_LIBRARIES})
  PKG_CHECK_MODULES(GMODULE REQUIRED gmodule-2.0)
  set(GMODULE_CFLAGS ${GMODULE_CFLAGS})
  set(GMODULE_LIBS ${GMODULE_LIBRARIES})
endif()

###case $with_glib in
###embedded) 
###  set(GLIB_CFLAGS '-I$(top_srcdir)/eglib/src -I$(top_builddir)/eglib/src')
###  set(GLIB_LIBS '-L$(top_builddir)/eglib/src -leglib -lm')
###  set(BUILD_GLIB_CFLAGS "$GLIB_CFLAGS")
###  set(BUILD_GLIB_LIBS "$GLIB_LIBS")
###  set(GMODULE_CFLAGS "$GLIB_CFLAGS")
###  set(GMODULE_LIBS "$GLIB_LIBS")
###  set(eglib_dir eglib)
###  AC_CONFIG_SUBDIRS(eglib)
###  ;;
###*)
###  ac_msg_error([Invalid argument to --with-glib.])
###esac
if(with_glib STREQUAL "embedded")
  set(EGLIB_BUILD yes)
endif()
###  
###AC_SUBST(GLIB_CFLAGS)
###AC_SUBST(GLIB_LIBS)
###AC_SUBST(GMODULE_CFLAGS)
###AC_SUBST(GMODULE_LIBS)
###AC_SUBST(BUILD_GLIB_CFLAGS)
###AC_SUBST(BUILD_GLIB_LIBS)
###AC_SUBST(eglib_dir)
###
###if test x$cross_compiling$platform_win32 = xnoyes; then
###   ac_msg_checking(for cygwin glib2-dev package)
###   if [ cygcheck --f /usr/lib/libglib-2.0.dll.a | grep -q glib2-devel ]; then
###      ac_msg_result(found)
###	  ac_msg_error([Mono cannot be built with the cygwin glib2-devel package installed, because that package doesn't work with -mno-cygwin. Please uninstall it then re-run configure.])
###   else
###      ac_msg_result(not found, ok)
###   fi
###
###   ac_msg_checking(for broken gwin32.h)
###   set(glib_include `$PKG_CONFIG --cflags-only-I glib-2.0 | sed -e 's/ -I.*//g' | sed -e 's/-I//g'`)
###   if test -f $glib_include/glib/gwin32.h; then
###	  if [ grep ftruncate $glib_include/glib/gwin32.h | grep -q define ]; then
###		 ac_msg_result(failed)
###		 set(hashmark '#')
###		 ac_msg_error([Your version of gwin32.h is broken and will cause compilation errors when building mono. Please fix it by deleting the line: '$hashmark   define ftruncate...' from '$glib_include/glib/gwin32.h' then re-run configure.])
###	  fi
###   fi
###   ac_msg_result(ok)
###fi

# Enable support for fast thread-local storage
# Some systems have broken support, so we allow to disable it.
set(tls __thread CACHE STRING "Select Thread Local Storage implementation. Possible values are '__thread_' and 'pthread' (defaults to __thread)")
set(with_tls ${tls})

# Enable support for using sigaltstack for SIGSEGV and stack overflow handling
# This does not work on some platforms (bug #55253)
set(sigaltstack yes CACHE BOOL "Enable/disable support for sigaltstack (defaults to yes)")
set(with_sigaltstack ${sigaltstack})

set(static_mono yes CACHE BOOL "Link mono statically to libmono (faster) (defaults to yes)")
set(with_static_mono ${static_mono})
###
###if test "x$enable_static" = "xno"; then
###   set(with_static_mono no)
###fi
###
###if test "x$platform_win32" = "xyes"; then
###   # Boehm GC requires the runtime to be in its own dll
###   set(with_static_mono no)
###fi
###
if(with_static_mono)
  set(STATIC_MONO yes)
endif()
set(enable_mcs_build yes)
###AC_ARG_ENABLE(mcs-build, [  --disable-mcs-build disable the build of the mcs directory], set(try_mcs_build $enableval, enable_mcs_build=yes))
###
###set(xen_opt,   [  --with-set(xen_opt yes,no          Enable Xen-specific behaviour (defaults to yes)],[],[with_xen_opt=yes]))
###if test "x$with_xen_opt" = "xyes"; then
###	ac_define(MONO_XEN_OPT, 1, [Xen-specific behaviour])
###	set(ORIG_CFLAGS $CFLAGS)
###	set(CFLAGS "$CFLAGS -mno-tls-direct-seg-refs")
###	ac_msg_checking(for -mno-tls-direct-seg-refs option to gcc)
###	AC_TRY_COMPILE([], [
###					   void main () { }
###	], [
###	   ac_msg_result(yes)
###	   # Pass it to libgc as well
###	   set(CFLAGS_FOR_LIBGC "$CFLAGS_FOR_LIBGC -mno-tls-direct-seg-refs")
###	], [
###	   ac_msg_result(no)
###	   set(CFLAGS $ORIG_CFLAGS)
###	])
###fi

set (quiet-build yes CACHE BOOL "Enable quiet runtime build (on by default)")

set(DISABLED_FEATURES none)

###AC_ARG_ENABLE(minimal, [  --enable-set(minimal LIST      drop support for LIST subsystems.)
###     LIST is a comma-separated list from: aot, profiler, decimal, pinvoke, debug,
###     reflection_emit, reflection_emit_save, large_code, logging, com, ssa, generics, attach, jit, simd.],
###[
###	for feature in `echo "$enable_minimal" | sed -e "s/,/ /g"`; do
###		eval "mono_feature_disable_$set(feature 'yes'")
###		AC_MSG_NOTICE([Disabled support for feature: $feature])
###	done
###	set(DISABLED_FEATURES $enable_minimal)
###	set(disabled "Disabled:    $enable_minimal")
###],[])
###
ac_define_unquoted(DISABLED_FEATURES "${DISABLED_FEATURES}" "String of disabled features")
###
###if test "x$mono_feature_disable_aot" = "xyes"; then
###	ac_define(DISABLE_AOT, 1, [Disable AOT support])
###fi
###
###if test "x$mono_feature_disable_profiler" = "xyes"; then
###	ac_define(DISABLE_PROFILER, 1, [Disable default profiler support])
###fi
###AM_CONDITIONAL(DISABLE_PROFILER, test x$mono_feature_disable_profiler = xyes)
###
###if test "x$mono_feature_disable_decimal" = "xyes"; then
###	ac_define(DISABLE_DECIMAL, 1, [Disable System.Decimal support])
###fi
###
###if test "x$mono_feature_disable_pinvoke" = "xyes"; then
###	ac_define(DISABLE_PINVOKE, 1, [Disable P/Invoke support])
###fi
###
###if test "x$mono_feature_disable_debug" = "xyes"; then
###	ac_define(DISABLE_DEBUG, 1, [Disable runtime debugging support])
###fi
###
###if test "x$mono_feature_disable_reflection_emit" = "xyes"; then
###	ac_define(DISABLE_REFLECTION_EMIT, 1, [Disable reflection emit support])
###	set(mono_feature_disable_reflection_emit_save yes)
###fi
###
###if test "x$mono_feature_disable_reflection_emit_save" = "xyes"; then
###	ac_define(DISABLE_REFLECTION_EMIT_SAVE, 1, [Disable assembly saving support in reflection emit])
###fi
###
###if test "x$mono_feature_disable_large_code" = "xyes"; then
###	ac_define(DISABLE_LARGE_CODE, 1, [Disable support for huge assemblies])
###fi
###
###if test "x$mono_feature_disable_logging" = "xyes"; then
###	ac_define(DISABLE_LOGGING, 1, [Disable support debug logging])
###fi
###
###if test "x$mono_feature_disable_com" = "xyes"; then
###	ac_define(DISABLE_COM, 1, [Disable COM support])
###fi
###
###if test "x$mono_feature_disable_ssa" = "xyes"; then
###	ac_define(DISABLE_SSA, 1, [Disable advanced SSA JIT optimizations])
###fi
###
###if test "x$mono_feature_disable_generics" = "xyes"; then
###	ac_define(DISABLE_GENERICS, 1, [Disable generics support])
###fi
###
###if test "x$mono_feature_disable_attach" = "xyes"; then
###	ac_define(DISABLE_ATTACH, 1, [Disable agent attach support])
###fi
###
###if test "x$mono_feature_disable_jit" = "xyes"; then
###	ac_define(DISABLE_JIT, 1, [Disable the JIT, only full-aot mode will be supported by the runtime.])
###fi
###
###AM_CONDITIONAL(DISABLE_JIT, test x$mono_feature_disable_jit = xyes)
###
###if test "x$mono_feature_disable_simd" = "xyes"; then
###	ac_define(DISABLE_SIMD, 1, [Disable SIMD intrinsics related optimizations.])
###fi
###
###ac_msg_checking(for visibility __attribute__)
###AC_TRY_COMPILE([], [
###   void __attribute__ ((visibility ("hidden"))) doit (void) {}
###   void main () { doit (); }
###], [
###   set(have_visibility_hidden yes)
###   ac_msg_result(yes)
###], [
###   set(have_visibility_hidden no)
###   ac_msg_result(no)
###])
###

#
# libgc checks
#

set(gc_headers no)
set(gc included)
set(use_included_gc no)
set(libgc_configure_args)
set(gc_default included)

set(gc ${gc_default} CACHE STRING "The GC library to use (defaults to included)")
set(with_gc ${gc})

# FIXME:
set(enable_parallel_mark yes)
###AC_ARG_ENABLE(parallel-mark, [  --enable-parallel-mark     Enables GC Parallel Marking], set(enable_parallel_mark $enableval, enable_parallel_mark=$parallel_mark))
###if test x$enable_parallel_mark = xyes; then
###	set(libgc_configure_args "$libgc_configure_args --enable-parallel-mark")
###fi
###
set(LIBGC_CFLAGS )
set(LIBGC_LIBS )
set(LIBGC_STATIC_LIBS )
set(libgc_dir )

if (gc STREQUAL included)
  set(found_boehm yes)
  set(gc_headers yes)
  set(use_included_gc yes)
  set(libgc_dir libgc)

  set(LIBGC_CFLAGS '-I${top_srcdir}/libgc/include')
  set(LIBGC_LIBS '${top_builddir}/libgc/libmonogc.la')
  set(LIBGC_STATIC_LIBS '${top_builddir}/libgc/libmonogc-static.la')

  ac_define(HAVE_BOEHM_GC 1 "Have Boehm GC")
###  AC_SUBST(HAVE_BOEHM_GC)

  ac_define(HAVE_GC_H 1 "Have gc.h")
  ac_define(USE_INCLUDED_LIBGC 1 "Use included libgc")

  # The included libgc contains GCJ support
  ac_define(HAVE_GC_GCJ_MALLOC 1 "Have GC_gcj_malloc")
  ac_define(HAVE_GC_ENABLE 1 "Have GC_enable")
  if (enable_parallel_mark STREQUAL yes)
	ac_define_unquoted(USED_GC_NAME "Included Boehm (with typed GC and Parallel Mark)" "GC description")
  else()
	ac_define_unquoted(USED_GC_NAME "Included Boehm (with typed GC)" "GC description")
  endif()
endif()
  
###case "x$gc" in
###	xboehm|xbohem|xyes)
###		ac_check_headers(gc.h gc/gc.h, set(gc_headers yes))
###		AC_CHECK_LIB(gc, GC_malloc, set(found_boehm "yes",,$libdl))
###
###		if test "x$found_boehm" != "xyes"; then
###			ac_msg_error("GC requested but libgc not found! Install libgc or run configure with --with-set(gc none."))
###		fi
###		if test "x$gc_headers" != "xyes"; then
###			ac_msg_error("GC requested but header files not found! You may need to install them by hand.")
###		fi
###
###		ac_define(HAVE_BOEHM_GC, 1, [Have Boehm GC])
###		AC_SUBST(HAVE_BOEHM_GC)
###		set(LIBGC_LIBS "-lgc $libdl")
###		set(LIBGC_STATIC_LIBS "$LIBGC_LIBS")
###
###		# ac_check_funcs does not work for some reason...
###		AC_CHECK_LIB(gc, GC_gcj_malloc, set(found_gcj_malloc "yes",,$libdl))
###		if test "x$found_gcj_malloc" = "xyes"; then
###			ac_define(HAVE_GC_GCJ_MALLOC, 1, [Have GC_gcj_malloc])
###			ac_define_unquoted(USED_GC_NAME, "System Boehm (with typed GC)", [GC description])
###		else
###			ac_define_unquoted(USED_GC_NAME, "System Boehm (no typed GC)", [GC description])
###		fi
###		AC_CHECK_LIB(gc, GC_enable, set(found_gc_enable "yes",,$libdl))
###		if test "x$found_gc_enable" = "xyes"; then
###			ac_define(HAVE_GC_ENABLE, 1, [Have 'GC_enable'])
###		fi
###		;;
###
###	xincluded)
###		set(found_boehm yes)
###		set(gc_headers yes)
###		set(use_included_gc yes)
###		set(libgc_dir libgc)
###
###		set(LIBGC_CFLAGS '-I$(top_srcdir)/libgc/include')
###		set(LIBGC_LIBS '$(top_builddir)/libgc/libmonogc.la')
###		set(LIBGC_STATIC_LIBS '$(top_builddir)/libgc/libmonogc-static.la')
###
###		ac_define(HAVE_BOEHM_GC, 1, [Have Boehm GC])
###		AC_SUBST(HAVE_BOEHM_GC)
###
###		ac_define(HAVE_GC_H, 1, [Have gc.h])
###		ac_define(USE_INCLUDED_LIBGC, 1, [Use included libgc])
###
###		# The included libgc contains GCJ support
###		ac_define(HAVE_GC_GCJ_MALLOC, 1, [Have GC_gcj_malloc])
###		ac_define(HAVE_GC_ENABLE, 1, [Have GC_enable])
###		if test x$enable_parallel_mark = xyes; then
###			ac_define_unquoted(USED_GC_NAME, "Included Boehm (with typed GC and Parallel Mark)", [GC description])
###		else
###			ac_define_unquoted(USED_GC_NAME, "Included Boehm (with typed GC)", [GC description])
###		fi
###		;;
###
###	xsgen)
###		set(found_boehm no)
###		set(gc_headers no)
###		set(use_included_gc no)
###		ac_define(HAVE_SGEN_GC,1,[Using the simple generational GC.])
###		ac_define(HAVE_MOVING_COLLECTOR,1,[The GC can move objects.])
###		ac_define(HAVE_WRITE_BARRIERS,1,[The GC needs write barriers.])
###		ac_define_unquoted(USED_GC_NAME, "Simple generational", [GC description])
###		;;
###
###	xnone)
###		AC_MSG_WARN("Compiling mono without GC.")
###		ac_define_unquoted(USED_GC_NAME, "none", [GC description])
###		ac_define(HAVE_NULL_GC,1,[No GC support.])
###		;;
###	*)
###		ac_msg_error([Invalid argument to --with-gc.])
###		;;
###esac
###
###set(large-heap, [  --with-large-set(heap yes,no       Enable support for GC heaps larger than 3GB (defaults to no)], [large_heap=$withval], [large_heap=no]))
###if test "x$large_heap" = "xyes"; then
###   echo "FOO"
###   set(CPPFLAGS "$CPPFLAGS -DLARGE_CONFIG")
###fi
###
###AM_CONDITIONAL(INCLUDED_LIBGC, test x$use_included_gc = xyes)
###AC_SUBST(LIBGC_CFLAGS)
###AC_SUBST(LIBGC_LIBS)
###AC_SUBST(LIBGC_STATIC_LIBS)
###AC_SUBST(libgc_dir)
###
#
# End of libgc checks
#

include(CheckFunctionExists)
include(CheckLibraryExists)

if(platform_win32 STREQUAL no)

###
	# hires monotonic clock support
###	AC_SEARCH_LIBS(clock_gettime, rt)

  check_function_exists (dlopen dlopen_found)
  if (dlopen_found)
	set(DL_LIB "")
  else()
	check_library_exists (-ldl dlopen "" libdl_found)
	if (libdl_found)
	  set(DL_LIB "-ldl")
	else()
	  set(dl_support no)
	endif()
  endif()

  if (dl_support STREQUAL no)
	# FIXME:
	###		AC_MSG_WARN([No dynamic loading support available])
  else()
	set(LIBS ${LIBS} ${DL_LIB})
	ac_define(HAVE_DL_LOADER 1 "dlopen-based dynamic loader available")
	
###		# from glib's configure.in
###		AC_CACHE_CHECK([for preceeding underscore in symbols],
###			mono_cv_uscore,[
###			AC_TRY_RUN([#include <dlfcn.h>
###			int mono_underscore_test (void) { return 42; }
###			int main() {
###			  void *f1 = (void*)0, *f2 = (void*)0, *handle;
###			  handle = dlopen ((void*)0, 0);
###			  if (handle) {
###			    f1 = dlsym (handle, "mono_underscore_test");
###			    f2 = dlsym (handle, "_mono_underscore_test");
###			  } return (!f2 || f1);
###			}],
###				[set(mono_cv_uscore yes],)
###				[set(mono_cv_uscore no],)
###			[])
###		])
###		if test "x$mono_cv_uscore" = "xyes"; then
###			set(MONO_DL_NEED_USCORE 1)
###		else
###			set(MONO_DL_NEED_USCORE 0)
###		fi
###		AC_SUBST(MONO_DL_NEED_USCORE)
###		AC_CHECK_FUNC(dlerror)
    endif()

	# ******************************************************************
	# *** Checks for the IKVM JNI interface library                  ***
	# ******************************************************************
	set(ikvm-native yes CACHE BOOL "Build the IKVM JNI interface library (defaults to yes)")
	set(with_ikvm_native ${ikvm-native})
	set(ikvm_native_dir )
	if(with_ikvm_native)
	  set(ikvm_native_dir ikvm-native)
	  set(jdk_headers_found "IKVM Native")
	endif()

	ac_check_headers(execinfo.h)

	ac_check_funcs(getgrgid_r)
	ac_check_funcs(getgrnam_r)
	ac_check_funcs(getpwnam_r)
	ac_check_funcs(getpwuid_r)
	ac_check_funcs(getresuid)
	ac_check_funcs(setresuid)
	ac_check_funcs(kqueue)
	ac_check_funcs(backtrace_symbols)
	ac_check_funcs(mkstemp)
	ac_check_funcs(mmap)
	ac_check_funcs(madvise)
	ac_check_funcs(getrusage)
	ac_check_funcs(getpriority)
	ac_check_funcs(setpriority)

	ac_check_funcs(sched_setaffinity)

	# ******************************************************************
	# *** Check for large file support                               ***
	# *** (If we were using autoconf 2.50 we'd use AC_SYS_LARGEFILE) ***
	# ******************************************************************
###	
	# Check that off_t can represent 2**63 - 1 correctly, working around
	# potential compiler bugs.  Defines LARGE_FILE_SUPPORT, adds $1 to
	# CPPFLAGS and sets $large_offt to yes if the test succeeds
###	set(large_offt no)
###	AC_DEFUN([LARGE_FILES], [
###		set(large_CPPFLAGS $CPPFLAGS)
###		set(CPPFLAGS "$CPPFLAGS $1")
###		AC_TRY_RUN([
###			#include <sys/types.h>
###
###			#define BIG_OFF_T (((off_t)1<<62)-1+((off_t)1<<62))
###
###			int main(void) {
###				int set(big_off_t ((BIG_OFF_T%2147483629==721) &&)
###					       (BIG_OFF_T%set(2147483647 =1));)
###				if(big_off_t) {
###					exit(0);
###				} else {
###					exit(1);
###				}
###			}
###		], [
###			ac_msg_result(ok)
###			ac_define(HAVE_LARGE_FILE_SUPPORT, 1, [Have large file support])
###			set(large_CPPFLAGS "$large_CPPFLAGS $1")
###			set(large_offt yes)
###		], [
###			ac_msg_result(no)
###		], "")
###		set(CPPFLAGS $large_CPPFLAGS)
###	])
###
###	ac_msg_checking(if off_t is 64 bits wide)
###	LARGE_FILES("")
###	if test $large_offt = no; then
###		ac_msg_checking(if set(_FILE_OFFSET_BITS 64 gives 64 bit off_t))
###		LARGE_FILES("-set(D_FILE_OFFSET_BITS 64"))
###	fi
###	if test $large_offt = no; then
###		AC_MSG_WARN([No 64 bit file size support available])
###	fi
###	
	# *****************************
	# *** Checks for libsocket  ***
	# *****************************
###	AC_CHECK_LIB(socket, socket, set(LIBS "$LIBS -lsocket"))
###
	# *******************************
	# *** Checks for MSG_NOSIGNAL ***
	# *******************************
###	ac_msg_checking(for MSG_NOSIGNAL)
###	AC_TRY_COMPILE([#include <sys/socket.h>], [
###		int f = MSG_NOSIGNAL;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_MSG_NOSIGNAL, 1, [Have MSG_NOSIGNAL])
###	], [
###		# We'll have to use signals
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for SOL_IP     ***
	# *****************************
###	ac_msg_checking(for SOL_IP)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = SOL_IP;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_SOL_IP, 1, [Have SOL_IP])
###	], [
###		# We'll have to use getprotobyname
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for SOL_IPV6     ***
	# *****************************
###	ac_msg_checking(for SOL_IPV6)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = SOL_IPV6;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_SOL_IPV6, 1, [Have SOL_IPV6])
###	], [
###		# We'll have to use getprotobyname
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for SOL_TCP    ***
	# *****************************
###	ac_msg_checking(for SOL_TCP)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = SOL_TCP;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_SOL_TCP, 1, [Have SOL_TCP])
###	], [
###		# We'll have to use getprotobyname
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for IP_PKTINFO ***
	# *****************************
###	ac_msg_checking(for IP_PKTINFO)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = IP_PKTINFO;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_IP_PKTINFO, 1, [Have IP_PKTINFO])
###	], [
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for IPV6_PKTINFO ***
	# *****************************
###	ac_msg_checking(for IPV6_PKTINFO)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = IPV6_PKTINFO;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_IPV6_PKTINFO, 1, [Have IPV6_PKTINFO])
###	], [
###		ac_msg_result(no)
###	])
###
	# **********************************
	# *** Checks for IP_DONTFRAGMENT ***
	# **********************************
###	ac_msg_checking(for IP_DONTFRAGMENT)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = IP_DONTFRAGMENT;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_IP_DONTFRAGMENT, 1, [Have IP_DONTFRAGMENT])
###	], [
###		ac_msg_result(no)
###	])
###
	# **********************************
	# *** Checks for IP_MTU_DISCOVER ***
	# **********************************
###	ac_msg_checking(for IP_MTU_DISCOVER)
###	AC_TRY_COMPILE([#include <netdb.h>], [
###		int level = IP_MTU_DISCOVER;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_IP_MTU_DISCOVER, 1, [Have IP_MTU_DISCOVER])
###	], [
###		ac_msg_result(no)
###	])
###
### 	# *********************************
	# *** Check for struct ip_mreqn ***
	# *********************************
###	ac_msg_checking(for struct ip_mreqn)
###	AC_TRY_COMPILE([#include <netinet/in.h>], [
###		struct ip_mreqn mreq;
###		mreq.imr_address.s_addr = 0;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_STRUCT_IP_MREQN, 1, [Have struct ip_mreqn])
###	], [
###		# We'll just have to try and use struct ip_mreq
###		ac_msg_result(no)
###		ac_msg_checking(for struct ip_mreq)
###		AC_TRY_COMPILE([#include <netinet/in.h>], [
###			struct ip_mreq mreq;
###			mreq.imr_interface.s_addr = 0;
###		], [
###			# Yes, we have it...
###			ac_msg_result(yes)
###			ac_define(HAVE_STRUCT_IP_MREQ, 1, [Have struct ip_mreq])
###		], [
###			# No multicast support
###			ac_msg_result(no)
###		])
###	])
###	
	# **********************************
	# *** Check for gethostbyname2_r ***
	# **********************************
###	ac_msg_checking(for gethostbyname2_r)
###		AC_TRY_LINK([#include <netdb.h>], [
###		gethostbyname2_r(NULL,0,NULL,NULL,0,NULL,NULL);
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_GETHOSTBYNAME2_R, 1, [Have gethostbyname2_r])
###	], [
###		ac_msg_result(no)
###	])
###
	# *****************************
	# *** Checks for libnsl     ***
	# *****************************
###	AC_CHECK_FUNC(gethostbyaddr, , AC_CHECK_LIB(nsl, gethostbyaddr, set(LIBS "$LIBS -lnsl")))

ac_check_funcs(inet_pton inet_aton)

# ***********************************************
# *** Checks for size of sockaddr_un.sun_path ***
# ***********************************************
# FIXME: cache
ac_msg_checking("size of sockaddr_un.sun_path")

FILE(WRITE "${CMAKE_BINARY_DIR}/${CMAKE_FILES_DIRECTORY}/CMakeTmp/conftest.c"
"
			#include <sys/types.h>
			#include <stdio.h>
			#include <sys/un.h>

			int main(void) {
				struct sockaddr_un sock_un;
				printf(\"%d\\n\", sizeof(sock_un.sun_path));
				exit(0);
			}
")

TRY_RUN(run_res run_compiled
  ${CMAKE_BINARY_DIR}
  ${CMAKE_BINARY_DIR}/${CMAKE_FILES_DIRECTORY}/CMakeTmp/conftest.c
  RUN_OUTPUT_VARIABLE output)

set(mono_cv_sizeof_sunpath 0)
if(run_compiled)
  if (run_res EQUAL 0)
	string(REGEX MATCH "[0-9]+" mono_cv_sizeof_sunpath ${output})
  endif()
endif()

ac_msg_result(${mono_cv_sizeof_sunpath})
ac_define(MONO_SIZEOF_SUNPATH ${mono_cv_sizeof_sunpath} "Sizeof sock_un.sun_path")
###
	# *************************************
	# *** Checks for zero length arrays ***
	# *************************************
###	ac_msg_checking(whether $CC supports zero length arrays)
###	AC_TRY_COMPILE([
###                struct s {
###                        int  length;
###                        char data [0];
###                };
###        ], [], [
###		ac_msg_result(yes)
###		ac_define_unquoted(MONO_ZERO_ARRAY_LENGTH, 0, [Length of zero length arrays])
###	], [
###		ac_msg_result(no)
###		ac_define_unquoted(MONO_ZERO_ARRAY_LENGTH, 1, [Length of zero length arrays])
###	])
###
	# *****************************
	# *** Checks for libxnet    ***
	# *****************************
###	case "${host}" in
###		*solaris* )
###			ac_msg_checking(for Solaris XPG4 support)
###			if test -f /usr/lib/libxnet.so; then
###				set(CPPFLAGS "$CPPFLAGS -D_XOPEN_SOURCE=500")
###	    			set(CPPFLAGS "$CPPFLAGS -D__EXTENSIONS__")
###				set(CPPFLAGS "$CPPFLAGS -D_XOPEN_SOURCE_EXTENDED=1")
###           			set(LIBS "$LIBS -lxnet")
###  	    			ac_msg_result(yes)
###			else
###  	    			ac_msg_result(no)
###			fi
###
###			if test "$GCC" = "yes"; then
###		    		set(CFLAGS "$CFLAGS -Wno-char-subscripts")
###			fi
###    		;;
###	esac
###
	# *****************************
	# *** Checks for libpthread ***
	# *****************************
# on FreeBSD -STABLE, the pthreads functions all reside in libc_r
# and libpthread does not exist
#
###	case "${host}" in
###		*-*-*freebsd*)
###			AC_CHECK_LIB(pthread, main, set(LIBS "$LIBS -pthread"))
###		;;
###		*)
###			AC_CHECK_LIB(pthread, main, set(LIBS "$LIBS -lpthread"))
###		;;
###	esac
ac_check_headers(pthread.h)
###	ac_check_funcs(pthread_mutex_timedlock)
###	ac_check_funcs(pthread_getattr_np pthread_attr_get_np)
###	ac_msg_checking(for PTHREAD_MUTEX_RECURSIVE)
###	AC_TRY_COMPILE([ #include <pthread.h>], [
###		pthread_mutexattr_t attr;
###		pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
###	], [
###		ac_msg_result(ok)
###	], [
###		ac_msg_result(no)
###		AC_MSG_WARN(Using mono_mutex_t for recursive mutexes)
###		ac_define(USE_MONO_MUTEX, 1, [Use mono_mutex_t])
###	])
###	ac_check_funcs(pthread_attr_setstacksize)
###	ac_check_funcs(pthread_attr_getstack)
###	ac_check_funcs(pthread_get_stacksize_np pthread_get_stackaddr_np)
###
	# ***********************************
	# *** Checks for working __thread ***
	# ***********************************
###	ac_msg_checking(for working __thread)
###	if test "x$with_tls" != "x__thread"; then
###		ac_msg_result(disabled)
###	else
###		AC_TRY_RUN([
###			#include <pthread.h>
###			__thread int i;
###			static int res1, res2;
###
###			void thread_main (void *arg)
###			{
###				i = arg;
###				sleep (1);
###				if (arg == 1)
###					res1 = (i == arg);
###				else
###					res2 = (i == arg);
###			}
###
###			int main () {
###				pthread_t t1, t2;
###
###				i = 5;
###
###				pthread_create (&t1, NULL, thread_main, 1);
###				pthread_create (&t2, NULL, thread_main, 2);
###
###				pthread_join (t1, NULL);
###				pthread_join (t2, NULL);
###
###				return !(res1 + res2 == 2);
###			}
###		], [
###				ac_msg_result(yes)
###		], [
###				ac_msg_result(no)
###				set(with_tls pthread)
###		])
###	fi
###
	# **************************************
	# *** Checks for working sigaltstack ***
	# **************************************
###	ac_msg_checking(for working sigaltstack)
###	if test "x$with_sigaltstack" != "xyes"; then
###		ac_msg_result(disabled)
###	else
###		AC_TRY_RUN([
###			#include <stdio.h>
###			#include <stdlib.h>
###			#include <unistd.h>
###			#include <signal.h>
###			#include <pthread.h>
###			#include <sys/wait.h>
###			#if defined(__FreeBSD__) || defined(__NetBSD__)
###			#define SA_STACK SA_ONSTACK
###			#endif
###			static void
###			sigsegv_signal_handler (int _dummy, siginfo_t *info, void *context)
###			{
###				exit (0);
###			}
###
###			static void *
###			loop (void *ignored)
###			{
###				char *ptr = NULL;
###
###				*ptr = 0;
###				return NULL;
###			}
###
###			static void
###			child ()
###			{
###				struct sigaction sa;
###				struct sigaltstack sas;
###				pthread_t id;
###				pthread_attr_t attr;
###
###				sa.sa_sigaction = sigsegv_signal_handler;
###				sigemptyset (&sa.sa_mask);
###				sa.sa_flags = SA_SIGINFO | SA_ONSTACK;
###				if (sigaction (SIGSEGV, &sa, NULL) == -1) {
###					perror ("sigaction");
###					return;
###				}
###
###				sas.ss_sp = malloc (SIGSTKSZ);
###				sas.ss_size = SIGSTKSZ;
###				sas.ss_flags = 0;
###				if (sigaltstack (&sas, NULL) == -1) {
###					perror ("sigaltstack");
###					return;
###				}
###
###				pthread_attr_init (&attr);
###				if (pthread_create(&id, &attr, loop, &attr) != 0) {
###					printf ("pthread_create\n");
###					return;
###				}
###
###				sleep (100);
###			}
###
###			int
###			main ()
###			{
###				pid_t son;
###				int status;
###				int i;
###
###				son = fork ();
###				if (son == -1) {
###					return 1;
###				}
###
###				if (son == 0) {
###					child ();
###					return 0;
###				}
###
###				for (i = 0; i < 3; ++i) {
###					sleep (1);
###					waitpid (son, &status, WNOHANG);
###					if (WIFEXITED (status) && WEXITSTATUS (status) == 0)
###						return 0;
###				}
###
###				kill (son, SIGKILL);
###				return 1;
###			}
###
###		], [
###				ac_msg_result(yes)
###				ac_define(HAVE_WORKING_SIGALTSTACK, 1, [Have a working sigaltstack])
###		], [
###				set(with_sigaltstack no)
###				ac_msg_result(no)
###		])
###	fi
###
	# ********************************
	# *** Checks for semaphore lib ***
	# ********************************
	# 'Real Time' functions on Solaris
	# posix4 on Solaris 2.6
	# pthread (first!) on Linux
###	AC_SEARCH_LIBS(sem_init, pthread rt posix4) 
	check_library_exists(pthread shm_open "" HAVE_SHM_OPEN1)
	if(HAVE_SHM_OPEN1)
	  # FIXME:
	else()
	  check_library_exists(rt shm_open "" HAVE_SHM_OPEN2)
	  if(HAVE_SHM_OPEN2)
		set(LIBS ${LIBS} -lrt)
		set(CMAKE_REQUIRED_LIBRARIES rt)
		ac_check_funcs(shm_open)
		set(CMAKE_REQUIRED_LIBRARIES)
	  else()
		# FIXME: posix4
	  endif()
	endif()

	# ********************************
	# *** Checks for timezone stuff **
	# ********************************
###	AC_CACHE_CHECK(for tm_gmtoff in struct tm, ac_cv_struct_tm_gmtoff,
###		AC_TRY_COMPILE([
###			#include <time.h>
###			], [
###			struct tm tm;
###			tm.tm_gmtoff = 1;
###			], set(ac_cv_struct_tm_gmtoff yes, ac_cv_struct_tm_gmtoff=no)))
###	if test $ac_cv_struct_tm_gmtoff = yes; then
###		ac_define(HAVE_TM_GMTOFF, 1, [Have tm_gmtoff])
###	else
###		AC_CACHE_CHECK(for timezone variable, ac_cv_var_timezone,
###			AC_TRY_COMPILE([
###				#include <time.h>
###			], [
###				timezone = 1;
###			], set(ac_cv_var_timezone yes, ac_cv_var_timezone=no)))
###		if test $ac_cv_var_timezone = yes; then
###			ac_define(HAVE_TIMEZONE, 1, [Have timezone variable])
###		else
###			AC_ERROR(unable to find a way to determine timezone)
###		fi
###	fi
###
# *********************************
# *** Checks for math functions ***
# *********************************
set(LIBS ${LIBS} -lm)
###	if test "x$has_broken_apple_cpp" != "xyes"; then
###	        ac_check_funcs(finite, , ac_msg_checking(for finite in math.h)
###	                AC_TRY_LINK([#include <math.h>], 
###	                [ finite(0.0); ], 
###	                ac_define(HAVE_FINITE, 1, [Have finite]) ac_msg_result(yes),
###	                ac_msg_result(no)))
###	fi
###        ac_check_funcs(isfinite, , ac_msg_checking(for isfinite in math.h)
###                AC_TRY_LINK([#include <math.h>], 
###                [ isfinite(0.0); ], 
###                ac_define(HAVE_ISFINITE, 1, [Have isfinite]) ac_msg_result(yes),
###                ac_msg_result(no)))
###
# ****************************************************************
# *** Checks for working poll() (macosx defines it but doesn't ***
# *** have it in the library (duh))                            ***
# ****************************************************************
###	ac_check_funcs(poll)
###
# *************************
# *** Check for signbit ***
# *************************
###	ac_msg_checking(for signbit)
###	AC_TRY_LINK([#include <math.h>], [
###		int s = signbit(1.0);
###	], [
###		ac_msg_result(yes)
###		ac_define(HAVE_SIGNBIT, 1, [Have signbit])
###	], [
###		ac_msg_result(no)
###	]) 
###
# **********************************
# *** epoll			   ***
# **********************************
ac_check_headers(sys/epoll.h)
set(haveepoll no)
###	ac_check_funcs(epoll_ctl, [set(haveepoll yes], ))
###	if test "x$haveepoll" = "xyes" -a "x$ac_cv_header_sys_epoll_h" = "xyes" ; then
###		ac_define(HAVE_EPOLL, 1, [epoll supported])
###	fi
###
# ******************************
# *** Checks for SIOCGIFCONF ***
# ******************************
ac_check_headers(sys/ioctl.h)
ac_check_headers(net/if.h)
###	ac_msg_checking(for ifreq)
###	AC_TRY_COMPILE([
###		#include <stdio.h>
###		#include <sys/ioctl.h>
###		#include <net/if.h>
###		], [
###		struct ifconf ifc;
###		struct ifreq *ifr;
###		void *x;
###		ifc.ifc_len = 0;
###		ifc.ifc_buf = NULL;
###		x = (void *) &ifr->ifr_addr;
###		],[
###			ac_msg_result(yes)
###			ac_define(HAVE_SIOCGIFCONF, 1, [Can get interface list])
###		], [
###			ac_msg_result(no)
###		])
# **********************************
# ***     Checks for sin_len     ***
# **********************************
###	ac_msg_checking(for sockaddr_in.sin_len)
###	AC_TRY_COMPILE([
###		#include <netinet/in.h>
###		], [
###		struct sockaddr_in saddr;
###		saddr.sin_len = sizeof (saddr);
###		],[
###			ac_msg_result(yes)
###			ac_define(HAVE_SOCKADDR_IN_SIN_LEN, 1, [sockaddr_in has sin_len])
###		], [
###			ac_msg_result(no)
###		])	
# **********************************
# ***    Checks for sin6_len     ***
# **********************************
###	ac_msg_checking(for sockaddr_in6.sin6_len)
###	AC_TRY_COMPILE([
###		#include <netinet/in.h>
###		], [
###		struct sockaddr_in6 saddr6;
###		saddr6.sin6_len = sizeof (saddr6);
###		],[
###			ac_msg_result(yes)
###			ac_define(HAVE_SOCKADDR_IN6_SIN_LEN, 1, [sockaddr_in6 has sin6_len])
###		], [
###			ac_msg_result(no)
###		])	
# **********************************
# *** Checks for MonoPosixHelper ***
# **********************************
ac_check_headers(checklist.h)
ac_check_headers(fstab.h)
ac_check_headers(attr/xattr.h)
ac_check_headers(sys/extattr.h)
ac_check_headers(sys/sendfile.h)
ac_check_headers(sys/statvfs.h)
ac_check_headers(sys/statfs.h)
ac_check_headers(sys/vfstab.h)
ac_check_headers(sys/xattr.h)
ac_check_headers(sys/mman.h)
ac_check_headers(sys/param.h)
ac_check_headers(sys/mount.h)
###	ac_check_funcs(getdomainname)
###	ac_check_funcs(setdomainname)
###	ac_check_funcs(fgetgrent)
###	ac_check_funcs(fgetpwent)
###	ac_check_funcs(fgetpwent)
###	ac_check_funcs(getfsstat)
###	ac_check_funcs(lutimes)
###	ac_check_funcs(mremap)
###	ac_check_funcs(remap_file_pages)
###	ac_check_funcs(posix_fadvise)
###	ac_check_funcs(posix_fallocate)
###	ac_check_funcs(posix_madvise)
###	ac_check_funcs(vsnprintf)
###	ac_check_funcs(sendfile)
###	ac_check_funcs(sethostid)
###	ac_check_funcs(statfs)
###	ac_check_funcs(fstatfs)
###	ac_check_funcs(statvfs)
###	ac_check_funcs(fstatvfs)
###	ac_check_funcs(stime)
###	ac_check_funcs(strerror_r)
###	ac_check_funcs(ttyname_r)
ac_check_sizeof(size_t)
###	AC_CHECK_TYPES([blksize_t], [ac_define(HAVE_BLKSIZE_T)], , 
###		[#include <sys/types.h>
###		 #include <sys/stat.h>
###		 #include <unistd.h>])
###	AC_CHECK_TYPES([blkcnt_t], [ac_define(HAVE_BLKCNT_T)], ,
###		[#include <sys/types.h>
###		 #include <sys/stat.h>
###		 #include <unistd.h>])
###	AC_CHECK_TYPES([suseconds_t], [ac_define(HAVE_SUSECONDS_T)], ,
###		[#include <sys/time.h>])
###	AC_CHECK_TYPES([struct flock], [ac_define(HAVE_STRUCT_FLOCK)], ,
###		[#include <unistd.h>
###		 #include <fcntl.h>])
###	AC_CHECK_TYPES([struct pollfd], [ac_define(HAVE_STRUCT_POLLFD)], ,
###		[#include <sys/poll.h>])
###	AC_CHECK_TYPES([struct stat], [ac_define(HAVE_STRUCT_STAT)], ,
###		[#include <sys/types.h>
###		 #include <sys/stat.h>
###		 #include <unistd.h>])
###	AC_CHECK_TYPES([struct timespec], [ac_define(HAVE_STRUCT_TIMESPEC)], ,
###		[#include <time.h>])
###	AC_CHECK_TYPES([struct timeval], [ac_define(HAVE_STRUCT_TIMEVAL)], ,
###		[#include <sys/time.h>
###		 #include <sys/types.h>
###		 #include <utime.h>])
###	AC_CHECK_TYPES([struct timezone], [ac_define(HAVE_STRUCT_TIMEZONE)], ,
###		[#include <sys/time.h>])
###	AC_CHECK_TYPES([struct utimbuf], [ac_define(HAVE_STRUCT_UTIMBUF)], ,
###		[#include <sys/types.h>
###		 #include <utime.h>])
###	AC_CHECK_MEMBERS(
###		[struct dirent.d_off, struct dirent.d_reclen, struct dirent.d_type],,, 
###		[#include <sys/types.h>
###		 #include <dirent.h>])
###
# Favour xattr through glibc, but use libattr if we have to
###	AC_CHECK_FUNC(lsetxattr, ,
###		AC_CHECK_LIB(attr, lsetxattr, set(XATTR_LIB "-lattr",))
###	)
###	AC_SUBST(XATTR_LIB)
###
# kinfo_proc.kp_proc works on darwin but fails on other simil-bsds
###	AC_CHECK_MEMBERS(
###		[struct kinfo_proc.kp_proc],,, 
###		[#include <sys/types.h>
###		 #include <sys/sysctl.h>
###		 #include <sys/proc.h>
###		 ])
###
# *********************************
# *** Checks for Windows compilation ***
# *********************************
ac_check_headers(sys/time.h)
ac_check_headers(sys/param.h)
ac_check_headers(dirent.h)

# *********************************
# *** Check for Console 2.0 I/O ***
# *********************************
ac_check_headers(curses.h)
ac_check_headers(term.h)
###	ac_check_headers([term.h], [], [],
###	[#if HAVE_CURSES_H
###	 #include <curses.h>
###	 #endif
###	])
ac_check_headers(termios.h)
###
# * This is provided in io-layer, but on windows it's only available
# * on xp+
###	ac_define(HAVE_GETPROCESSID, 1, [Define if GetProcessId is available])
###else
###	set(jdk_headers_found no)
###	AC_CHECK_LIB(ws2_32, main, set(LIBS "$LIBS -lws2_32", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(psapi, main, set(LIBS "$LIBS -lpsapi", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(ole32, main, set(LIBS "$LIBS -lole32", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(winmm, main, set(LIBS "$LIBS -lwinmm", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(oleaut32, main, set(LIBS "$LIBS -loleaut32", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(advapi32, main, set(LIBS "$LIBS -ladvapi32", AC_ERROR(bad mingw install?)))
###	AC_CHECK_LIB(version, main, set(LIBS "$LIBS -lversion", AC_ERROR(bad mingw install?)))
###
# *********************************
# *** Check for struct ip_mreqn ***
# *********************************
###	ac_msg_checking(for struct ip_mreqn)
###	AC_TRY_COMPILE([#include <ws2tcpip.h>], [
###		struct ip_mreqn mreq;
###		mreq.imr_address.s_addr = 0;
###	], [
###		# Yes, we have it...
###		ac_msg_result(yes)
###		ac_define(HAVE_STRUCT_IP_MREQN)
###	], [
###		# We'll just have to try and use struct ip_mreq
###		ac_msg_result(no)
###		ac_msg_checking(for struct ip_mreq)
###		AC_TRY_COMPILE([#include <ws2tcpip.h>], [
###			struct ip_mreq mreq;
###			mreq.imr_interface.s_addr = 0;
###		], [
###			# Yes, we have it...
###			ac_msg_result(yes)
###			ac_define(HAVE_STRUCT_IP_MREQ)
###		], [
###			# No multicast support
###			ac_msg_result(no)
###		])
###	])
###	ac_check_funcs(GetProcessId)
###fi
###
endif()

# socklen_t check
ac_msg_checking("for socklen_t")
check_c_source_compiles("
#include <sys/types.h>
#include <sys/socket.h>
void main () { 
  socklen_t foo; 
}" HAVE_SOCKLEN_T)
if(HAVE_SOCKLEN_T)
  ac_define(HAVE_SOCKLEN_T 1 "Have socklen_t")
  ac_msg_result(yes)
else()
  ac_msg_result(no)
endif()

###ac_msg_checking(for array element initalizer support)
###AC_TRY_COMPILE([#include <sys/socket.h>], [
###	const int array[] = {[1] = 2,};
###], [
# Yes, we have it...
###	ac_msg_result(yes)
###	ac_define(HAVE_ARRAY_ELEM_INIT,1,[Supports C99 array initialization])
###], [
# We'll have to use signals
###	ac_msg_result(no)
###])
###
ac_check_funcs(trunc)
if(NOT HAVE_TRUNC)
  ac_msg_checking("for trunc in math.h")
  # Simply calling trunc (0.0) is no good since gcc will optimize the call away
  set(compiles)
  check_c_source_compiles("
#include <math.h>
void main () {
static void *p = &trunc;
}
" compiles)
  if (compiles)
	ac_define(HAVE_TRUNC 1 "")
	ac_msg_result(yes)
  else()
	ac_msg_result(no)
  endif()
endif()

###if test "x$ac_cv_truncl" != "xyes"; then
###   AC_CHECK_LIB(sunmath, aintl, [ ac_define(HAVE_AINTL, 1, [Has the 'aintl' function]) set(LIBS "$LIBS -lsunmath"]))
###fi

ac_check_funcs(round)
ac_check_funcs(rint)

# ****************************
# *** Look for /dev/random ***
# ****************************

###ac_msg_checking([if usage of random device is requested])
###AC_ARG_ENABLE(dev-random,
###[  --disable-dev-random    disable the use of the random device (enabled by default)],
###set(try_dev_random $enableval, try_dev_random=yes))

###ac_msg_result($try_dev_random)

###case "{$target}" in
###    *-openbsd*)
###    set(NAME_DEV_RANDOM "/dev/srandom")
###    ;;

# Win32 does not have /dev/random, they have their own method...

###    *-*-mingw*|*-*-cygwin*)
###    set(ac_cv_have_dev_random no)
###    ;;

# Everywhere else, it's /dev/random

###    *)
###    set(NAME_DEV_RANDOM "/dev/random")
###    ;;
###esac

###ac_define_unquoted(NAME_DEV_RANDOM, "$NAME_DEV_RANDOM", [Name of /dev/random])

# Now check if the device actually exists

###if test "x$try_dev_random" = "xyes"; then
###    AC_CACHE_CHECK(for random device, ac_cv_have_dev_random,
###    [if test -r "$NAME_DEV_RANDOM" ; then
###        set(ac_cv_have_dev_random yes; else ac_cv_have_dev_random=no; fi]))
###    if test "x$ac_cv_have_dev_random" = "xyes"; then
###        ac_define(HAVE_CRYPT_RNG, 1, [Have /dev/random])
###    fi
###else
###    ac_msg_checking(for random device)
###    set(ac_cv_have_dev_random no)
###    ac_msg_result(has been disabled)
###fi

###if test "x$platform_win32" = "xyes"; then
###    ac_define(HAVE_CRYPT_RNG)
###fi

###if test "x$ac_cv_have_dev_random" = "xno" \
###    && test "x$platform_win32" = "xno"; then
###    AC_MSG_WARN([[
###***
###*** A system-provided entropy source was not found on this system.
###*** Because of this, the System.Security.Cryptography random number generator
###*** will throw a NotImplemented exception.
###***
###*** If you are seeing this message, and you know your system DOES have an
###*** entropy collection in place, please contact <crichton@gimp.org> and
###*** provide information about the system and how to access the random device.
###***
###*** Otherwise you can install either egd or prngd and set the environment
###*** variable MONO_EGD_SOCKET to point to the daemon's socket to use that.
###***]])
###fi
### 
###ac_msg_checking([if inter-process shared handles are requested])
###AC_ARG_ENABLE(shared-handles, [  --disable-shared-handles disable inter-process shared handles], set(try_shared_handles $enableval, try_shared_handles=yes))
###ac_msg_result($try_shared_handles)
###if test "x$try_shared_handles" != "xyes"; then
###	ac_define(DISABLE_SHARED_HANDLES, 1, [Disable inter-process shared handles])
###	AC_SUBST(DISABLE_SHARED_HANDLES)
###fi

###if test x$gc = xsgen; then
###   if test x$with_tls != x__thread; then
###	  ac_msg_error([The SGEN garbage collector depends on a working __thread implementation, and either --with-set(thread pthread was passed to configure, or the configure test for __thread failed.]))
###   fi
###fi

###AC_ARG_ENABLE(nunit-tests, [  --enable-nunit-tests	Run the nunit tests of the class library on 'make check'])
###AM_CONDITIONAL(ENABLE_NUNIT_TESTS, [test x$enable_nunit_tests = xyes])

ac_msg_checking("if big-arrays are to be enabled")
set(big-arrays no CACHE STRING "Enable the allocation and indexing of arrays greater than Int32.MaxValue]")
set(enable_big_arrays ${big-arrays})
###if test "x$enable_big_arrays" = "xyes" ; then
###    if  test "x$ac_cv_sizeof_void_p" = "x8"; then
###	ac_define(MONO_BIG_ARRAYS,1,[Enable the allocation and indexing of arrays greater than Int32.MaxValue])
###    else
###        ac_msg_error([The allocation and indexing of arrays greater than Int32.MaxValue is not supported on this platform.])
###    fi
###fi
###ac_msg_result($enable_big_arrays)

# **************
# *** DTRACE ***
# **************

set(dtrace ${has_dtrace} CACHE BOOL "Enable DTrace probes")
set(enable_dtrace ${dtrace})

###if test "x$enable_dtrace" = "xyes"; then
###   if test "x$has_dtrace" = "xno"; then
###   	  ac_msg_error([DTrace probes are not supported on this platform.])
###   fi
###   AC_PATH_PROG(DTRACE, [dtrace], [no], [$PATH:/usr/sbin])
###   if test "x$DTRACE" = "xno"; then
###   	  ac_msg_result([dtrace utility not found, dtrace support disabled.])
###	  set(enable_dtrace no)
###   fi
###fi

set(dtrace_g no)
###if test "x$enable_dtrace" = "xyes"; then
###	ac_define(ENABLE_DTRACE, 1, [Enable DTrace probes])
###	set(DTRACEFLAGS )
###	if test "x$ac_cv_sizeof_void_p" = "x8"; then
###		case "$host" in
###			powerpc-*-darwin*)
###			set(DTRACEFLAGS "-arch ppc64")
###			;;
###			i*86-*-darwin*)
###			set(DTRACEFLAGS "-arch x86_64")
###			;;
###			*)
###			set(DTRACEFLAGS -64)
###			;;
###		esac
###	else
###		case "$host" in
###			powerpc-*-darwin*)
###			set(DTRACEFLAGS "-arch ppc")
###			;;
###			i*86-*-darwin*)
###			set(DTRACEFLAGS "-arch i386")
###			;;
###			*)
###			set(DTRACEFLAGS -32)
###			;;
###		esac
###	fi
###	AC_SUBST(DTRACEFLAGS)
###	case "$host" in
###		*-*-solaris*)
###		set(dtrace_g yes)
###		;;
###	esac
###	ac_check_headers([sys/sdt.h])
###fi
###AM_CONDITIONAL(ENABLE_DTRACE, [test x$enable_dtrace = xyes])
###AM_CONDITIONAL(DTRACE_G_REQUIRED, [test x$dtrace_g = xyes])

# **************
# ***  LLVM  ***
# **************

set(llvm no CACHE BOOL "Enable the experimental LLVM back-end")
set(enable_llvm ${llvm})

###if test "x$enable_llvm" = "xyes"; then
###   AC_PATH_PROG(LLVM_CONFIG, llvm-config, no)
###   if test "x$LLVM_CONFIG" = "xno"; then
###   	  ac_msg_error([llvm-config not found.])
###   fi

###   set(LLVM_CXXFLAGS `$LLVM_CONFIG --cflags`)
###   set(LLVM_LDFLAGS `$LLVM_CONFIG --ldflags`)
###   set(LLVM_LIBS `$LLVM_CONFIG --libs core engine`)
###   set(LLVM_LIBS "$LLVM_LDFLAGS $LLVM_LIBS -lstdc++")

###   AC_SUBST(LLVM_CXXFLAGS)
###   AC_SUBST(LLVM_LIBS)
###   ac_define(ENABLE_LLVM, 1, [Enable the LLVM back end])
###fi

if(enable_llvm)
  set(ENABLE_LLVM yes)
endif()

#
# Architecture-specific checks
#
set(TARGET "unknown")
set(ACCESS_UNALIGNED "yes")

set(JIT_SUPPORTED no)
set(INTERP_SUPPORTED no)
set(LIBC "libc.so.6")
set(INTL "libc.so.6")
set(SQLITE "libsqlite.so.0")
set(SQLITE3 "libsqlite3.so.0")
set(X11 "libX11.so")

set(sizeof_register "SIZEOF_VOID_P")

set(jit_wanted false)
set(interp_wanted false)

if(host MATCHES "(x86_64-.*-.*)|(amd64-.*-.*)")
  set(TARGET AMD64)
  set(arch_target amd64)
  set(JIT_SUPPORTED yes)
  set(jit_wanted true)
elseif(host MATCHES "arm.*-linux.*")
  set(TARGET ARM)
  set(arch_target arm)
  set(ACCESS_UNALIGNED no)
  set(JIT_SUPPORTED yes)
  set(jit_wanted true)
elseif(host MATCHES "mips.*")
  set(TARGET MIPS)
  set(arch_target mips)
  set(ACCESS_UNALIGNED no)
  set(JIT_SUPPORTED yes)
  set(jit_wanted true)
###		ac_msg_checking(for mips n32)
###		AC_TRY_COMPILE([], [
###		void main () {
###		#if _MIPS_SIM != _ABIN32
###		#error Not mips n32
###		#endif
###   		}
###		],[
###		ac_msg_result(yes)
###		set(sizeof_register 8)
###		],[
###		ac_msg_result(no)
###		])
###		;;
else()
  message(FATAL_ERROR "Host ${host} not yet supported by the cmake build.")
endif()

# FIXME: Define the others as well
if (${TARGET} STREQUAL "X86")
  ac_define(TARGET_X86 1 [...])
elseif (${TARGET} STREQUAL "AMD64")
  ac_define(TARGET_AMD64 1 [...])
elseif (${TARGET} STREQUAL "ARM")
  ac_define(TARGET_ARM 1 [...])
endif()

###case "$host" in
#	mips-sgi-irix5.* | mips-sgi-irix6.*)
#		set(TARGET MIPS;)
#		set(ACCESS_UNALIGNED "no")
#		;;
###	i*86-*-*)
###		set(TARGET X86;)
###		set(arch_target x86;)
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		case $host_os in
###		  solaris*)
###			set(LIBC "libc.so")
###			set(INTL "libintl.so")
###			if test "x$ac_cv_sizeof_void_p" = "x8"; then
###				set(TARGET AMD64)
###				set(arch_target amd64)
###			fi

###			# On solaris 10 x86, gcc prints a warning saying 'visibility attribute not supported on this configuration; ignored', but linking fails. A test case:
###			# int astruct __attribute__ ((visibility ("hidden")));
###			# void foo ()
###			# {
###			#	void *p = &astruct;
###			# }
###			# gcc -fPIC --shared -o libfoo.so foo.c
###			# yields:
###			# foo.c:6: warning: visibility attribute not supported in this configuration; ignored
###			# ld: fatal: relocation error: R_386_GOTOFF: file /var/tmp//ccxYR96k.o: symbol astruct: relocation must bind locally
###			set(have_visibility_hidden no)

###		esac
###		;;
###	ia64-*-*)
###		set(TARGET IA64)
###		set(arch_target ia64)
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		set(LIBC "libc.so.6.1")
###		set(INTL "libc.so.6.1")
###		AC_CHECK_LIB(unwind, _U_dyn_register, [], [ac_msg_error(library libunwind not found)])
###		set(libmono_ldflags "-lunwind")
###		;;
###	sparc*-*-*)
###		if test "x$ac_cv_sizeof_void_p" = "x8"; then
###		   set(TARGET SPARC64)
###		else
###			set(TARGET SPARC)
###		fi
###		set(arch_target sparc;)
###		set(JIT_SUPPORTED yes)
###		set(ACCESS_UNALIGNED "no")
###		case $host_os in
###		  linux*) ;;
###		  *)
###			set(LIBC "libc.so")
###			set(INTL "libintl.so")
###		esac
###		set(jit_wanted true)
###		if test x"$GCC" = xyes; then
###			# We don't support v8 cpus
###			set(CFLAGS "$CFLAGS -Wno-cast-align -mcpu=v9")
###		fi
###		if test x"$AR" = xfalse; then
###			ac_msg_error([The required utility 'ar' is not found in your PATH. Usually it can be found in /usr/ccs/bin.])
###		fi
###		;;
###       alpha*-*-linux* | alpha*-*-osf*)
###		set(TARGET ALPHA;)
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		set(arch_target alpha;)
###		set(CFLAGS "$CFLAGS -mieee -O0")
###		case $host_os in
###		  linux*)
###			set(LIBC "libc.so.6.1")
###			set(INTL "libc.so.6.1")
###		esac
###	       ;;
###	*-*-mingw*|*-*-cygwin*)
###		# When this is enabled, it leads to very strange crashes at runtime (gcc-3.4.4)
###		set(have_visibility_hidden no)
###		set(INTL "intl")
###		;;
###	hppa2.0w-hp-hpux11.00 | hppa64-hp-hpux11.00)
###		set(TARGET HPPA;)
###		set(arch_target hppa; )
###		set(LIBC "libc.sl")
###		set(ACCESS_UNALIGNED "no")
###		set(INTERP_SUPPORTED yes)
###		set(interp_wanted true)
###		;;
###	hppa*linux*)
###		set(TARGET HPPA;)
###		ac_define(MONO_ARCH_REGPARMS,1,[Architecture uses registers for Parameters])
###		set(arch_target hppa; )
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		;;
###	macppc-*-openbsd* | powerpc*-*-linux* | powerpc-*-openbsd* | \
###        powerpc-*-sysv* | powerpc-*-darwin* | powerpc-*-netbsd* )
###		if test "x$ac_cv_sizeof_void_p" = "x8"; then
###			set(TARGET POWERPC64;)
###			set(CPPFLAGS "$CPPFLAGS -D__mono_ppc__ -D__mono_ppc64__")
###			set(CFLAGS "$CFLAGS -mminimal-toc")
###		else
###			set(TARGET POWERPC;)
###			set(CPPFLAGS "$CPPFLAGS -D__mono_ppc__")
###		fi
###		ac_define(MONO_ARCH_REGPARMS,1,[Architecture uses registers for Parameters])
###		set(arch_target ppc;)
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		;;
###	arm*-darwin*)
###		set(TARGET ARM;)
###		set(arch_target arm;)
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(CPPFLAGS "$CPPFLAGS -DARM_FPU_NONE=1")
###		set(jit_wanted true)
###		;;
###	s390-*-linux*)
###		set(TARGET S390;)
###		ac_define(MONO_ARCH_REGPARMS,1,[Architecture uses registers for Parameters])
###		set(arch_target s390;)
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		# Required CFLAGS for s390[x].  USE_STRING_INLINES is automatic with gcc 4.1
###		set(CFLAGS "$CFLAGS -mbackchain -D__USE_STRING_INLINES")
###		;;
###	s390x-*-linux*)
###		set(TARGET S390x;)
###		ac_define(MONO_ARCH_REGPARMS,1,[Architecture uses registers for Parameters])
###		set(arch_target s390x;)
###		set(ACCESS_UNALIGNED "no")
###		set(JIT_SUPPORTED yes)
###		set(jit_wanted true)
###		set(CFLAGS "$CFLAGS -mbackchain -D__USE_STRING_INLINES")
###		;;
###esac

if (${sizeof_register} STREQUAL "4")
   ac_define(SIZEOF_REGISTER 4 "size of machine integer registers")
elseif (${sizeof_register} STREQUAL "8")
   ac_define(SIZEOF_REGISTER 8 "size of machine integer registers")
else()
   ac_define(SIZEOF_REGISTER SIZEOF_VOID_P "size of machine integer registers")
endif()

###if (${have_visibility_hidden" = "xyes"; then
###   ac_define(HAVE_VISIBILITY_HIDDEN, 1, [Support for the visibility ("hidden") attribute])
###fi

###set(jit, [  --with-set(jit yes,no              If you want to build scripts that default to the JIT (defaults to no)],[)
###	if test x$withval = xyes; then
###	   set(jit_wanted true)
###	else
###	   set(jit_wanted false)
###	fi
###])

###set(interp, [  --with-set(interp yes,no           If you want to build scripts that default to the interpreter (defaults to no)],[)
###	if test x$withval = xyes; then
###	   set(interp_wanted true)
###	else
###	   set(interp_wanted false)
###	fi
###])

set(USEJIT no)
if(JIT_SUPPORTED)
  if (jit_wanted)
	set(USEJIT yes)
	set(jit_status "Building and using the JIT")
  else()
	if (interp_wanted)
	  set(jit_status "Building the JIT, defaulting to the interpreter")
	else()
	  message(FATAL_ERROR "No JIT or interpreter support available or selected.")
	endif()
  endif()
else()
	if (interp_wanted)
	  set(jit_status "interpreter")
	else()
	  message(FATAL_ERROR "No JIT or interpreter support available or selected.")
	endif()
endif()  
set(USE_JIT ${USEJIT})

set(libsuffix ".so")

###case "$host" in
###     *-*-darwin*)
###	set(libsuffix ".dylib")
###	set(LIBC "libc.dylib")
###	set(INTL "libintl.dylib")
###	set(SQLITE "libsqlite.0.dylib")
###	set(SQLITE3 "libsqlite3.0.dylib")
###	set(X11 "libX11.dylib")
###	;;
###     *-*-*netbsd*)
###	set(LIBC "libc.so.12")
###	set(INTL "libintl.so.0")
###	;;
###    *-*-*freebsd*)
###    	set(LIBC "libc.so")
###	set(INTL "libintl.so")
###	;;
###    *-*-*openbsd*)
###    	set(LIBC "libc.so")
###	set(INTL "libintl.so")
###	;;
###    *-*-*linux*)
###	AC_PATH_X
###	ac_msg_checking(for the soname of libX11.so)
###	for i in $x_libraries /usr/lib /usr/lib64; do
###		for r in 4 5 6; do
###			if test -f $i/libX11.so.$r; then
###				set(X11 libX11.so.$r)
###				ac_msg_result($X11)
###			fi
###		done
###	done
###	
###	if (${X11" = "xlibX11.so"; then
###		AC_MSG_WARN([Could not find X development libs. Do you have the -devel package installed? WinForms may not work...]);
###	fi
###	;;
###esac


###AC_SUBST(libsuffix)

ac_check_headers(valgrind/memcheck.h)
if(${TARGET} STREQUAL "AMD64" OR ${TARGET} STREQUAL "X86")
  if(with_tls STREQUAL __thread)
		#
		# On some linux distributions, TLS works in executables, but linking 
		# against a shared library containing TLS fails with:
		# undefined reference to `__tls_get_addr'
		#
###		rm -f conftest.c conftest.so conftest
###		echo "static __thread int foo; void main () { foo = 5; }" > conftest.c
###		$CC -fPIC --shared -o conftest.so conftest.c > /dev/null 2>&1
###		$CC -o conftest conftest.so > /dev/null 2>&1
###		if test ! -f conftest; then
###		   AC_MSG_WARN([Disabling usage of __thread.]);
###		   set(with_tls pthread)
###		fi
###		rm -f conftest.c conftest.so conftest
  endif()
endif()

set(mono_debugger_supported no)
if(${TARGET} STREQUAL "AMD64" OR ${TARGET} STREQUAL "X86")
  if(use_included_gc)
	if (host MATCHES ".*-.*-.*linux.*")
	  set(mono_debugger_supported yes)
	endif()
  endif()
endif()

ac_msg_checking("if the Mono Debugger is supported on this platform")
if(mono_debugger_supported)
  ac_define(MONO_DEBUGGER_SUPPORTED 1 "The Mono Debugger is supported on this platform")
endif()
ac_msg_result(${mono_debugger_supported})
if(mono_debugger_supported)
  set(MONO_DEBUGGER_SUPPORTED yes)
endif()

if (with_tls STREQUAL "__thread")
  ac_define(HAVE_KW_THREAD 1 "Have __thread keyword")
  ac_define(USE_COMPILER_TLS 1 "Use __thread for TLS access")
# Pass the information to libgc
set(CPPFLAGS "${CPPFLAGS} -DUSE_COMPILER_TLS")
###	ac_msg_checking(if the tls_model attribute is supported)
###	AC_TRY_COMPILE([static __thread int foo __attribute__((tls_model("initial-exec")));], [
###		], [
###			ac_msg_result(yes)
###			ac_define(HAVE_TLS_MODEL_ATTR, 1, [tld_model available])
###		], [
###			ac_msg_result(no)
###	])
###fi

endif()

# ******************************************
# *** Check to see what FPU is available ***
# ******************************************
# FIXME: Don't do this if cross-compiling
if(${TARGET} STREQUAL "ARM")
  ac_msg_checking("which FPU to use")
  set(CMAKE_REQUIRED_FLAGS "-mfloat-abi=softfp -mfpu=vfp")
  set(compiles_fpu_vfp)
  check_c_source_compiles("
void main () {
		__asm__ (\"faddd   d7, d6, d7\");
}
" compiles_fpu_vfp)
  set(compiles_fpu_fpa)
  set(cmake_required_flags)
  check_c_source_compiles("
void main () {
		__asm__ (\"ldfd f0, [r0]\");
}
" compiles_fpu_fpa)
  if(compiles_fpu_vfp)
	set(fpu VFP)
  elseif(compiles_fpu_fpa)
	set(fpu FPA)
  else()
	set(fpu NONE)
  endif()
  ac_msg_result(${fpu})
  set(CPPFLAGS "${CPPFLAGS} -DARM_FPU_${fpu}=1")
  set(fpu)
endif()

if(${TARGET} STREQUAL "unknown")
	set(CPPFLAGS ${CPPFLAGS} -DNO_PORT)
	ac_msg_warn("mono has not been ported to ${host}: some things may not work.")
endif()

if(NOT ACCESS_UNALIGNED)
  set(CPPFLAGS "${CPPFLAGS} -DNO_UNALIGNED_ACCESS")
endif()

###case "x$gc" in
###	xincluded)
###		# Pass CPPFLAGS to libgc configure
###		# We should use a separate variable for this to avoid passing useless and
###		# potentially problematic defines to libgc (like -set(D_FILE_OFFSET_BITS 64))
###		# This should be executed late so we pick up the final version of CPPFLAGS
###		# The problem with this approach, is that during a reconfigure, the main
###		# configure scripts gets invoked with these arguments, so we use separate
###		# variables understood by libgc's configure to pass CPPFLAGS and CFLAGS.
###		set(LIBGC_CPPFLAGS $CPPFLAGS)
###		if test x$TARGET = xSPARC -o x$TARGET = xSPARC64; then
###			set(LIBGC_CPPFLAGS `echo $LIBGC_CPPFLAGS | sed -e 's/-D_FILE_OFFSET_BITS=64//g'`)
###		fi
###		set(ac_configure_args "$ac_configure_args --disable-embed-check --with-libgc-threads=$libgc_threads $libgc_configure_args \"CPPFLAGS_FOR_LIBGC=$LIBGC_CPPFLAGS\" \"CFLAGS_FOR_LIBGC=$CFLAGS_FOR_LIBGC\"")
###		AC_CONFIG_SUBDIRS(libgc)
###		;;
###esac

set(preview yes CACHE BOOL "If you want to install the 2.0 FX preview (defaults to yes)")
set(PREVIEW ${preview})
set(moonlight yes CACHE BOOL "If you want to build the Moonlight 2.1 assemblies (defaults to yes)")
set(MOONLIGHT ${moonlight})
set(OPROFILE no)
set(oprofile no CACHE STRING "<oprofile install dir> or 'no' to disable oprofile support (defaults to no)")
if (NOT oprofile STREQUAL no)
###	if test x$with_oprofile != xno; then
###	    set(oprofile_include $with_oprofile/include)
###	    if test ! -f $oprofile_include/opagent.h; then
###	   	  ac_msg_error([oprofile include file not found at $oprofile_include/opagent.h])
###		fi
###	    set(OPROFILE yes)
###		set(OPROFILE_CFLAGS "-I$oprofile_include")
###	    set(OPROFILE_LIBS "-L$with_oprofile/lib/oprofile -lopagent")
###	    ac_define(HAVE_OPROFILE,1,[Have oprofile support])
###	fi
###])

endif()
set(MALLOC_MEMPOOLS no)
set(malloc_mempools no CACHE STRING "Use malloc for each single mempool allocation (only for runtime debugging, defaults to no)")
###	if test x$with_malloc_mempools = xyes; then
###		set(MALLOC_MEMPOOLS yes)
###		ac_define(USE_MALLOC_FOR_MEMPOOLS,1,[Use malloc for each single mempool allocation])
###	fi
###])


set(DISABLE_MCS_DOCS no)
set(mcs_docs yes CACHE STRING "If you want to build the documentation under mcs (defaults to yes)")
if(NOT mcs_docs)
  set(DISABLE_MCS_DOCS yes)
endif()
if(OPROFILE)
  set(HAVE_OPROFILE yes)
endif()
###AC_SUBST(OPROFILE_CFLAGS)
###AC_SUBST(OPROFILE_LIBS)

set(libs_list)
foreach(lib ${LIBS})
  set(libs_list "${libs_list} ${lib}")
endforeach()
set(libmono_ldflags "${libmono_ldflags} ${libs_list}")

if(PREVIEW)
  set(INSTALL_2_0 yes)
endif()
if(MOONLIGHT)
  set(INSTALL_2_1 yes)
endif()

###AM_CONDITIONAL(MIPS_GCC, test ${TARGET}${ac_cv_prog_gcc} = MIPSyes)
###AM_CONDITIONAL(MIPS_SGI, test ${TARGET}${ac_cv_prog_gcc} = MIPSno)
# Define a variable for the target
set(${TARGET} 1)

if (interp_wanted)
  set(INTERP_SUPPORTED yes)
endif()
if (gc STREQUAL "included")
  set(INCLUDED_LIBGC yes)
endif()

###AC_SUBST(LIBC)
###AC_SUBST(INTL)
###AC_SUBST(SQLITE)
###AC_SUBST(SQLITE3)
###AC_SUBST(X11)
ac_define_unquoted(ARCHITECTURE "${arch_target}" "The architecture this is running on")
###AC_SUBST(arch_target)
###AC_SUBST(CFLAGS)
###AC_SUBST(CPPFLAGS)
###AC_SUBST(LDFLAGS)

set(mono_build_root ${CMAKE_BINARY_DIR})

if (USEJIT)
  set(mono_runtime mono/mini/mono)
else()
  set(mono_runtime mono/interpreter/mint)
endif()

set(mono_cfg_root ${mono_build_root}/runtime)
if (platform_win32)
###if test x$platform_win32 = xyes; then
###  if (${cross_compiling" = "xno"; then
###    set(mono_cfg_dir `cygpath -w -a $mono_cfg_root`\\etc)
###  else
###    set(mono_cfg_dir `echo $mono_cfg_root | tr '/' '\\\'`\\etc)
###  fi
else()
  set(mono_cfg_dir ${mono_cfg_root}/etc)
endif()

function(ac_config_files file)
  configure_file("${file}.in" ${file} @ONLY)
endfunction()
ac_config_files("po/mcs/Makefile.in")

ac_config_files("runtime/mono-wrapper")
ac_config_files("runtime/monodis-wrapper")
execute_process (COMMAND chmod a+x runtime/mono-wrapper runtime/monodis-wrapper)

###AC_CONFIG_COMMANDS([runtime/etc/mono/1.0/machine.config],
###[   set(depth ../../../..)
###    case $srcdir in
###    [[\\/$]]* | ?:[[\\/]]* ) set(reldir $srcdir ;;)
###    .) set(reldir $depth ;;)
###    *) set(reldir $depth/$srcdir ;;)
###    esac
###    $ac_aux_dir/install-sh -d runtime/etc/mono/1.0
###    cd runtime/etc/mono/1.0
###    rm -f machine.config
###    $LN_S $reldir/data/net_1_1/machine.config machine.config
###    cd $depth
###],[set(LN_S '$LN_S']))

###AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/machine.config],
###[   set(depth ../../../..)
###    case $srcdir in
###    [[\\/$]]* | ?:[[\\/]]* ) set(reldir $srcdir ;;)
###    .) set(reldir $depth ;;)
###    *) set(reldir $depth/$srcdir ;;)
###    esac
###    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0
###    cd runtime/etc/mono/2.0
###    rm -f machine.config
###    $LN_S $reldir/data/net_2_0/machine.config machine.config
###    cd $depth
###],[set(LN_S '$LN_S']))

###AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/web.config],
###[   set(depth ../../../..)
###    case $srcdir in
###    [[\\/$]]* | ?:[[\\/]]* ) set(reldir $srcdir ;;)
###    .) set(reldir $depth ;;)
###    *) set(reldir $depth/$srcdir ;;)
###    esac
###    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0
###    cd runtime/etc/mono/2.0
###    rm -f web.config
###    $LN_S $reldir/data/net_2_0/web.config web.config
###    cd $depth
###],[set(LN_S '$LN_S']))

###AC_CONFIG_COMMANDS([runtime/etc/mono/browscap.ini],
###[   set(depth ../../..)
###    case $srcdir in
###    [[\\/$]]* | ?:[[\\/]]* ) set(reldir $srcdir ;;)
###    .) set(reldir $depth ;;)
###    *) set(reldir $depth/$srcdir ;;)
###    esac
###    $ac_aux_dir/install-sh -d runtime/etc/mono/
###    cd runtime/etc/mono/
###    rm -f browscap.ini
###    $LN_S $reldir/data/browscap.ini browscap.ini
###    cd $depth
###],[set(LN_S '$LN_S']))

###AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/Browsers/Compat.browser],
###[   set(depth ../../../../..)
###    case $srcdir in
###    [[\\/$]]* | ?:[[\\/]]* ) set(reldir $srcdir ;;)
###    .) set(reldir $depth ;;)
###    *) set(reldir $depth/$srcdir ;;)
###    esac
###    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0/Browsers/
###    cd runtime/etc/mono/2.0/Browsers
###    rm -f Compat.browser
###    $LN_S $reldir/data/net_2_0/Browsers/Compat.browser Compat.browser
###    cd $depth
###],[set(LN_S '$LN_S']))

###if test x$enable_quiet_build = xyes; then
###   AC_CONFIG_COMMANDS([quiet], [for i in `find mono libgc support -name Makefile.in | sed -e 's/Makefile.in/Makefile/g'`; do if test -f $i; then $srcdir/scripts/patch-quiet.sh $i; fi; done], [set(shell $SHELL]))
###   AC_CONFIG_COMMANDS([quiet-libtool], [sed -e 's/$echo "copying selected/$show "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool; sed -e 's/$ECHO "copying selected/# "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool])
###fi

if("${prefix}" STREQUAL "")
  set(prefix /usr/local)
endif()
if("${exec_prefix}" STREQUAL "")
  set(exec_prefix "\${prefix}")
  set(exec_prefix_full "${prefix}")
else()
  set(exec_prefix_full "${exec_prefix}")
endif()

# FIXME: Make these overridable
set(bindir "\${exec_prefix}/bin")
set(bindir_full "${exec_prefix_full}/bin")
set(sbindir "\${exec_prefix}/sbin")
set(libexecdir "\${exec_prefix}/libexec")
set(datarootdir "\${prefix}/share")
set(datadir "\${datarootdir}")
set(sysconfdir "\${prefix}/etc")
set(sharedstatedir "\${prefix}/com")
set(localstatedir "\${prefix}/var")
set(includedir "\${prefix}/include")
set(oldincludedir "/usr/include")
set(docdir "\${datarootdir}/doc/\${PACKAGE}")
set(infodir "\${datarootdir}/info")
set(htmldir "\${docdir}")
set(dvidir "\${docdir}")
set(pdfdir "\${docdir}")
set(psdir "\${docdir}")
set(libdir "\${exec_prefix}/lib")
set(localedir "\${datarootdir}/locale")
set(mandir "\${datarootdir}/man")

autoheader("config.h" autoheader_vars)

set(SUBDIRS po ${libgc_dir} ${eglib_dir} mono ${ikvm_native_dir} support data runtime scripts man samples web msvc docs)

foreach(dir ${SUBDIRS})
  add_subdirectory(${dir})
endforeach()

# Implementation of AC_OUTPUT for cmake
function(ac_output outputs)
  foreach (output ${ARGV})
	configure_file ("${output}.in" "${output}" @ONLY)
  endforeach()
endfunction()

ac_output(
mono-uninstalled.pc
scripts/mono-find-provides
scripts/mono-find-requires
mono/tests/tests-config
data/mint.pc
data/mono.pc
data/mono-cairo.pc
data/mono-nunit.pc
data/mono-options.pc
data/mono-lineeditor.pc
data/monodoc.pc
data/mono.web.pc
data/dotnet.pc
data/dotnet35.pc
data/wcf.pc
data/cecil.pc
data/system.web.extensions_1.0.pc
data/system.web.extensions.design_1.0.pc
data/system.web.mvc.pc
data/config
)

###AC_OUTPUT([
###mono-uninstalled.pc
###scripts/mono-find-provides
###scripts/mono-find-requires
###mono/dis/Makefile
###mono/cil/Makefile
###mono/arch/Makefile
###mono/arch/x86/Makefile
###mono/arch/amd64/Makefile
###mono/arch/hppa/Makefile
###mono/arch/ppc/Makefile
###mono/arch/sparc/Makefile
###mono/arch/s390/Makefile
###mono/arch/s390x/Makefile
###mono/arch/arm/Makefile
###mono/arch/alpha/Makefile
###mono/arch/ia64/Makefile
###mono/arch/mips/Makefile
###mono/interpreter/Makefile
###mono/tests/Makefile
###mono/tests/tests-config
###mono/tests/assemblyresolve/Makefile
###mono/tests/cas/Makefile
###mono/tests/cas/assembly/Makefile
###mono/tests/cas/demand/Makefile
###mono/tests/cas/inheritance/Makefile
###mono/tests/cas/linkdemand/Makefile
###mono/tests/cas/threads/Makefile
###mono/benchmark/Makefile
###mono/monograph/Makefile
###mono/profiler/Makefile
###ikvm-native/Makefile
###scripts/Makefile
###man/Makefile
###web/Makefile
###docs/Makefile
###data/Makefile
###data/net_1_1/Makefile
###data/net_2_0/Makefile
###data/net_2_0/Browsers/Makefile
###data/mint.pc
###data/mono.pc
###data/mono-cairo.pc
###data/mono-nunit.pc
###data/mono-options.pc
###data/mono-lineeditor.pc
###data/monodoc.pc
###data/mono.web.pc
###data/dotnet.pc
###data/dotnet35.pc
###data/wcf.pc
###data/cecil.pc
###data/system.web.extensions_1.0.pc
###data/system.web.extensions.design_1.0.pc
###data/system.web.mvc.pc
###samples/Makefile
###support/Makefile
###data/config
###tools/Makefile
###tools/locale-builder/Makefile
###runtime/Makefile
###msvc/Makefile
###po/Makefile
###])

###if test x$platform_win32 = xyes; then
###   # Get rid of 'cyg' prefixes in library names
###   sed -e "s/\/cyg\//\/\//" libtool > libtool.new; mv libtool.new libtool; chmod 755 libtool
###   # libtool seems to inherit -mno-cygwin from our CFLAGS, and uses it to compile its executable
###   # wrapper scripts which use exec(). gcc has no problem compiling+linking this, but the resulting
###   # executable doesn't work...
###   sed -e "s,-mno-cygwin,,g" libtool > libtool.new; mv libtool.new libtool; chmod 755 libtool
###fi

# FIXME:
set(mcs_INSTALL ${mono_build_root}/install-sh)

###    case $INSTALL in
###    [[\\/$]]* | ?:[[\\/]]* ) set(mcs_INSTALL $INSTALL ;;)
###    *) set(mcs_INSTALL $mono_build_root/$INSTALL ;;)
###    esac

# Compute a 4 part version number into myver from ${mono_version}
string(REGEX REPLACE "([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+).*" "\\1.\\2.\\3.\\4" myver "${mono_version}.0.0.0")

set(config.make ${srcdir}/${mcsdir}/build/config.make)
#
# If we are cross compiling, we don't build in the mcs/ tree.  Let us not clobber
# any existing config.make.  This allows people to share the same source tree
# with different build directories, one native and one cross
#
if(NOT cross_compiling)
  set(not_cross_compiling yes)
endif()
if(not_cross_compiling AND enable_mcs_build)
  file(WRITE ${config.make} "prefix=${prefix}
exec_prefix=${exec_prefix}
sysconfdir=${sysconfdir}
mono_libdir=\${exec_prefix}/lib
MCS_FLAGS=$(PLATFORM_DEBUG_FLAGS)
IL_FLAGS=/debug
RUNTIME=${mono_build_root}/runtime/mono-wrapper
ILDISASM=${mono_build_root}/runtime/monodis-wrapper
INSTALL=${mcs_INSTALL}
MONO_VERSION=${myver}
")
endif()

if(platform_darwin)
  file(APPEND ${config.make} "PLATFORM=darwin\n")
endif()

if (${TARGET} STREQUAL "AMD64" AND ${platform_win32} STREQUAL "no" AND AOT_SUPPORTED)
  file(APPEND ${config.make} "ENABLE_AOT=1\n")
endif()

if (DISABLE_MCS_DOCS)
  file(APPEND ${config.make} "DISABLE_MCS_DOCS=yes\n")
endif()

###  # if we have an olive folder, override the default settings
###  if test -d $olivedir; then

###    test -w $srcdir/$olivedir/build || chmod +w $srcdir/$olivedir/build

###    if test x$cross_compiling = xno && test x$enable_olive_build != xno; then
###      echo "set(prefix $prefix" > $srcdir/$olivedir/build/config.make)
###      echo "set(exec_prefix $exec_prefix" >> $srcdir/$olivedir/build/config.make)
###      echo 'set(mono_libdir ${exec_prefix}/lib' >> $srcdir/$olivedir/build/config.make)
###      echo 'MCS_FLAGS = $(PLATFORM_DEBUG_FLAGS)' >> $srcdir/$olivedir/build/config.make
###      echo "RUNTIME = $mono_build_root/runtime/mono-wrapper" >> $srcdir/$olivedir/build/config.make
###      echo "MONO_VERSION = $myver" >> $srcdir/$olivedir/build/config.make
###	  if test x$with_moonlight = xyes; then
###        echo "WITH_MOONLIGHT = yes" >> $srcdir/$olivedir/build/config.make
###      fi
###    fi
###  fi

if(NOT libgdiplus_loc)
  set(libgdiplus_msg "assumed to be installed")
else()
  set(libgdiplus_msg ${libgdiplus_loc})
endif()

message(STATUS
"
        mcs source:    ${mcs_topdir}
        olive source:  ${olive_topdir}

	GC:	       ${gc}
	TLS:           ${with_tls}
	SIGALTSTACK:   ${with_sigaltstack}
	Engine:        ${jit_status}
	2.0 Beta:      ${PREVIEW}
	2.1 Alpha:     ${MOONLIGHT}
	JNI support:   ${jdk_headers_found}
	libgdiplus:    ${libgdiplus_msg}
	zlib:          ${zlib_msg}
	oprofile:      ${OPROFILE}
	BigArrays:     ${enable_big_arrays}
	DTrace:        ${enable_dtrace}
	Parallel Mark: ${enable_parallel_mark}
	LLVM Back End: ${enable_llvm}
	${disabled}

")

if(NOT with_static_mono)
  if(NOT platform_win32)
	ac_msg_warn("Turning off static Mono is a risk, you might run into unexpected bugs")
  endif()
endif()

if(gc STREQUAL sgen)
message("
 IMPORTANT:
 IMPORTANT: You have selected an experimental, work-in-progress 
 IMPORTANT: GC engine.  This GC engine is currently not supported
 IMPORTANT: and is not yet ready for use.  
 IMPORTANT:
 IMPORTANT: There are known problems with it, use at your own risk.
 IMPORTANT:
")
endif()

if(enable_llvm)
message("
 IMPORTANT:
 IMPORTANT: The LLVM Back End is experimental and does not work yet.
 IMPORTANT:
")
endif()

# Makefile.am

###	-rm -fr $(mcslib)/monolite-*
###	-mkdir -p $(mcslib)
###	test ! -d $(monolite) || test ! -d $(monolite).old || rm -fr $(monolite).old
###	test ! -d $(monolite) || mv -f $(monolite) $(monolite).old
###	cd $(mcslib) && { (wget -O- $(monolite_url) || curl $(monolite_url)) | gzip -d | tar xf - ; }
###	cd $(mcslib) && mv -f monolite-* monolite


#### Keep in sync with SUBDIRS
##### 'tools' is not normally built
###DIST_SUBDIRS = po libgc $(eglib_dir) mono ikvm-native support data runtime scripts man samples web tools msvc docs

###EXTRA_DIST= nls.m4 po.m4 progtest.m4 mono-uninstalled.pc.in build-mingw32.sh LICENSE mkinstalldirs

###DISTCHECK_CONFIGURE_FLAGS = EXTERNAL_MCS=false EXTERNAL_RUNTIME=false

#### Distribute the 'mcs' tree too
###dist-hook:
###	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
###	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) PROFILE=net_1_1 distdir=$$d dist-recursive
###	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0 distdir=$$d dist-recursive

###pkgconfigdir = $(libdir)/pkgconfig
###noinst_DATA = mono-uninstalled.pc
###DISTCLEANFILES= mono-uninstalled.pc

###.PHONY: get-monolite-latest mcs-do-compiler-tests compiler-tests bootstrap-world

#### building with monolite
set(mcslib ${mcs_topdir}/class/lib)
set(monolite ${mcslib}/monolite)
set(monolite_url http://mono.ximian.com/daily/monolite-latest.tar.gz)
add_custom_target(get-monolite-latest
COMMAND	-rm -fr ${mcslib}/monolite-*
COMMAND	-mkdir -p ${mcslib}
COMMAND	test ! -d ${monolite} || test ! -d ${monolite}.old || rm -fr ${monolite}.old
COMMAND	test ! -d ${monolite} || mv -f ${monolite} ${monolite}.old
COMMAND	cd ${mcslib} && { (wget -O- ${monolite_url} || curl ${monolite_url}) | gzip -d | tar xf - \; }
COMMAND	cd ${mcslib} && mv -f monolite-* monolite
)

###compiler-tests: mcs-do-clean
###	$(MAKE) all
###	$(MAKE) mcs-do-compiler-tests

###compiler-tests-net_2_0:
###	-rm -f $(mcs_topdir)/build/common/Consts.cs.save
###	-mv -f $(mcs_topdir)/build/common/Consts.cs $(mcs_topdir)/build/common/Consts.cs.save
###	cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0_bootstrap clean
###	cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0 clean
###	-mv -f $(mcs_topdir)/build/common/Consts.cs.save $(mcs_topdir)/build/common/Consts.cs
###	$(MAKE) all
###	$(MAKE) build_profiles=net_2_0 mcs-do-compiler-tests

###bootstrap-world: compiler-tests
###	$(MAKE) install

###bootstrap-world-net_2_0: compiler-tests-net_2_0
###	$(MAKE) install

#### internal targets
###mcs-do-clean:
###	cd runtime && $(MAKE) clean-local
###	cd mono/tests && $(MAKE) clean
###mcs-do-compiler-tests:
###	cd runtime && $(MAKE) test_select='TEST_SUBDIRS="tests errors"' check-local
###	cd mono/tests && $(MAKE) check

###win32getdeps:
###	wget http://www.go-mono.com/archive/pkgconfig-0.11-20020310.zip
###	wget http://www.go-mono.com/archive/glib-2.0.4-20020703.zip 
###	wget http://www.go-mono.com/archive/glib-dev-2.0.4-20020703.zip 
###	wget http://www.go-mono.com/archive/libiconv-1.7.zip 
###	wget http://www.go-mono.com/archive/libiconv-dev-1.7.zip 
###	wget http://www.go-mono.com/archive/libintl-0.10.40-20020101.zip
###	unzip -n -d / pkgconfig-0.11-20020310.zip
###	unzip -n -d / glib-2.0.4-20020703.zip
###	unzip -n -d / glib-dev-2.0.4-20020703.zip
###	unzip -n -d / libiconv-1.7.zip
###	unzip -n -d / libiconv-dev-1.7.zip
###	unzip -n -d / libintl-0.10.40-20020101.zip

###win32setup:
###	makensis /DMILESTONE=$(VERSION) /DSOURCE_INSTALL_DIR=$(SOURCE_INSTALL_DIR) /DBUILDNUM=$(BUILDNUM) monowiz.win32.nsi

###bootstrap: all
###	@echo "*** 'make bootstrap' is obsolete.  Just run 'make' to perform a combined mono+mcs build"
###	exit 1

###patch-quiet:
###	find mono -name Makefile -exec scripts/patch-quiet.sh {} \;
###	find libgc -name Makefile -exec scripts/patch-quiet.sh {} \;

###update-csproj:
###	(cd $(mcs_topdir)/build/csproj && gmcs genproj.cs) && (cd runtime; make V=1 extra_targets=csproj-local)
