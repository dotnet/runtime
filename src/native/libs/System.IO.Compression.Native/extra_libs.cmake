
macro(append_extra_compression_libs NativeLibsExtra)
  if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)
    list(APPEND ${NativeLibsExtra} ${BROTLIDEC} ${BROTLIENC})
  endif ()
endmacro()
