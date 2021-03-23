project(mscorpe)

add_definitions(-DFEATURE_CORECLR)

set(MSCORPE_SOURCES
  iceefilegen.cpp
  ceefilegenwriter.cpp
  pewriter.cpp
  ceefilegenwritertokens.cpp
)

add_library_clr(mscorpe STATIC
  ${MSCORPE_SOURCES}
)
