project(osxbundlerun)

include_directories(../unixcoreruncommon)

add_compile_options(-fPIE)

set(CORERUN_SOURCES 
    ../unixcoreruncommon/coreruncommon.cpp
    osxbundlerun.cpp 
)

add_executable(osxbundlerun
    ${CORERUN_SOURCES}
)

target_link_libraries(osxbundlerun 
    dl
)

add_dependencies(osxbundlerun
    coreclr
)

install (TARGETS osxbundlerun DESTINATION .)
