function(append_extra_networking_apple_libs NativeLibsExtra)
    find_library(COREFOUNDATION CoreFoundation)
    find_library(SECURITY Security)
    find_library(NETWORK Network)
    find_library(FOUNDATION Foundation)

    set(${NativeLibsExtra} ${${NativeLibsExtra}} ${COREFOUNDATION} ${SECURITY} ${NETWORK} ${FOUNDATION} PARENT_SCOPE)
endfunction() 