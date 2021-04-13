project(System.DirectoryServices.Native C)

set (NATIVEPROTOCOLS_SOURCES
    ../../AnyOS/System.DirectoryServices.Native/LdapProxyForVarArgs.c
)

if(CLR_CMAKE_TARGET_OSX)
    # Workaround for "'ber_scanf' is deprecated: first deprecated in macOS 10.11 - use OpenDirectory Framework".
    add_compile_options(-Wno-deprecated-declarations)
endif()

if (GEN_SHARED_LIB)

    add_library(System.DirectoryServices.Native
        SHARED
        ${NATIVEPROTOCOLS_SOURCES}
        ${VERSION_FILE_PATH}
    )

    find_library(LIBLDAP NAMES ldap libldap.dylib libldap-2.4.so.2)
    if(LIBLDAP STREQUAL LIBLDAP-NOTFOUND)
        message(FATAL_ERROR "Cannot find OpenLdap")
    else()
        message(STATUS "OpenLdap found as ${LIBLDAP}")
    endif()

    target_link_libraries(System.DirectoryServices.Native ${LIBLDAP})

    install_with_stripped_symbols (System.DirectoryServices.Native PROGRAMS .)
endif ()

add_library(System.DirectoryServices.Native-Static
    STATIC
    ${NATIVEPROTOCOLS_SOURCES}
)

install (TARGETS System.IO.Ports.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
