project(mscorpe)

add_definitions(-DFEATURE_CORECLR)

set(MSCORPE_SOURCES
  iceefilegen.cpp
  ceefilegenwriter.cpp
  pewriter.cpp
  ceefilegenwritertokens.cpp
  utilcodeinit.cpp
)

if(WIN32)
  list(APPEND MSCORPE_SOURCES
    Native.rc
  )
else()
  add_compile_options(-Wno-delete-non-virtual-dtor)
endif(WIN32)

add_library_clr(mscorpe STATIC
  ${MSCORPE_SOURCES}
)
