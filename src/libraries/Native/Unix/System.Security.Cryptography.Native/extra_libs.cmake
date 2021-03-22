
macro(append_extra_cryptography_libs NativeLibsExtra)
    if(CMAKE_STATIC_LIB_LINK)
       set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
    endif(CMAKE_STATIC_LIB_LINK)

    find_package(OpenSSL)

    if(NOT OPENSSL_FOUND)
        message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (on Linux, but this may vary by distro) or openssl (on macOS) !!!. See the requirements document for your specific operating system: https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements.")
    endif(NOT OPENSSL_FOUND)
    
    
    if (FEATURE_DISTRO_AGNOSTIC_SSL OR CLR_CMAKE_TARGET_OSX)
        # Link with libdl.so to get the dlopen / dlsym / dlclose
        list(APPEND ${NativeLibsExtra} dl)
    else()
        list(APPEND ${NativeLibsExtra} ${OPENSSL_CRYPTO_LIBRARY} ${OPENSSL_SSL_LIBRARY})
    endif()
endmacro()
