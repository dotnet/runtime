project(sosplugin)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(LLDB_SRC_DIR $ENV{LLDB_SRC_DIR})
set(LLVM_BUILD_DIR $ENV{LLVM_BUILD_DIR})

message(LLDB_SRC_DIR='$ENV{LLDB_SRC_DIR}')
message(LLVM_BUILD_DIR='$ENV{LLVM_BUILD_DIR}')

if((NOT ${LLDB_SRC_DIR} STREQUAL "") AND (NOT ${LLVM_BUILD_DIR} STREQUAL ""))
    add_compile_options(-Wno-delete-non-virtual-dtor)

    set(LLDB_INCLUDE_DIR "${LLDB_SRC_DIR}/include")

    include_directories(inc)
    include_directories("${LLDB_INCLUDE_DIR}")

    link_directories("${LLVM_BUILD_DIR}/lib")

    set(SOURCES
        sosplugin.cpp
        soscommand.cpp
        debugclient.cpp
    )

    add_library(sosplugin SHARED ${SOURCES})
    add_dependencies(sosplugin sos)
    target_link_libraries(sosplugin lldb)

    # add the install targets
    install (TARGETS sosplugin DESTINATION .)
endif()