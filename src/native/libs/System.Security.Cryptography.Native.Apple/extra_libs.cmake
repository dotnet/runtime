
macro(append_extra_cryptography_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION_LIBRARY CoreFoundation)
    find_library(SECURITY_LIBRARY Security)
    find_library(CRYPTOKIT_LIBRARY CryptoKit)

    list(APPEND ${NativeLibsExtra} ${COREFOUNDATION_LIBRARY} ${SECURITY_LIBRARY} ${CRYPTOKIT_LIBRARY} -L/usr/lib/swift -lobjc -lswiftCore -lswiftFoundation)
endmacro()
