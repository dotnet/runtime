project(unixcorerun)

include_directories(../unixcoreruncommon)

set(CORERUN_SOURCES 
    ../unixcoreruncommon/coreruncommon.cpp
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

# Libc turns locks into no-ops if pthread was not loaded into process yet. Loading
# pthread by the process executable ensures that all locks are initialized properly.
target_link_libraries(corerun
    pthread
)

add_dependencies(corerun
    coreclr
)

install (TARGETS corerun DESTINATION .)
