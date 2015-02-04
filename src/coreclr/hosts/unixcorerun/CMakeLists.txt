project(unixcorerun)

set(CORERUN_SOURCES 
    corerun.cpp 
)

add_executable(corerun
    ${CORERUN_SOURCES}
)

target_link_libraries(corerun 
    dl
)

add_dependencies(corerun
    coreclr
)

install (TARGETS corerun DESTINATION .)
