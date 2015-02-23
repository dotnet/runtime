project(sosplugin)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if((NOT $ENV{LLDB_INCLUDE_DIR} STREQUAL "") AND (NOT $ENV{LLDB_LIB_DIR} STREQUAL ""))
    # The OSx build depends on the environment variables LLDB_INCLUDE_DIR and LLDB_LIB_DIR being set
    set(LLDB_INCLUDE_DIR "$ENV{LLDB_INCLUDE_DIR}")
    set(LLDB_LIB_DIR "$ENV{LLDB_LIB_DIR}")
elseif(CLR_CMAKE_PLATFORM_LINUX)
    # The Linux build depends on the lldb-3.5-dev package
    # TODO 2/23/15 - temporary until we get the lldb-3.5-dev package installed on the CI machine.
    #set(LLVM_DIR "/usr/lib/llvm-3.5")
    #set(LLDB_INCLUDE_DIR "${LLVM_DIR}/include")
    #set(LLDB_LIB_DIR "${LLVM_DIR}/lib")
endif()

if((NOT ${LLDB_INCLUDE_DIR} STREQUAL "") AND (NOT ${LLDB_LIB_DIR} STREQUAL ""))
    message(LLDB_INCLUDE_DIR=${LLDB_INCLUDE_DIR})
    message(LLDB_LIB_DIR=${LLDB_LIB_DIR})

    add_compile_options(-Wno-delete-non-virtual-dtor)

    include_directories(inc)
    include_directories("${LLDB_INCLUDE_DIR}")

    set(SOURCES
        sosplugin.cpp
        soscommand.cpp
        debugclient.cpp
    )

    add_library(sosplugin SHARED ${SOURCES})
    add_dependencies(sosplugin sos)

    if(CLR_CMAKE_PLATFORM_LINUX)
        link_directories("${LLDB_LIB_DIR}")
    endif(CLR_CMAKE_PLATFORM_LINUX)

    if(CLR_CMAKE_PLATFORM_DARWIN)
        find_library(LLDB LLDB PATHS ${LLDB_LIB_DIR})
        target_link_libraries(sosplugin ${LLDB})
    endif(CLR_CMAKE_PLATFORM_DARWIN)

    # add the install targets
    install (TARGETS sosplugin DESTINATION .)
endif()
