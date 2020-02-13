project(unixcorerun)

include_directories(../unixcoreruncommon)

set(CORERUN_SOURCES
    corerun.cpp
)

_add_executable(corerun
    ${CORERUN_SOURCES}
)

# FreeBSD and NetBSD implement dlopen(3) in libc
if(NOT CLR_CMAKE_TARGET_FREEBSD AND NOT CLR_CMAKE_TARGET_NETBSD)
    target_link_libraries(corerun
        dl
    )
endif(NOT CLR_CMAKE_TARGET_FREEBSD AND NOT CLR_CMAKE_TARGET_NETBSD)

# Libc turns locks into no-ops if pthread was not loaded into process yet. Loading
# pthread by the process executable ensures that all locks are initialized properly.
target_link_libraries(corerun
    unixcoreruncommon
)

# Android implements pthread natively
if(NOT CLR_CMAKE_TARGET_ANDROID)
  target_link_libraries(corerun
    pthread
  )
endif()

install_clr(TARGETS corerun)
