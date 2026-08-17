add_definitions(
    -DFEATURE_NO_HOST
    -DFEATURE_METADATA_LOAD_TRUSTED_IMAGES)

if(CLR_CMAKE_HOST_UNIX AND HAVE_PTHREAD_CONDATTR_SETCLOCK)
    add_compile_definitions(DBG_TRANSPORT_HAS_PTHREAD_CONDATTR_SETCLOCK=1)
endif()

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

    if (CLR_CMAKE_TARGET_ARCH_AMD64 OR ((CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_ARM)
        AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD))
        set(CORDBDI_SOURCES_ASM_FILE ${ARCH_SOURCES_DIR}/floatconversion.asm)
    endif()

    if ((CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64) AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
        convert_to_absolute_path(CORDBDI_SOURCES_ASM_FILE ${CORDBDI_SOURCES_ASM_FILE})
        preprocess_files(CORDBDI_SOURCES_ASM_FILE ${CORDBDI_SOURCES_ASM_FILE})
    endif()
elseif(CLR_CMAKE_HOST_UNIX)

    if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_LOONGARCH64 OR CLR_CMAKE_TARGET_ARCH_RISCV64)
      set(CORDBDI_SOURCES_ASM_FILE
        ${ARCH_SOURCES_DIR}/floatconversion.S
      )
    endif()

endif(CLR_CMAKE_HOST_WIN32)

if (CLR_CMAKE_TARGET_WIN32)
    convert_to_absolute_path(CORDBDI_HEADERS ${CORDBDI_HEADERS})
    list(APPEND CORDBDI_SOURCES ${CORDBDI_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

list(APPEND CORDBDI_SOURCES ${CORDBDI_SOURCES_ASM_FILE})

add_library_clr(cordbdi STATIC ${CORDBDI_SOURCES})
set_target_properties(cordbdi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(cordbdi PRIVATE stdafx.h)
add_dependencies(cordbdi eventing_headers)

add_executable_clr(coreclr_debug_session_event_tests
    EXCLUDE_FROM_ALL
    ../shared/dbgtransportsessionevent.cpp
    ../shared/dbgtransportsessioneventtest.cpp
)

target_link_libraries(coreclr_debug_session_event_tests PRIVATE coreclrminipal)

if(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(coreclr_debug_session_event_tests
        PRIVATE
        kernel32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
elseif(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(coreclr_debug_session_event_tests PRIVATE pthread)
endif()

if(CLR_CMAKE_HOST_UNIX)
    add_library_clr(cordbdi_universal STATIC ${CORDBDI_SOURCES})
    set_target_properties(cordbdi_universal PROPERTIES DBI_COMPONENT TRUE)
    target_compile_definitions(cordbdi_universal PRIVATE MSCORDBI_LINKS_PRIVATE_PAL)
    target_precompile_headers(cordbdi_universal PRIVATE stdafx.h)
    add_dependencies(cordbdi_universal eventing_headers)
endif(CLR_CMAKE_HOST_UNIX)
