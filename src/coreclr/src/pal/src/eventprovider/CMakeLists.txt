cmake_minimum_required(VERSION 2.8.12.2)

project(eventprovider)

set(CMAKE_INCLUDE_CURRENT_DIR ON)   

add_definitions(-DPAL_STDCPP_COMPAT=1)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)

add_library(eventprovider
    STATIC
    "lttng/eventprovdotnetruntimestress.cpp"
    "lttng/eventprovdotnetruntime.cpp"
    "lttng/eventprovdotnetruntimeprivate.cpp"
    "lttng/eventprovdotnetruntimerundown.cpp"
    )
add_subdirectory(tracepointprovider)

# Install the static PAL library for VS
install (TARGETS eventprovider DESTINATION lib)

