cmake_minimum_required (VERSION 2.6)
project (platformdefines)
set(SOURCES platformdefines.cpp)

# add the executable
add_library(platformdefines STATIC ${SOURCES})

# add the install targets
install (TARGETS platformdefines DESTINATION bin)
