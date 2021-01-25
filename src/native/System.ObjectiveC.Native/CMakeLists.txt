set(CMAKE_OBJC_STANDARD 11)
set(CMAKE_OBJC_STANDARD_REQUIRED TRUE)

enable_language(OBJC)

set (SOURCES
    placeholder.c
)

add_library(System.ObjectiveC.Native STATIC ${SOURCES})