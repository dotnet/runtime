project(superpmi-shim-collector)

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

set(SUPERPMI_SHIM_COLLECTOR_SOURCES
    coreclrcallbacks.cpp
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo.cpp
    ieememorymanager.cpp
    iexecutionengine.cpp
    superpmi-shim-collector.cpp
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
if (WIN32)
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-collector.def ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.def)

    list(APPEND SUPERPMI_SHIM_COLLECTOR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.def)
endif (WIN32)

_add_library(superpmi-shim-collector
    SHARED
    ${SUPERPMI_SHIM_COLLECTOR_SOURCES}
)

target_precompile_header(TARGET superpmi-shim-collector HEADER standardpch.h)

if(CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(superpmi-shim-collector
        utilcodestaticnohost
        mscorrc_debug
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi-shim-collector
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )

    _install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/superpmi-shim-collector.pdb DESTINATION PDB)
endif(CLR_CMAKE_PLATFORM_UNIX)

_install (TARGETS superpmi-shim-collector DESTINATION .)
