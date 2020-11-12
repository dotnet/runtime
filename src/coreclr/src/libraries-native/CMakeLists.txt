set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Suppress exporting of the PAL APIs
add_definitions(-DPALEXPORT=EXTERN_C)

set(STATIC_LIBS_ONLY 1)

if(CLR_CMAKE_TARGET_WIN32)
    set(GLOBALIZATION_NATIVE_DIR ${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Globalization.Native)
    set(COMPRESSION_NATIVE_DIR ${CLR_REPO_ROOT_DIR}/src/libraries/Native/Windows/clrcompression)

    include_directories("${GLOBALIZATION_NATIVE_DIR}")
    include_directories("${COMPRESSION_NATIVE_DIR}")

    include_directories("${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/Common")

    add_subdirectory(${COMPRESSION_NATIVE_DIR} clrcompression)
    add_subdirectory(${GLOBALIZATION_NATIVE_DIR} System.Globalization.Native)

else()
    add_subdirectory(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix Native.Unix)
endif()
