macro(append_extra_compression_libs NativeLibsExtra)
  if (CLR_CMAKE_TARGET_ANDROID OR HOST_ANDROID)
      # need special case here since we want to link against libz.so but find_package() would resolve libz.a
      set(ZLIB_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
      find_package(ZLIB REQUIRED)
      set(ZLIB_LIBRARIES ${ZLIB_LIBRARIES} m)
  else()
    list(APPEND ${NativeLibsExtra} zlib)
  endif ()

  if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)

    list(APPEND ${NativeLibsExtra} ${BROTLIDEC} ${BROTLIENC})
  endif ()
endmacro()
