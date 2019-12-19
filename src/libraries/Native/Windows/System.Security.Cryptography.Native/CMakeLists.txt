project(System.Security.Cryptography.Native C)

set(CMAKE_INCLUDE_CURRENT_DIR ON)


add_definitions(-DPIC=1 -DOPENSSL_API_COMPAT=0x10100000L -D_CRT_SECURE_NO_WARNINGS)
ADD_DEFINITIONS ( /MT )

if(CMAKE_STATIC_LIB_LINK)
   set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
endif(CMAKE_STATIC_LIB_LINK)

find_package(OpenSSL)
if(NOT OPENSSL_FOUND)
    message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (or the appropriate package for your platform) !!!")
endif(NOT OPENSSL_FOUND)

include_directories(${OPENSSL_INCLUDE_DIR})

set(NATIVECRYPTO_SOURCES
	../../Unix/System.Security.Cryptography.Native/apibridge.c
	../../Unix/System.Security.Cryptography.Native/openssl.c
	../../Unix/System.Security.Cryptography.Native/pal_asn1.c
	../../Unix/System.Security.Cryptography.Native/pal_bignum.c
	../../Unix/System.Security.Cryptography.Native/pal_bio.c
	../../Unix/System.Security.Cryptography.Native/pal_dsa.c
	../../Unix/System.Security.Cryptography.Native/pal_ecdsa.c
	../../Unix/System.Security.Cryptography.Native/pal_ecc_import_export.c
	../../Unix/System.Security.Cryptography.Native/pal_eckey.c
	../../Unix/System.Security.Cryptography.Native/pal_err.c
	../../Unix/System.Security.Cryptography.Native/pal_evp.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_pkey.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_pkey_dsa.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_pkey_ecdh.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_pkey_eckey.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_pkey_rsa.c
	../../Unix/System.Security.Cryptography.Native/pal_evp_cipher.c
	../../Unix/System.Security.Cryptography.Native/pal_hmac.c
	../../Unix/System.Security.Cryptography.Native/pal_ocsp.c
	../../Unix/System.Security.Cryptography.Native/pal_pkcs12.c
	../../Unix/System.Security.Cryptography.Native/pal_pkcs7.c
	../../Unix/System.Security.Cryptography.Native/pal_rsa.c
	../../Unix/System.Security.Cryptography.Native/pal_ssl.c
	../../Unix/System.Security.Cryptography.Native/pal_x509.c
	../../Unix/System.Security.Cryptography.Native/pal_x509_name.c
	../../Unix/System.Security.Cryptography.Native/pal_x509_root.c
	../../Unix/System.Security.Cryptography.Native/pal_x509ext.c
)

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    if (NOT CMAKE_SYSTEM_NAME STREQUAL Linux)
        message(FATAL_ERROR "FEATURE_DISTRO_AGNOSTIC_SSL can only be enabled for Linux")
    endif()

    list(APPEND NATIVECRYPTO_SOURCES
        opensslshim.c
    )
    add_definitions(-DFEATURE_DISTRO_AGNOSTIC_SSL)
endif()

add_library(objlib OBJECT ${NATIVECRYPTO_SOURCES} ${VERSION_FILE_PATH})
#target_link_libraries(objlib
#      msvcrt.lib
#)

add_library(security.cryptography.native.openssl
    SHARED
    $<TARGET_OBJECTS:objlib>
)

# Disable the "lib" prefix.
set_target_properties(security.cryptography.native.openssl PROPERTIES PREFIX "")

target_link_libraries(security.cryptography.native.openssl
    ${OPENSSL_CRYPTO_LIBRARY}
    ${OPENSSL_SSL_LIBRARY}
    msvcrt.lib
)
  

include(configure.cmake)

#set(__LinkLibraries "${__LinkLibraries} libcrt.lib")

function(install_library_and_symbols targetName)
    # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
    # generator expression doesn't work correctly returning the wrong path and on
    # the newer cmake versions the LOCATION property isn't supported anymore.
    if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
        set(install_source_file $<TARGET_FILE:${targetName}>)
    else()
        get_property(install_source_file TARGET ${targetName} PROPERTY LOCATION)
    endif()

    install(PROGRAMS ${install_source_file} DESTINATION .)
    install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)
endfunction()


install_library_and_symbols (security.cryptography.native.openssl)
#install (TARGETS System.Security.Cryptography.Native.OpenSsl-Static DESTINATION .)
