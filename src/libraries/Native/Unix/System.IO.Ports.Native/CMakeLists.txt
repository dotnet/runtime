project(System.IO.Ports.Native C)

if (GEN_SHARED_LIB)
    add_library(System.IO.Ports.Native
        SHARED
        pal_termios.c
        pal_serial.c
        ${VERSION_FILE_PATH}
    )
endif()

add_library(System.IO.Ports.Native-Static
    STATIC
    pal_termios.c
    pal_serial.c
)

set_target_properties(System.IO.Ports.Native-Static PROPERTIES OUTPUT_NAME System.IO.Ports.Native  CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.IO.Ports.Native PROGRAMS .)
endif()

install (TARGETS System.IO.Ports.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
