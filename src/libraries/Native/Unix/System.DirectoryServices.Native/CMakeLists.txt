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

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID)
        set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/System.DirectoryServices.Native_unixexports.src)
        set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/System.DirectoryServices.Native.exports)
        generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
        set_exports_linker_option(${EXPORTS_FILE})

        add_custom_target(System.DirectoryServices.Native_exports DEPENDS ${EXPORTS_FILE})
        add_dependencies(System.DirectoryServices.Native System.DirectoryServices.Native_exports)

        set_property(TARGET System.DirectoryServices.Native APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
        set_property(TARGET System.DirectoryServices.Native APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
    endif()

    install_with_stripped_symbols (System.DirectoryServices.Native PROGRAMS .)
endif ()

add_library(System.DirectoryServices.Native-Static
    STATIC
    ${NATIVEPROTOCOLS_SOURCES}
)

set_target_properties(System.DirectoryServices.Native-Static PROPERTIES OUTPUT_NAME System.DirectoryServices.Native  CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.DirectoryServices.Native-Static DESTINATION ${STATIC_LIB_DESTINATION})
