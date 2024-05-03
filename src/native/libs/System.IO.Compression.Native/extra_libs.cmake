
macro(append_extra_compression_libs NativeLibsExtra)
  # TODO: remove the mono-style HOST_ variable checks once Mono is using eng/native/configureplatform.cmake to define the CLR_CMAKE_TARGET_ defines
  if (CLR_CMAKE_TARGET_BROWSER OR HOST_BROWSER OR CLR_CMAKE_TARGET_WASI OR HOST_WASI)
      # nothing special to link
  elseif (CLR_CMAKE_TARGET_ANDROID OR HOST_ANDROID)
      # need special case here since we want to link against libz.so but find_package() would resolve libz.a
      set(ZLIBNG_LIBRARIES z)
  elseif (CLR_CMAKE_TARGET_SUNOS OR HOST_SOLARIS)
      set(ZLIBNG_LIBRARIES z m)
  else ()
      # TODO: We don't want zlib anymore, but if I change it to ZLIBNG REQUIRED or ZLIB-NG REQUIRED,
      # this is unable to find zlib-ng in my linux machine. It seems zlib-ng is not available by default in
      # the package manager in Ubuntu, unlike zlib.
      # So do we still want to keep this? If yes, how should I get this to work?
      # find_package(ZLIB REQUIRED)
      set(ZLIBNG_LIBRARIES ${ZLIBNG_LIBRARIES} m)
  endif ()
  list(APPEND ${NativeLibsExtra} ${ZLIBNG_LIBRARIES})

  if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)

    list(APPEND ${NativeLibsExtra} ${BROTLIDEC} ${BROTLIENC})
  endif ()
endmacro()
