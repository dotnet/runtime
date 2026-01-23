# SPDX-License-Identifier: 0BSD

#############################################################################
#
# SYNOPSIS
#
#   TUKLIB_INTEGER
#
# DESCRIPTION
#
#   Checks for tuklib_integer.h:
#     - Endianness
#     - Does the compiler or the operating system provide byte swapping macros
#     - Does the hardware support fast unaligned access to 16-bit, 32-bit,
#       and 64-bit integers
#
#############################################################################
#
# Author: Lasse Collin
#
#############################################################################

AC_DEFUN_ONCE([TUKLIB_INTEGER], [
AC_REQUIRE([TUKLIB_COMMON])
AC_REQUIRE([AC_C_BIGENDIAN])

AC_MSG_CHECKING([if __builtin_bswap16/32/64 are supported])
AC_LINK_IFELSE([AC_LANG_PROGRAM([[]],
			[[__builtin_bswap16(1);
			__builtin_bswap32(1);
			__builtin_bswap64(1);]])],
[
	AC_DEFINE([HAVE___BUILTIN_BSWAPXX], [1],
		[Define to 1 if the GNU C extensions
		__builtin_bswap16/32/64 are supported.])
	AC_MSG_RESULT([yes])
], [
	AC_MSG_RESULT([no])

	# Look for other byteswapping methods.
	AC_CHECK_HEADERS([byteswap.h sys/endian.h sys/byteorder.h], [break])

	# Even if we have byteswap.h we may lack the specific macros/functions.
	if test x$ac_cv_header_byteswap_h = xyes ; then
		m4_foreach([FUNC], [bswap_16,bswap_32,bswap_64], [
			AC_MSG_CHECKING([if FUNC is available])
			AC_LINK_IFELSE([AC_LANG_SOURCE([
#include <byteswap.h>
int
main(void)
{
	FUNC[](42);
	return 0;
}
			])], [
				AC_DEFINE(HAVE_[]m4_toupper(FUNC), [1],
					[Define to 1 if] FUNC [is available.])
				AC_MSG_RESULT([yes])
			], [AC_MSG_RESULT([no])])

		])dnl
	fi
])

# On archs that we use tuklib_integer_strict_align() (see below), we need
# objdump to detect support for unaligned access. (Libtool needs objdump
# too, so Libtool does this same tool check as well.)
AC_CHECK_TOOL([OBJDUMP], [objdump], [false])

# An internal helper that attempts to detect if -mstrict-align or
# -mno-strict-align is in effect. This sets enable_unaligned_access=yes
# if compilation succeeds and the regex passed as an argument does *not*
# match the objdump output of a check program. Otherwise this sets
# enable_unaligned_access=no.
tuklib_integer_strict_align ()
{
	# First guess no.
	enable_unaligned_access=no

	# Force -O2 because without optimizations the memcpy()
	# won't be optimized out.
	tuklib_integer_saved_CFLAGS=$CFLAGS
	CFLAGS="$CFLAGS -O2"
	AC_COMPILE_IFELSE([AC_LANG_SOURCE([[
			#include <string.h>
			unsigned int check_strict_align(const void *p)
			{
				unsigned int i;
				memcpy(&i, p, sizeof(i));
				return i;
			}
		]])], [
			# Disassemble the test function from the object file.
			if $OBJDUMP -d conftest.$ac_objext > conftest.s ; then
				# This function should be passed a regex that
				# matches if there are instructions that load
				# unsigned bytes. Such instructions indicate
				# that -mstrict-align is in effect.
				#
				# NOTE: Use braces to avoid M4 parameter
				# expansion.
				if grep -- "${1}" conftest.s > /dev/null ; then
					:
				else
					# No single-byte unsigned load
					# instructions were found,
					# so it seems that -mno-strict-align
					# is in effect.
					# Override our earlier guess.
					enable_unaligned_access=yes
				fi
			fi
		])
	CFLAGS=$tuklib_integer_saved_CFLAGS
}

AC_MSG_CHECKING([if unaligned memory access should be used])
AC_ARG_ENABLE([unaligned-access], AS_HELP_STRING([--enable-unaligned-access],
		[Enable if the system supports *fast* unaligned memory access
		with 16-bit, 32-bit, and 64-bit integers. By default,
		this is enabled on x86, x86-64,
		32/64-bit big endian PowerPC,
		64-bit little endian PowerPC,
		and some ARM, ARM64, and RISC-V systems.]),
	[], [enable_unaligned_access=auto])
if test "x$enable_unaligned_access" = xauto ; then
	# NOTE: There might be other architectures on which unaligned access
	# is fast.
	case $host_cpu in
		i?86|x86_64|powerpc|powerpc64|powerpc64le)
			enable_unaligned_access=yes
			;;
		arm*|riscv*)
			# On 32-bit ARM, GCC and Clang
			# #define __ARM_FEATURE_UNALIGNED
			# if and only if unaligned access is supported.
			#
			# RISC-V C API Specification says that if
			# __riscv_misaligned_fast is defined then
			# unaligned access is known to be fast.
			#
			# MSVC is handled as a special case: We assume that
			# 32-bit ARM supports fast unaligned access.
			# If MSVC gets RISC-V support then this will assume
			# fast unaligned access on RISC-V too.
			AC_COMPILE_IFELSE([AC_LANG_SOURCE([
				#if !defined(__ARM_FEATURE_UNALIGNED) \
					&& !defined(__riscv_misaligned_fast) \
					&& !defined(_MSC_VER)
				compile error
				#endif
				int main(void) { return 0; }
			])],
			[enable_unaligned_access=yes],
			[enable_unaligned_access=no])
			;;
		aarch64*)
			# On ARM64, Clang defines __ARM_FEATURE_UNALIGNED
			# if and only if unaligned access is supported.
			# However, GCC (at least up to 15.2.0) defines it
			# even when using -mstrict-align, so autodetection
			# with this macro doesn't work with GCC on ARM64.
			# (It does work on 32-bit ARM.) See:
			#
			# https://gcc.gnu.org/bugzilla/show_bug.cgi?id=111555
			#
			# We need three checks:
			#
			# 1. If __ARM_FEATURE_UNALIGNED is defined and the
			#    compiler isn't GCC, unaligned access is enabled.
			#    If the compiler is MSVC, unaligned access is
			#    enabled even without __ARM_FEATURE_UNALIGNED.
			AC_COMPILE_IFELSE([AC_LANG_SOURCE([
				#if defined(__ARM_FEATURE_UNALIGNED) \
					&& (!defined(__GNUC__) \
						|| defined(__clang__))
				#elif defined(_MSC_VER)
				#else
				compile error
				#endif
				int main(void) { return 0; }
			])], [enable_unaligned_access=yes])

			# 2. If __ARM_FEATURE_UNALIGNED is not defined,
			#    unaligned access is disabled.
			if test "x$enable_unaligned_access" = xauto ; then
				AC_COMPILE_IFELSE([AC_LANG_SOURCE([
					#ifdef __ARM_FEATURE_UNALIGNED
					compile error
					#endif
					int main(void) { return 0; }
				])], [enable_unaligned_access=no])
			fi

			# 3. Use heuristics to detect if -mstrict-align is
			#    in effect when building with GCC.
			if test "x$enable_unaligned_access" = xauto ; then
				[tuklib_integer_strict_align \
						'[[:blank:]]ldrb[[:blank:]]']
			fi
			;;
		loongarch*)
			# See sections 7.4, 8.1, and 8.2:
			# https://github.com/loongson/la-softdev-convention/blob/v0.2/la-softdev-convention.adoc
			#
			# That is, desktop and server processors likely support
			# unaligned access in hardware but embedded processors
			# might not. GCC defaults to -mno-strict-align and so
			# do majority of GNU/Linux distributions. As of
			# GCC 15.2, there is no predefined macro to detect
			# if -mstrict-align or -mno-strict-align is in effect.
			# Use heuristics based on compiler output.
			[
				tuklib_integer_strict_align \
						'[[:blank:]]ld\.bu[[:blank:]]'
			]
			;;
		*)
			enable_unaligned_access=no
			;;
	esac
fi
if test "x$enable_unaligned_access" = xyes ; then
	AC_DEFINE([TUKLIB_FAST_UNALIGNED_ACCESS], [1], [Define to 1 if
		the system supports fast unaligned access to 16-bit,
		32-bit, and 64-bit integers.])
	AC_MSG_RESULT([yes])
else
	AC_MSG_RESULT([no])
fi

AC_MSG_CHECKING([if unsafe type punning should be used])
AC_ARG_ENABLE([unsafe-type-punning],
	AS_HELP_STRING([--enable-unsafe-type-punning],
		[This introduces strict aliasing violations and may result
		in broken code. However, this might improve performance in
		some cases, especially with old compilers (e.g.
		GCC 3 and early 4.x on x86, GCC < 6 on ARMv6 and ARMv7).]),
	[], [enable_unsafe_type_punning=no])
if test "x$enable_unsafe_type_punning" = xyes ; then
	AC_DEFINE([TUKLIB_USE_UNSAFE_TYPE_PUNNING], [1], [Define to 1 to use
		unsafe type punning, e.g. char *x = ...; *(int *)x = 123;
		which violates strict aliasing rules and thus is
		undefined behavior and might result in broken code.])
	AC_MSG_RESULT([yes])
else
	AC_MSG_RESULT([no])
fi

AC_MSG_CHECKING([if __builtin_assume_aligned is supported])
AC_LINK_IFELSE([AC_LANG_PROGRAM([[]], [[__builtin_assume_aligned("", 1);]])],
	[
		AC_DEFINE([HAVE___BUILTIN_ASSUME_ALIGNED], [1],
			[Define to 1 if the GNU C extension
			__builtin_assume_aligned is supported.])
		AC_MSG_RESULT([yes])
	], [
		AC_MSG_RESULT([no])
	])
])dnl
