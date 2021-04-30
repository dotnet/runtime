
macro(append_extra_cryptography_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION_LIBRARY CoreFoundation)
    find_library(SECURITY_LIBRARY Security)
    find_library(FOUNDATION_LIBRARY Foundation)

    list(APPEND ${NativeLibsExtra} ${COREFOUNDATION_LIBRARY} ${SECURITY_LIBRARY} ${FOUNDATION_LIBRARY})
endmacro()
