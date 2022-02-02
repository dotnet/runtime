
macro(append_extra_cryptography_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION_LIBRARY CoreFoundation)
    find_library(SECURITY_LIBRARY Security)

    list(APPEND ${NativeLibsExtra} ${COREFOUNDATION_LIBRARY} ${SECURITY_LIBRARY})
endmacro()
