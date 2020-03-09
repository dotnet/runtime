project(System.Security.Cryptography.Native.Apple C)

find_library(COREFOUNDATION_LIBRARY CoreFoundation)
find_library(SECURITY_LIBRARY Security)

set(NATIVECRYPTO_SOURCES
    pal_digest.c
    pal_hmac.c
    pal_random.c
    pal_symmetric.c
)

if (NOT CLR_CMAKE_TARGET_IOS)  # TODO: reenable more sources
    set(NATIVECRYPTO_SOURCES
        ${NATIVECRYPTO_SOURCES}
        pal_ecc.c
        pal_keyagree.c
        pal_keychain.c
        pal_rsa.c
        pal_sec.c
        pal_seckey.c
        pal_signverify.c
        pal_ssl.c
        pal_trust.c
        pal_x509.c
        pal_x509chain.c
    )
endif()

add_library(System.Security.Cryptography.Native.Apple
    SHARED
    ${NATIVECRYPTO_SOURCES}
    ${VERSION_FILE_PATH}
)

add_library(System.Security.Cryptography.Native.Apple-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Apple-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Apple CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.Security.Cryptography.Native.Apple
    ${COREFOUNDATION_LIBRARY}
    ${SECURITY_LIBRARY}
)

install_library_and_symbols (System.Security.Cryptography.Native.Apple)
install (TARGETS System.Security.Cryptography.Native.Apple-Static DESTINATION .)
