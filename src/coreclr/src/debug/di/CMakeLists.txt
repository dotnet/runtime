add_definitions(-DDBG_TARGET_AMD64=1 -DDBG_TARGET_WIN64=1 -DDBG_TARGET_64BIT=1 -D_TARGET_AMD64_=1)
add_definitions(-DFEATURE_METADATA_CUSTOM_DATA_SOURCE -DFEATURE_METADATA_DEBUGGEE_DATA_SOURCE -DFEATURE_NO_HOST -DFEATURE_METADATA_LOAD_TRUSTED_IMAGES)

set(CORDBDI_SOURCES
  shimprocess.cpp
  shimcallback.cpp
  shimevents.cpp
  shimdatatarget.cpp
  shimstackwalk.cpp
  breakpoint.cpp
  cordb.cpp
  divalue.cpp
  dbgtransportmanager.cpp
  hash.cpp
  module.cpp
  nativepipeline.cpp
  platformspecific.cpp
  process.cpp
  rsappdomain.cpp
  rsassembly.cpp
  rsclass.cpp
  rsfunction.cpp
  rsmain.cpp
  rsmda.cpp
  rsregsetcommon.cpp
  rsstackwalk.cpp
  rsthread.cpp
  rstype.cpp
  shared.cpp
  symbolinfo.cpp
  valuehome.cpp
)

if(WIN32)
    #use static crt
    add_definitions(-MT) 

    set(CORDBDI_SOURCES
      ${CORDBDI_SOURCES}
      amd64/floatconversion.asm
    )
elseif(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    set(CORDBDI_SOURCES
      ${CORDBDI_SOURCES}
      amd64/floatconversion.S
    )

endif(WIN32)

add_precompiled_header(stdafx.h stdafx.cpp CORDBDI_SOURCES)

add_library(cordbdi STATIC ${CORDBDI_SOURCES})