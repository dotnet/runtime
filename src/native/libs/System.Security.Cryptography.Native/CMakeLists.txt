project(System.Security.Cryptography.Native C)

# These are happening inside of OpenSSL-defined macros out of our control
add_compile_options(-Wno-cast-align)
add_compile_options(-Wno-reserved-id-macro)
add_compile_options(-Wno-documentation)
add_compile_options(-Wno-used-but-marked-unused)

add_definitions(-DOPENSSL_API_COMPAT=0x10100000L)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_cryptography_libs(NATIVE_LIBS_EXTRA)

include_directories(${OPENSSL_INCLUDE_DIR})

set(NATIVECRYPTO_SOURCES
    apibridge.c
    apibridge_30.c
    openssl.c
    pal_asn1.c
    pal_bignum.c
    pal_bio.c
    pal_dsa.c
    pal_ecdsa.c
    pal_ecc_import_export.c
    pal_eckey.c
    pal_err.c
    pal_evp.c
    pal_evp_pkey.c
    pal_evp_pkey_dsa.c
    pal_evp_pkey_ecdh.c
    pal_evp_pkey_eckey.c
    pal_evp_pkey_rsa.c
    pal_evp_cipher.c
    pal_hmac.c
    pal_ocsp.c
    pal_pkcs7.c
    pal_ssl.c
    pal_x509.c
    pal_x509_name.c
    pal_x509_root.c
    pal_x509ext.c
)

if (LOCAL_BUILD)
    set(CMAKE_POSITION_INDEPENDENT_CODE ON)
    add_definitions(-DLOCAL_BUILD)
    add_definitions(-DPALEXPORT=EXTERN_C)
    add_definitions(-DTARGET_UNIX)
    # For minipal files
    include_directories(../../)
    include_directories(../Common)
    include_directories(${CMAKE_CURRENT_BINARY_DIR})
    configure_file(
        ../Common/pal_config.h.in
        ${CMAKE_CURRENT_BINARY_DIR}/pal_config.h)
endif()


# Always build portable on macOS because OpenSSL is not a system component
# and our prebuilts should not assume a specific ABI version for the types
# that use OpenSSL at runtime.
if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST)
    set(FEATURE_DISTRO_AGNOSTIC_SSL True)

    # by default uninitialized variables like the shim _ptr functions, will turn into common symbols
    # and on OSX those have linking problems in libraries (ELF vs. Mach-O difference)
    add_compile_options(-fno-common)
endif()

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    list(APPEND NATIVECRYPTO_SOURCES
        opensslshim.c
    )
    add_definitions(-DFEATURE_DISTRO_AGNOSTIC_SSL)
    add_compile_options(-pthread)
    add_linker_flag(-pthread)
endif()

add_library(objlib OBJECT ${NATIVECRYPTO_SOURCES} ${VERSION_FILE_PATH})

if (GEN_SHARED_LIB)
    add_library(System.Security.Cryptography.Native.OpenSsl
        SHARED
        $<TARGET_OBJECTS:objlib>
    )
endif()

add_library(System.Security.Cryptography.Native.OpenSsl-Static
    STATIC
    $<TARGET_OBJECTS:objlib>
    entrypoints.c
)

set_target_properties(System.Security.Cryptography.Native.OpenSsl-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.OpenSsl CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    if (FEATURE_DISTRO_AGNOSTIC_SSL)
        # on macOS and Alpine the link step fails with undefined symbols, and the script doesn't run.
        # if the build succeeds, the script would succeed, except it uses a Linux-only command.
        #
        # on Linux, the build will succeed with undefined symbols, then the script reports them
        # and fails the build for us.
        if (NOT APPLE AND NOT CLR_CMAKE_TARGET_ALPINE_LINUX)
            add_custom_command(TARGET System.Security.Cryptography.Native.OpenSsl POST_BUILD
                COMMENT "Verifying System.Security.Cryptography.Native.OpenSsl.so dependencies"
                COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-so.sh
                    $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
                    "Verification failed. System.Security.Cryptography.Native.OpenSsl.so has undefined dependencies. These are likely OpenSSL APIs that need to be added to opensslshim.h"
                VERBATIM
            )
        endif()
    endif()

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID)
        add_custom_command(TARGET System.Security.Cryptography.Native.OpenSsl POST_BUILD
            COMMENT "Verifying System.Security.Cryptography.Native.OpenSsl entry points against entrypoints.c "
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-entrypoints.sh
                $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
                ${CMAKE_CURRENT_SOURCE_DIR}/entrypoints.c
                ${CMAKE_NM}
            VERBATIM
        )
    endif()

    target_link_libraries(System.Security.Cryptography.Native.OpenSsl
        PRIVATE
        ${NATIVE_LIBS_EXTRA}
    )
endif()

include(configure.cmake)

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.Security.Cryptography.Native.OpenSsl PROGRAMS .)
endif()

install (TARGETS System.Security.Cryptography.Native.OpenSsl-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
