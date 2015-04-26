project(unixcorerun)

# Work around XCode 6.3 SDK issue where __debug is included by some
# system headers but not present by adding our own include directory
# with a dummy file called __debug.
if(APPLE)
    include_directories(inc)
endif(APPLE)

set(CORERUN_SOURCES 
    corerun.cpp 
)

add_executable(corerun
    ${CORERUN_SOURCES}
)

# FreeBSD implements dlopen in libc
if(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    target_link_libraries(corerun 
        dl
    )
endif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

# FreeBSD requires pthread to be loaded by the executable process
if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    target_link_libraries(corerun
        pthread
    )
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

add_dependencies(corerun
    coreclr
)

install (TARGETS corerun DESTINATION .)
