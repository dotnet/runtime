
macro(append_extra_compression_libs NativeLibsExtra)
  # TODO: remove the mono-style HOST_ variable checks once Mono is using eng/native/configureplatform.cmake to define the CLR_CMAKE_TARGET_ defines
  if (CLR_CMAKE_TARGET_BROWSER OR HOST_BROWSER)
      # nothing special to link
  elseif (CLR_CMAKE_TARGET_ANDROID OR HOST_ANDROID)
      # need special case here since we want to link against libz.so but find_package() would resolve libz.a
      set(ZLIB_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_SUNOS OR HOST_SOLARIS)
      set(ZLIB_LIBRARIES z m)
  else ()
      find_package(ZLIB REQUIRED)
  endif ()
  list(APPEND ${NativeLibsExtra} ${ZLIB_LIBRARIES})
endmacro()
