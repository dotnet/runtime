set(TARGET_ARCH_NAME $ENV{TARGET_BUILD_ARCH})
set(CROSS_ROOTFS $ENV{ROOTFS_DIR})

macro(set_cache_value)
  set(${ARGV0} ${ARGV1} CACHE STRING "Result from TRY_RUN" FORCE)
  set(${ARGV0}__TRYRUN_OUTPUT "dummy output" CACHE STRING "Output from TRY_RUN" FORCE)
endmacro()

if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv7-alpine-linux-musleabihf OR
   EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv6-alpine-linux-musleabihf OR
   EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/aarch64-alpine-linux-musl)

  set(ALPINE_LINUX 1)
elseif(EXISTS ${CROSS_ROOTFS}/bin/freebsd-version)
  set(FREEBSD 1)
  set(CMAKE_SYSTEM_NAME FreeBSD)
  set(CLR_CMAKE_TARGET_OS FreeBSD)
elseif(EXISTS ${CROSS_ROOTFS}/usr/platform/i86pc)
  set(ILLUMOS 1)
  set(CLR_CMAKE_TARGET_OS SunOS)
endif()

if(TARGET_ARCH_NAME MATCHES "^(armel|arm|arm64|x86)$" OR FREEBSD OR ILLUMOS)
  set_cache_value(HAVE_CLOCK_MONOTONIC_EXITCODE 0)
  set_cache_value(HAVE_CLOCK_REALTIME_EXITCODE 0)

  if(ALPINE_LINUX)
    set_cache_value(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP_EXITCODE 1)
  else()
    set_cache_value(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP_EXITCODE 0)
  endif()

  if(FREEBSD)
    set_cache_value(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP 1)
    set_cache_value(HAVE_CLOCK_MONOTONIC 1)
    set_cache_value(HAVE_CLOCK_REALTIME 1)
  endif()
else()
  message(FATAL_ERROR "Arch is ${TARGET_ARCH_NAME}. Only armel, arm, arm64 and x86 are supported!")
endif()
