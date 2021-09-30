# Output the toolchain information required to create a command line that builds with the right rootfs as XML

include(${CMAKE_CURRENT_LIST_DIR}/../common/cross/toolchain.cmake)

message("<toolchain-info>")
message("<target-triple>${TOOLCHAIN}</target-triple>")
message("<linker-args>${CMAKE_SHARED_LINKER_FLAGS_INIT}</linker-args>")
message("<compiler-args>${ADDED_COMPILE_OPTIONS}</compiler-args>")
message("</toolchain-info>")