
macro(append_extra_cryptography_libs NativeLibsExtra)
    if(CMAKE_STATIC_LIB_LINK)
       set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
    endif(CMAKE_STATIC_LIB_LINK)
    
    if(CLR_CMAKE_TARGET_ANDROID AND NOT CROSS_ROOTFS)
        # TEMP: consume OpenSSL dependencies from external sources via env. variables
        set(OPENSSL_FOUND 1)
        set(OPENSSL_INCLUDE_DIR $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/include)
        if(CLR_CMAKE_TARGET_ARCH_ARM64)
            set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.arm64-v8a/libcrypto.so)
            set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.arm64-v8a/libssl.so)
        elseif(CLR_CMAKE_TARGET_ARCH_ARM)
            set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.armeabi-v7a/libcrypto.so)
            set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.armeabi-v7a/libssl.so)
        elseif(CLR_CMAKE_TARGET_ARCH_I386)
            set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.x86/libcrypto.so)
            set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.x86/libssl.so)
        else()
            set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.x86_64/libcrypto.so)
            set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.x86_64/libssl.so)
        endif()
    else()
        find_package(OpenSSL)
    endif()
    
    if(NOT OPENSSL_FOUND)
        message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (on Linux, but this may vary by distro) or openssl (on macOS) !!!. See the requirements document for your specific operating system: https://github.com/dotnet/runtime/tree/master/docs/workflow/requirements.")
    endif(NOT OPENSSL_FOUND)
    
    
    if (FEATURE_DISTRO_AGNOSTIC_SSL OR CLR_CMAKE_TARGET_OSX)
        # Link with libdl.so to get the dlopen / dlsym / dlclose
        list(APPEND ${NativeLibsExtra} dl)
    else()
        list(APPEND ${NativeLibsExtra} ${OPENSSL_CRYPTO_LIBRARY} ${OPENSSL_SSL_LIBRARY})
    endif()
endmacro()
