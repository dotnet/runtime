project(System.IO.Ports.Native C)

set(PORTS_SOURCES
    pal_termios.c
    pal_serial.c
)

if (HAVE_TERMIOS2)
    list (APPEND PORTS_SOURCES pal_termios2.c)
endif ()

if (GEN_SHARED_LIB)
    add_library(System.IO.Ports.Native
        SHARED
        ${PORTS_SOURCES}
        ${VERSION_FILE_PATH}
    )

    if (CLR_CMAKE_TARGET_HAIKU)
        target_link_libraries(System.IO.Ports.Native
            PRIVATE
            -lnetwork
        )
    endif()
endif()

add_library(System.IO.Ports.Native-Static
    STATIC
    ${PORTS_SOURCES}
)

set_target_properties(System.IO.Ports.Native-Static PROPERTIES OUTPUT_NAME System.IO.Ports.Native  CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.IO.Ports.Native PROGRAMS .)
endif()

install (TARGETS System.IO.Ports.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
