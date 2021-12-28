project(System.Native C)

if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    add_definitions(-DHAS_CONSOLE_SIGNALS)
endif ()

if (CLR_CMAKE_HOST_ALPINE_LINUX)
    # Fix up the thread stack size for MUSL to more reasonable size.
    # TODO: https://github.com/dotnet/runtimelab/issues/791
    add_definitions(-DENSURE_PRIMARY_STACK_SIZE)
endif ()

if (CLR_CMAKE_TARGET_OSX)
    add_definitions(-D_DARWIN_C_SOURCE)
endif ()

set(NATIVE_SOURCES
    pal_dynamicload.c
    pal_errno.c
    pal_interfaceaddresses.c
    pal_io.c
    pal_maphardwaretype.c
    pal_memory.c
    pal_mount.c
    pal_networking.c
    pal_networkstatistics.c
    pal_process.c
    pal_random.c
    pal_runtimeinformation.c
    pal_signal.c
    pal_string.c
    pal_tcpstate.c
    pal_threading.c
    pal_time.c
    pal_uid.c
    pal_datetime.c
    pal_sysctl.c
)

if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    list (APPEND NATIVE_SOURCES pal_autoreleasepool.m)
    set_source_files_properties(pal_autoreleasepool.m PROPERTIES COMPILE_FLAGS -fno-objc-arc)
else()
    list (APPEND NATIVE_SOURCES pal_autoreleasepool.c)
endif()

if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    list (APPEND NATIVE_SOURCES pal_environment.m)
else()
    list (APPEND NATIVE_SOURCES pal_environment.c)
endif()

if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    set(NATIVE_SOURCES ${NATIVE_SOURCES}
        pal_log.m
        pal_searchpath.m)
else ()
    list (APPEND NATIVE_SOURCES
        pal_searchpath.c
        pal_console.c
        pal_log.c)
endif ()

if (CLR_CMAKE_TARGET_MACCATALYST)
    set(NATIVE_SOURCES ${NATIVE_SOURCES}
        pal_iossupportversion.m)
else ()
    list (APPEND NATIVE_SOURCES
        pal_iossupportversion.c)
endif ()

if (NOT CLR_CMAKE_TARGET_BROWSER)
    list (APPEND NATIVE_SOURCES pal_networkchange.c)
endif ()

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_system_libs(NATIVE_LIBS_EXTRA)

if (GEN_SHARED_LIB)
    add_library(System.Native
        SHARED
        ${NATIVE_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.Native
        ${NATIVE_LIBS_EXTRA}
    )

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER)
        add_custom_command(TARGET System.Native POST_BUILD
            COMMENT "Verifying System.Native entry points against entrypoints.c "
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-entrypoints.sh
                $<TARGET_FILE:System.Native>
                ${CMAKE_CURRENT_SOURCE_DIR}/entrypoints.c
                ${CMAKE_NM}
            VERBATIM
        )
    endif()

    install_with_stripped_symbols (System.Native PROGRAMS .)
endif ()

if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER)
    set(NATIVE_SOURCES ${NATIVE_SOURCES} entrypoints.c)
endif()

add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
)

set_target_properties(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
