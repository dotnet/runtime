project(mscorpe)

add_definitions(-DFEATURE_CORECLR)

set(MSCORPE_SOURCES
  iceefilegen.cpp
  ceefilegenwriter.cpp
  pewriter.cpp
  ceefilegenwritertokens.cpp
)

if(NOT WIN32)
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-delete-non-virtual-dtor>)
endif()

add_library_clr(mscorpe STATIC
  ${MSCORPE_SOURCES}
)
