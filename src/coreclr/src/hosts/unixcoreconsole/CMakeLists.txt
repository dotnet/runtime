project(unixcoreconsole)

include_directories(../unixcoreruncommon)

set(CORECONSOLE_SOURCES
    coreconsole.cpp
)

_add_executable(coreconsole
    ${CORECONSOLE_SOURCES}
)

# FreeBSD and NetBSD implement dlopen(3) in libc
if(NOT CLR_CMAKE_TARGET_FREEBSD AND NOT CLR_CMAKE_TARGET_NETBSD)
    target_link_libraries(coreconsole
        dl
    )
endif(NOT CLR_CMAKE_TARGET_FREEBSD AND NOT CLR_CMAKE_TARGET_NETBSD)

# Libc turns locks into no-ops if pthread was not loaded into process yet. Loading
# pthread by the process executable ensures that all locks are initialized properly.
target_link_libraries(coreconsole
    unixcoreruncommon
)

if(NOT CLR_CMAKE_TARGET_ANDROID)
  target_link_libraries(coreconsole
    pthread
  )
endif()

install_clr(TARGETS coreconsole)
