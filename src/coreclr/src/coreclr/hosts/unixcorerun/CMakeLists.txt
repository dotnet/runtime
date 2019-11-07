project(unixcorerun)

include_directories(../unixcoreruncommon)

set(CORERUN_SOURCES
    corerun.cpp
)

_add_executable(corerun
    ${CORERUN_SOURCES}
)

# FreeBSD and NetBSD implement dlopen(3) in libc
if(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    target_link_libraries(corerun
        dl
    )
endif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)

# Libc turns locks into no-ops if pthread was not loaded into process yet. Loading
# pthread by the process executable ensures that all locks are initialized properly.
target_link_libraries(corerun
    unixcoreruncommon
)

# Android implements pthread natively
if(NOT CLR_CMAKE_PLATFORM_ANDROID)
  target_link_libraries(corerun
    pthread
  )
endif()

install_clr(corerun)
