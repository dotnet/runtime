
project(System.Security.Cryptography.Native)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Issue 2546 - Many deprecation warnings in System.Security.Cryptography.Native on Mac OS X
add_compile_options(-Wno-deprecated-declarations)

# These are happening inside of OpenSSL-defined macros out of our control
add_compile_options(-Wno-cast-align)

add_definitions(-DPIC=1)

find_library(CRYPTO NAMES crypto)
if(CRYPTO STREQUAL CRYPTO-NOTFOUND)
    message(SEND_ERROR "!!! Cannot find libcrypto and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (or the appropriate package for your platform) !!!")
    return()
endif()

set(NATIVECRYPTO_SOURCES
    openssl.c
)

add_library(System.Security.Cryptography.Native
    SHARED
    ${NATIVECRYPTO_SOURCES}
)

# Disable the "lib" prefix.
set_target_properties(System.Security.Cryptography.Native PROPERTIES PREFIX "")

target_link_libraries(System.Security.Cryptography.Native
  ${CRYPTO}
)

install (TARGETS System.Security.Cryptography.Native DESTINATION .)
