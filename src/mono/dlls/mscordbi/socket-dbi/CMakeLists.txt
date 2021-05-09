project(socket-dbi)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(socket_sources
    socket.cpp
    socket.h
)

add_library(socket-dbi STATIC ${socket_sources})
