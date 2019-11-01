project(System.IO.Ports.Native C)

add_library(System.IO.Ports.Native
    SHARED
    pal_termios.c
    pal_serial.c
    ${VERSION_FILE_PATH}
)

add_library(System.IO.Ports.Native-Static
    STATIC
    pal_termios.c
    pal_serial.c
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.IO.Ports.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.IO.Ports.Native-Static PROPERTIES OUTPUT_NAME System.IO.Ports.Native  CLEAN_DIRECT_OUTPUT 1)

install_library_and_symbols (System.IO.Ports.Native)
install (TARGETS System.IO.Ports.Native-Static DESTINATION .)
