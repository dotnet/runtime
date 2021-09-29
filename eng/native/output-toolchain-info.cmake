# Output the toolchain information required to create a command line that builds with the right rootfs as XML

include(${CMAKE_CURRENT_LIST_DIR}/../common/cross/toolchain.cmake)

message("<toolchain-info>")
message("<target-triple>${TOOLCHAIN}</target-triple>")
message("<linker-args>${CMAKE_SHARED_LINKER_FLAGS_INIT}</linker-args>")
get_directory_property(COMPILER_ARGS COMPILE_OPTIONS)
message("<compiler-args>${COMPILER_ARGS}</compiler-args>")
message("</toolchain-info>")