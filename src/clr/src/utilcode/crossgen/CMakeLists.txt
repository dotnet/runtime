include(${CLR_DIR}/crossgen.cmake)

list(APPEND UTILCODE_SOURCES 
  ../hostimpl.cpp
)

add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
add_library(utilcode_crossgen STATIC ${UTILCODE_SOURCES})
if(CLR_CMAKE_PLATFORM_UNIX)
  target_link_libraries(utilcode_crossgen  nativeresourcestring)
endif(CLR_CMAKE_PLATFORM_UNIX)
