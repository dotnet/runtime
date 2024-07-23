macro(append_extra_compression_libs NativeLibsExtra)
  set(ZLIB_LIBRARIES "")
  # TODO: remove the mono-style HOST_ variable checks once Mono is using eng/native/configureplatform.cmake to define the CLR_CMAKE_TARGET_ defines
  if (CLR_CMAKE_TARGET_BROWSER OR HOST_BROWSER OR CLR_CMAKE_TARGET_WASI OR HOST_WASI)
      # nothing special to link
  elseif (CLR_CMAKE_HOST_ARCH_ARMV6)
      find_package(ZLIB REQUIRED)
      list(APPEND ZLIB_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
      find_package(ZLIB REQUIRED)
      list(APPEND ZLIB_LIBRARIES m)
  else()
    # 'z' is used in:
    # - Platforms that set CMAKE_USE_SYSTEM_ZLIB to true, like Android.
    # - When it is set to true via CLI using --cmakeargs.
    # 'zlib' represents our in-tree zlib, and is used in all other platforms
    # that don't meet any of the previous special requirements, like most
    # regular Unix and Windows builds.
    list(APPEND ZLIB_LIBRARIES $<$<BOOL:$<TARGET_PROPERTY:CLR_CMAKE_USE_SYSTEM_ZLIB>>:z:zlib>)
  endif ()
  list(APPEND ${NativeLibsExtra} ${ZLIB_LIBRARIES})

  if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)

    list(APPEND ${NativeLibsExtra} ${BROTLIDEC} ${BROTLIENC})
  endif ()
endmacro()
