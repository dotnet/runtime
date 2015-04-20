include(${CLR_DIR}/crossgen.cmake)

list(APPEND UTILCODE_SOURCES 
  ../hostimpl.cpp
)

add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
add_library(utilcode_crossgen STATIC ${UTILCODE_SOURCES})
