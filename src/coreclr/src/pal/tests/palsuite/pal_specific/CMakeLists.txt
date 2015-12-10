cmake_minimum_required(VERSION 2.8.12.2)

# TODO: make these tests compile
# add_subdirectory(PAL_get_stderr)
# add_subdirectory(PAL_get_stdin)
# add_subdirectory(PAL_get_stdout)

add_subdirectory(pal_entrypoint)
add_subdirectory(PAL_errno)
add_subdirectory(PAL_GetPALDirectoryW)
add_subdirectory(pal_initializedebug)
add_subdirectory(PAL_Initialize_Terminate)

