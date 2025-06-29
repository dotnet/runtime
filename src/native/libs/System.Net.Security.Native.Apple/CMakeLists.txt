project(System.Net.Security.Native.Apple C)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_compile_options(-Wno-deprecated-declarations)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)
append_extra_networking_apple_libs(NATIVE_LIBS_EXTRA)

set(NATIVENETWORKING_SOURCES
    pal_networkframework.m
    entrypoints.c
)

if (GEN_SHARED_LIB)
    add_library(System.Net.Security.Native.Apple
        SHARED
        ${NATIVENETWORKING_SOURCES}
        ${VERSION_FILE_PATH}
    )
endif()

add_library(System.Net.Security.Native.Apple-Static
    STATIC
    ${NATIVENETWORKING_SOURCES}
)

set_target_properties(System.Net.Security.Native.Apple-Static PROPERTIES OUTPUT_NAME System.Net.Security.Native.Apple CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    target_link_libraries(System.Net.Security.Native.Apple
        PRIVATE
        ${NATIVE_LIBS_EXTRA}
    )

    add_custom_command(TARGET System.Net.Security.Native.Apple POST_BUILD
        COMMENT "Verifying System.Net.Security.Native.Apple entry points"
        COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-entrypoints.sh
            $<TARGET_FILE:System.Net.Security.Native.Apple>
            ${CMAKE_CURRENT_SOURCE_DIR}/entrypoints.c
            ${CMAKE_NM}
        VERBATIM
    )
endif()

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.Net.Security.Native.Apple PROGRAMS .)
endif()

install (TARGETS System.Net.Security.Native.Apple-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
