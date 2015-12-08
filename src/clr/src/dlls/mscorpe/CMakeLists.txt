project(mscorpe)

add_definitions(-DFEATURE_CORECLR)

set(MSCORPE_SOURCES
  ICeeFileGen.cpp
  CeeFileGenWriter.cpp
  PEWriter.cpp
  CeeFileGenWriterTokens.cpp
  utilcodeinit.cpp
)

if(WIN32)
  list(APPEND MSCORPE_SOURCES
    Native.rc
  )
endif(WIN32)

add_library(mscorpe STATIC
  ${MSCORPE_SOURCES}
)
