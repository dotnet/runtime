
macro(append_extra_cryptography_libs NativeLibsExtra)
    if(CMAKE_STATIC_LIB_LINK)
       set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
    endif(CMAKE_STATIC_LIB_LINK)

    # This is bad and wrong, but good enough to satisfy the build
    # We only care about having "enough" OpenSSL to get the native lib built
    # here, and it's on the end user to ship libssl/libcrypto from Google
    if(FORCE_ANDROID_OPENSSL)
        set(OPENSSL_CRYPTO_LIBRARY /usr/lib/x86_64-linux-gnu/libcrypto.so)
        set(OPENSSL_SSL_LIBRARY /usr/lib/x86_64-linux-gnu/libssl.so)
        # Things get more wrong. We need Desktop OpenSSL headers, but
        # /usr/include is special cased and forbidden. We need to copy
        # the headers to a different location and use them
        if(NOT DEFINED OPENSSL_INCLUDE_DIR)
            string(RANDOM LENGTH 24 _s)
            set(OPENSSL_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${_s}/opensslheaders CACHE PATH "temporary directory")
            file(MAKE_DIRECTORY ${OPENSSL_INCLUDE_DIR})
            file(COPY /usr/include/openssl DESTINATION ${OPENSSL_INCLUDE_DIR})
            file(GLOB_RECURSE opensslconf /usr/include/*/openssl/*conf*.h)
            file(COPY ${opensslconf} DESTINATION ${OPENSSL_INCLUDE_DIR}/openssl/)
        endif()
    endif()

    find_package(OpenSSL)

    if(NOT OPENSSL_FOUND)
        message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (on Linux, but this may vary by distro) or openssl (on macOS) !!!. See the requirements document for your specific operating system: https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements.")
    endif(NOT OPENSSL_FOUND)
    
    
    if (FEATURE_DISTRO_AGNOSTIC_SSL OR CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST)
        # Link with libdl.so to get the dlopen / dlsym / dlclose
        list(APPEND ${NativeLibsExtra} dl)
    else()
        list(APPEND ${NativeLibsExtra} ${OPENSSL_CRYPTO_LIBRARY} ${OPENSSL_SSL_LIBRARY})
    endif()
endmacro()
