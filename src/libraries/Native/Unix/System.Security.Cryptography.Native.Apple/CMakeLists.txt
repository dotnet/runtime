
project(System.Security.Cryptography.Native.Apple)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC=1)

find_library(COREFOUNDATION_LIBRARY CoreFoundation)
find_library(SECURITY_LIBRARY Security)

set(NATIVECRYPTO_SOURCES
    pal_digest.cpp
    pal_ecc.cpp
    pal_hmac.cpp
    pal_keychain.cpp
    pal_random.cpp
    pal_rsa.cpp
    pal_sec.cpp
    pal_seckey.cpp
    pal_signverify.cpp
    pal_ssl.cpp
    pal_symmetric.cpp
    pal_trust.cpp
    pal_x509.cpp
    pal_x509chain.cpp
)

add_library(System.Security.Cryptography.Native.Apple
    SHARED
    ${NATIVECRYPTO_SOURCES}
    ${VERSION_FILE_PATH}
)

add_library(System.Security.Cryptography.Native.Apple-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix.
set_target_properties(System.Security.Cryptography.Native.Apple PROPERTIES PREFIX "")

# Disable the "lib" prefix and override default name
set_target_properties(System.Security.Cryptography.Native.Apple-Static PROPERTIES PREFIX "")
set_target_properties(System.Security.Cryptography.Native.Apple-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Apple CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.Security.Cryptography.Native.Apple
    ${COREFOUNDATION_LIBRARY}
    ${SECURITY_LIBRARY}
)

install_library_and_symbols (System.Security.Cryptography.Native.Apple)
install (TARGETS System.Security.Cryptography.Native.Apple-Static DESTINATION .)
