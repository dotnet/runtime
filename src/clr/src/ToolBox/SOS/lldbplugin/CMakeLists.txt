project(sosplugin)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(LLVM_DIR "/usr/lib/llvm-3.5")
set(LLDB_INCLUDE_DIR "${LLVM_DIR}/include")
set(LLDB_LIB_DIR "${LLVM_DIR}/lib")

add_compile_options(-Wno-delete-non-virtual-dtor)

include_directories(inc)
include_directories("${LLDB_INCLUDE_DIR}")
link_directories("${LLDB_LIB_DIR}")

set(SOURCES
    sosplugin.cpp
    soscommand.cpp
    debugclient.cpp
)

add_library(sosplugin SHARED ${SOURCES})
add_dependencies(sosplugin sos)

# add the install targets
install (TARGETS sosplugin DESTINATION .)