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

target_link_libraries(corerun 
    dl
)

add_dependencies(corerun
    coreclr
)

install (TARGETS corerun DESTINATION .)
