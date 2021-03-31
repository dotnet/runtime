project(System.DirectoryServices.Native)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if (GEN_SHARED_LIB)
    include (GenerateExportHeader)
endif()

set (NATIVEPROTOCOLS_SOURCES
    ../../AnyOS/System.DirectoryServices.Native/LdapProxyForVarArgs.c
)

if (GEN_SHARED_LIB)
    add_library(System.DirectoryServices.Native
        SHARED
        ${NATIVEPROTOCOLS_SOURCES}
        System.DirectoryServices.Native.def
        # This will add versioning to the library
        ${CMAKE_REPO_ROOT}/artifacts/obj/NativeVersion.rc
    )

endif()

list(APPEND __LinkLibraries Wldap32.dll)

add_library(System.DirectoryServices.Native-Static
    STATIC
    ${NATIVEPROTOCOLS_SOURCES}
)

# Allow specification of arguments that should be passed to the linker
if (GEN_SHARED_LIB)
    SET_TARGET_PROPERTIES(System.DirectoryServices.Native PROPERTIES LINK_OPTIONS "${__LinkArgs};${__SharedLinkArgs}")
endif()
SET_TARGET_PROPERTIES(System.DirectoryServices.Native-Static PROPERTIES STATIC_LIBRARY_OPTIONS "${__LinkArgs}")

# Allow specification of libraries that should be linked against
if (GEN_SHARED_LIB)
    target_link_libraries(System.DirectoryServices.Native ${__LinkLibraries})
endif()
target_link_libraries(System.DirectoryServices.Native-Static ${__LinkLibraries})

if (GEN_SHARED_LIB)
    GENERATE_EXPORT_HEADER( System.DirectoryServices.Native
         BASE_NAME System.DirectoryServices.Native
         EXPORT_MACRO_NAME System.DirectoryServices.Native_EXPORT
         EXPORT_FILE_NAME System.DirectoryServices.Native_Export.h
         STATIC_DEFINE System.DirectoryServices.Native_BUILT_AS_STATIC
    )

    install (TARGETS System.DirectoryServices.Native DESTINATION .)
    install (FILES $<TARGET_PDB_FILE:System.DirectoryServices.Native> DESTINATION .)
endif()

install (TARGETS System.DirectoryServices.Native-Static DESTINATION ${STATIC_LIB_DESTINATION})
