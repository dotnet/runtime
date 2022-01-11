project(unitygc)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(../../src/coreclr/gc)
include_directories(../../src/coreclr/gc/env)
include_directories(../../src/coreclr/inc)
include_directories(../../src/native)

set(GC_SOURCES
  unitygc.cpp)

if(${CMAKE_SYSTEM_NAME} STREQUAL Windows)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib)
  add_definitions(-DTARGET_WINDOWS)
else()
  set (GC_LINK_LIBRARIES)
endif(${CMAKE_SYSTEM_NAME} STREQUAL Windows)

add_library(unitygc SHARED ${GC_SOURCES})
target_link_libraries(unitygc ${GC_LINK_LIBRARIES})
#install_clr(TARGETS unitygc DESTINATIONS . COMPONENT runtime)
