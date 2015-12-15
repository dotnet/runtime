project(unixcoreruncommon)

add_library(unixcoreruncommon
    STATIC
    coreruncommon.cpp
)

target_link_libraries(unixcoreruncommon dl)
