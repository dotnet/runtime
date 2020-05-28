project(osxbundlerun)

include_directories(../unixcoreruncommon)

set(CORERUN_SOURCES
    osxbundlerun.cpp
)

_add_executable(osxbundlerun
    ${CORERUN_SOURCES}
)

target_link_libraries(osxbundlerun
    dl
    unixcoreruncommon
)

add_dependencies(osxbundlerun
    coreclr
)

install_clr(TARGETS osxbundlerun)
