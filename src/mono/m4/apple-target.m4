# Usage:
# AC_MONO_APPLE_TARGET(target-name, action-if-found, action-if-not-found)
#
# Checks whether `target-name` is defined in "TargetConditionals.h"
#
AC_DEFUN([AC_MONO_APPLE_TARGET], [
	AC_MONO_APPLE_AVAILABLE([$1], [for $1], [$1 == 1], $2, $3)
])


# Usage:
# AC_MONO_APPLE_AVAILABLE(name, message, conditional, action-if-found, action-if-not-found)
#
# Checks for `conditional` using "TargetConditionals.h" and "AvailabilityMacros.h"
#
AC_DEFUN([AC_MONO_APPLE_AVAILABLE], [
	# cache the compilation check
	AC_CACHE_CHECK([$2], [ac_cv_apple_available_[]$1], [
		AC_TRY_COMPILE([
			#include "TargetConditionals.h"
			#include "AvailabilityMacros.h"
		],[
			#if !($3)
			#error failed
			#endif
		], [
			ac_cv_apple_available_[]$1=yes
		], [
			ac_cv_apple_available_[]$1=no
		])
	])
	# keep the actions out of the cache check because they need to be always executed.
	if test x$ac_cv_apple_available_[]$1 = xyes; then
		$1=yes
		[$4]
	else
		$1=no
		[$5]
	fi
])
