project(superpmi-shim-counter)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

if(WIN32)
  #use static crt
  add_definitions(-MT)
endif(WIN32)

include_directories(.)
include_directories(../superpmi-shared)

set(SUPERPMI_SHIM_COUNTER_SOURCES
    coreclrcallbacks.cpp
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo.cpp
    ieememorymanager.cpp
    iexecutionengine.cpp
    methodcallsummarizer.cpp
    superpmi-shim-counter.cpp
    ../superpmi-shared/callutils.cpp
    ../superpmi-shared/compileresult.cpp
    ../superpmi-shared/errorhandling.cpp
    ../superpmi-shared/logging.cpp
    ../superpmi-shared/mclist.cpp
    ../superpmi-shared/methodcontext.cpp
    ../superpmi-shared/methodcontextreader.cpp
    ../superpmi-shared/simpletimer.cpp
    ../superpmi-shared/spmiutil.cpp
    ../superpmi-shared/tocfile.cpp
    ../superpmi-shared/typeutils.cpp
    ../superpmi-shared/spmidumphelper.cpp
)

add_precompiled_header(
    standardpch.h
    ../superpmi-shared/standardpch.cpp
    SUPERPMI_SHIM_COUNTER_SOURCES
)

if (WIN32)
    preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-counter.def ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-counter.def)

    list(APPEND SUPERPMI_SHIM_COUNTER_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-counter.def)
endif (WIN32)

_add_library(superpmi-shim-counter
    SHARED
    ${SUPERPMI_SHIM_COUNTER_SOURCES}
)

if(CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(superpmi-shim-counter
        utilcodestaticnohost
        mscorrc_debug
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi-shim-counter
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )

    _install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/superpmi-shim-counter.pdb DESTINATION PDB)
endif(CLR_CMAKE_PLATFORM_UNIX)

_install (TARGETS superpmi-shim-counter DESTINATION .)
