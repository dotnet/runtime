project(unixcoreconsole)

include_directories(../unixcoreruncommon)

set(CORECONSOLE_SOURCES 
    ../unixcoreruncommon/coreruncommon.cpp
    coreconsole.cpp 
)

add_executable(coreconsole
    ${CORECONSOLE_SOURCES}
)

# FreeBSD implements dlopen in libc
if(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    target_link_libraries(coreconsole 
        dl
    )
endif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

# FreeBSD requires pthread to be loaded by the executable process
if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    target_link_libraries(coreconsole
        pthread
    )
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

add_dependencies(coreconsole
    coreclr
)

install (TARGETS coreconsole DESTINATION .)
