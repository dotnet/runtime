
project(System.Security.Cryptography.Native.Apple)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC=1)

set(NATIVECRYPTO_SOURCES
    pal_random.cpp
)

add_library(System.Security.Cryptography.Native.Apple
    SHARED
    ${NATIVECRYPTO_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix.
set_target_properties(System.Security.Cryptography.Native.Apple PROPERTIES PREFIX "")

target_link_libraries(System.Security.Cryptography.Native.Apple
)

install_library_and_symbols (System.Security.Cryptography.Native.Apple)
