set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(DATASTREAM_SOURCES
    data_stream.c
    data_stream.h
)

add_library(datastream OBJECT ${DATASTREAM_SOURCES})
