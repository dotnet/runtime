set(TARGET_ARCH_NAME $ENV{TARGET_BUILD_ARCH})

macro(set_cache_value)
  set(${ARGV0} ${ARGV1} CACHE STRING "Result from TRY_RUN" FORCE)
endmacro()

if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv6-alpine-linux-musleabihf OR
   EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/aarch64-alpine-linux-musl)
  
   SET(ALPINE_LINUX 1)
else()
   SET(ALPINE_LINUX 0)
endif()

if(TARGET_ARCH_NAME MATCHES "^(armel|arm|arm64|x86)$")
  set_cache_value(HAVE_CLOCK_MONOTONIC_EXITCODE 0)
  set_cache_value(HAVE_CLOCK_REALTIME_EXITCODE 0)

  if(ALPINE_LINUX)
    set_cache_value(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP_EXITCODE 1)
  else()
    set_cache_value(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP_EXITCODE 0)
  endif()
else()
  message(FATAL_ERROR "Arch is ${TARGET_ARCH_NAME}. Only armel, arm, arm64 and x86 are supported!")
endif()
