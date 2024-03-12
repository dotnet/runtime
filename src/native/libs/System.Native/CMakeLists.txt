project(System.Native C)

if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_WASI)
    add_definitions(-DHAS_CONSOLE_SIGNALS)
endif ()

if (CLR_CMAKE_TARGET_OSX)
    add_definitions(-D_DARWIN_C_SOURCE)
endif ()

set (MINIPAL_SOURCES "")
set (SHARED_MINIPAL_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/minipal")

include(${CLR_SRC_NATIVE_DIR}/minipal/configure.cmake)
list(APPEND MINIPAL_SOURCES
    random.c
)

addprefix(MINIPAL_SOURCES ${SHARED_MINIPAL_SOURCE_PATH} "${MINIPAL_SOURCES}")

set(NATIVE_SOURCES
    pal_errno.c
    pal_interfaceaddresses.c
    pal_io.c
    pal_maphardwaretype.c
    pal_memory.c
    pal_networkstatistics.c
    pal_random.c
    pal_runtimeinformation.c
    pal_string.c
    pal_tcpstate.c
    pal_time.c
    pal_datetime.c
    pal_sysctl.c
)

list(APPEND NATIVE_SOURCES
  ${MINIPAL_SOURCES}
)

if (NOT CLR_CMAKE_TARGET_WASI)
    list (APPEND NATIVE_SOURCES
        pal_dynamicload.c
        pal_mount.c
        pal_networking.c
        pal_process.c
        pal_signal.c
        pal_threading.c
        pal_uid.c
    )
else()
    list (APPEND NATIVE_SOURCES
        pal_dynamicload_wasi.c
        pal_mount_wasi.c
        pal_networking_wasi.c
        pal_process_wasi.c
        pal_signal_wasi.c
        pal_threading_wasi.c
        pal_uid_wasi.c
    )
endif()

if (CLR_CMAKE_TARGET_APPLE)
    list (APPEND NATIVE_SOURCES_OBJC_NO_ARC
        pal_autoreleasepool.m
        pal_environment.m
        pal_searchpath.m
        pal_datetime.m)

    if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        list (APPEND NATIVE_SOURCES_OBJC_NO_ARC pal_log.m)

        if (CLR_CMAKE_TARGET_MACCATALYST)
            list (APPEND NATIVE_SOURCES_OBJC_NO_ARC pal_iossupportversion.m)
        else()
            list (APPEND NATIVE_SOURCES pal_iossupportversion.c)
        endif ()
    elseif (CLR_CMAKE_TARGET_OSX)
        list (APPEND NATIVE_SOURCES
            pal_console.c
            pal_log.c
            pal_iossupportversion.c)
    else()
        message(FATAL_ERROR "Unsupported Apple platform")
    endif ()

    set_source_files_properties(${NATIVE_SOURCES_OBJC_NO_ARC} PROPERTIES COMPILE_FLAGS "-fno-objc-arc ${CLR_CMAKE_COMMON_OBJC_FLAGS}")
    list (APPEND NATIVE_SOURCES ${NATIVE_SOURCES_OBJC_NO_ARC})
else()
    list (APPEND NATIVE_SOURCES
        pal_autoreleasepool.c
        pal_environment.c
        pal_searchpath.c
        pal_log.c
        pal_iossupportversion.c)

    if (CLR_CMAKE_TARGET_WASI)
        list (APPEND NATIVE_SOURCES pal_console_wasi.c)
    else()
        list (APPEND NATIVE_SOURCES pal_console.c)
    endif()
endif()

if (NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
    list (APPEND NATIVE_SOURCES pal_networkchange.c)
endif ()

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_system_libs(NATIVE_LIBS_EXTRA)

if (CLR_CMAKE_TARGET_ANDROID AND NOT HAVE_GETIFADDRS)
    add_definitions(-DANDROID_GETIFADDRS_WORKAROUND)
    add_compile_options(-Wno-gnu-zero-variadic-macro-arguments)

    list (APPEND NATIVE_LIBS_EXTRA -llog)
    list (APPEND NATIVE_SOURCES pal_ifaddrs.c)
endif ()

if (GEN_SHARED_LIB)
    add_library(System.Native
        SHARED
        ${NATIVE_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.Native
        PRIVATE
        ${NATIVE_LIBS_EXTRA}
    )

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
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

if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
    set(NATIVE_SOURCES ${NATIVE_SOURCES} entrypoints.c)
endif()

add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
)

set_target_properties(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
