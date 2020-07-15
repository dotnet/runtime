project (hostpolicy)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES HostpolicyMock.cpp )
add_library(hostpolicy SHARED ${SOURCES})

install(TARGETS hostpolicy DESTINATION bin)
