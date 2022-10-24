# Configure the compiler
include(../configure.cmake)

include_directories(BEFORE ./inc)

add_subdirectory(dnmd/)
add_subdirectory(mddump/)
