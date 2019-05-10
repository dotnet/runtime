include(${CLR_DIR}/crossgen.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_CROSSGEN_SOURCES)
add_library_clr(utilcode_crossgen STATIC ${UTILCODE_CROSSGEN_SOURCES})
if(CLR_CMAKE_PLATFORM_UNIX)
  target_link_libraries(utilcode_crossgen  nativeresourcestring)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_dependencies(utilcode_crossgen ${UTILCODE_DEPENDENCIES})
