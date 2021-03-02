include(CheckIncludeFiles)
include(CMakePushCheckState)

check_include_files(
    GSS/GSS.h
    HAVE_GSSFW_HEADERS)

option(HeimdalGssApi "use heimdal implementation of GssApi" OFF)
if (HeimdalGssApi)
   check_include_files(
       gssapi/gssapi.h
       HAVE_HEIMDAL_HEADERS)
endif()

if (CLR_CMAKE_TARGET_LINUX)
    cmake_push_check_state(RESET)
    set (CMAKE_REQUIRED_DEFINITIONS "-D_GNU_SOURCE")
    set (CMAKE_REQUIRED_LIBRARIES "-lanl")

    check_symbol_exists(
        getaddrinfo_a
        netdb.h
        HAVE_GETADDRINFO_A)

    cmake_pop_check_state()
endif ()
