project (hostpolicy)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES HostpolicyMock.cpp )
add_library(hostpolicy ${TEST_LIB_TYPE} ${SOURCES})

install(TARGETS hostpolicy DESTINATION bin)
