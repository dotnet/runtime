
macro(append_extra_cryptography_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION_LIBRARY CoreFoundation)
    find_library(SECURITY_LIBRARY Security)

    list(APPEND ${NativeLibsExtra} ${COREFOUNDATION_LIBRARY} ${SECURITY_LIBRARY})

    if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        find_library(CRYPTOKIT_LIBRARY CryptoKit)

        if (CLR_CMAKE_TARGET_OSX)
            list(APPEND ${NativeLibsExtra} ${CRYPTOKIT_LIBRARY} -L/usr/lib/swift -lobjc -lswiftCore -lswiftFoundation)
        endif()
    endif()
endmacro()
