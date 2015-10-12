cmake_minimum_required(VERSION 2.8.12.2)

project(coreclrtraceptprovider)

set(CMAKE_INCLUDE_CURRENT_DIR ON)   

add_definitions(-DPAL_STDCPP_COMPAT=1)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)
include_directories(${COREPAL_SOURCE_DIR}/src/eventprovider)
add_compile_options(-fPIC)

add_library(coreclrtraceptprovider
    SHARED 
    "../lttng/traceptprovdotnetruntime.cpp"
    "../lttng/traceptprovdotnetruntimeprivate.cpp"
    "../lttng/traceptprovdotnetruntimerundown.cpp"
    "../lttng/traceptprovdotnetruntimestress.cpp"
    )

target_link_libraries(coreclrtraceptprovider
    -llttng-ust
    )


# Install the static PAL library for VS
install (TARGETS coreclrtraceptprovider DESTINATION .)

