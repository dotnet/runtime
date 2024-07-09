
macro(append_extra_cryptography_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION_LIBRARY CoreFoundation)
    find_library(SECURITY_LIBRARY Security)
    find_library(NETWORK_LIBRARY Network)

    list(APPEND ${NativeLibsExtra} ${COREFOUNDATION_LIBRARY} ${NETWORK_LIBRARY} ${SECURITY_LIBRARY})

    if (CLR_CMAKE_TARGET_OSX)
        find_library(CRYPTOKIT_LIBRARY CryptoKit)

        list(APPEND ${NativeLibsExtra} ${CRYPTOKIT_LIBRARY} -L/usr/lib/swift -lobjc -lswiftCore -lswiftFoundation)
    endif()
endmacro()
