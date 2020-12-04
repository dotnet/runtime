
macro(append_extra_compression_libs NativeLibsExtra)
  if (CLR_CMAKE_TARGET_BROWSER)
      # nothing special to link
  elseif (CLR_CMAKE_TARGET_ANDROID)
      # need special case here since we want to link against libz.so but find_package() would resolve libz.a
      set(ZLIB_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_SUNOS)
      set(ZLIB_LIBRARIES z m)
  else ()
      find_package(ZLIB REQUIRED)
  endif ()
  list(APPEND ${NativeLibsExtra} ${ZLIB_LIBRARIES})
endmacro()
