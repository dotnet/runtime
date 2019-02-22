project(mscorpe)

add_definitions(-DFEATURE_CORECLR)

set(MSCORPE_SOURCES
  iceefilegen.cpp
  ceefilegenwriter.cpp
  pewriter.cpp
  ceefilegenwritertokens.cpp
)

if(WIN32)
else()
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wno-delete-non-virtual-dtor")
endif(WIN32)

add_library_clr(mscorpe STATIC
  ${MSCORPE_SOURCES}
)
