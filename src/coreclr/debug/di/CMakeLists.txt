add_definitions(
    -DFEATURE_NO_HOST
    -DFEATURE_METADATA_LOAD_TRUSTED_IMAGES)

set(CORDBDI_SOURCES
  shimprocess.cpp
  shimcallback.cpp
  shimevents.cpp
  shimdatatarget.cpp
  shimstackwalk.cpp
  breakpoint.cpp
  cordb.cpp
  cordbregisterset.cpp
  divalue.cpp
  dbgtransportmanager.cpp
  hash.cpp
  module.cpp
  platformspecific.cpp
  process.cpp
  rsappdomain.cpp
  rsassembly.cpp
  rsclass.cpp
  rsfunction.cpp
  rsmain.cpp
  rsregsetcommon.cpp
  rsstackwalk.cpp
  rsthread.cpp
  rstype.cpp
  shared.cpp
  symbolinfo.cpp
  valuehome.cpp
)

set(CORDBDI_HEADERS
    shimpriv.h
    shimdatatarget.h
    classfactory.h
    dbgtransportmanager.h
    eventchannel.h
    helpers.h
    nativepipeline.h
    rsenumerator.hpp
    rspriv.h
    rspriv.inl
    symbolinfo.h)


if(CLR_CMAKE_HOST_WIN32)
    #use static crt
    set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

if (CLR_CMAKE_TARGET_WIN32)
    convert_to_absolute_path(CORDBDI_HEADERS ${CORDBDI_HEADERS})
    list(APPEND CORDBDI_SOURCES ${CORDBDI_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(cordbdi STATIC ${CORDBDI_SOURCES})
set_target_properties(cordbdi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(cordbdi PRIVATE stdafx.h)
add_dependencies(cordbdi eventing_headers)

if(CLR_CMAKE_HOST_UNIX)
    add_library_clr(cordbdi_universal STATIC ${CORDBDI_SOURCES})
    set_target_properties(cordbdi_universal PROPERTIES DBI_COMPONENT TRUE)
    target_compile_definitions(cordbdi_universal PRIVATE MSCORDBI_LINKS_PRIVATE_PAL)
    target_precompile_headers(cordbdi_universal PRIVATE stdafx.h)
    add_dependencies(cordbdi_universal eventing_headers)
endif(CLR_CMAKE_HOST_UNIX)
