macro(append_extra_compression_libs NativeLibsExtra)
  set(ZLIB_LIBRARIES "")
  # TODO: remove the mono-style HOST_ variable checks once Mono is using eng/native/configureplatform.cmake to define the CLR_CMAKE_TARGET_ defines
  if (CLR_CMAKE_TARGET_BROWSER OR HOST_BROWSER OR CLR_CMAKE_TARGET_WASI OR HOST_WASI)
      # nothing special to link
  elseif (CLR_CMAKE_TARGET_ANDROID OR HOST_ANDROID)
      # need special case here since we want to link against libz.so but find_package() would resolve libz.a
      list(APPEND ZLIB_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
      find_package(ZLIB REQUIRED)
      list(APPEND ZLIB_LIBRARIES m)
  else()
    list(APPEND ZLIB_LIBRARIES zlib)
  endif ()
  list(APPEND ${NativeLibsExtra} ${ZLIB_LIBRARIES})

  if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)

    list(APPEND ${NativeLibsExtra} ${BROTLIDEC} ${BROTLIENC})
  endif ()
endmacro()
