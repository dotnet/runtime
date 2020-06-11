project(superpmi)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DUSE_COREDISTOOLS)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
endif(CLR_CMAKE_HOST_WIN32)

include_directories(.)
include_directories(../superpmi-shared)

set(SUPERPMI_SOURCES
    commandline.cpp
    cycletimer.cpp
    icorjitinfo.cpp
    jitdebugger.cpp
    jitinstance.cpp
    methodstatsemitter.cpp
    neardiffer.cpp
    parallelsuperpmi.cpp
    superpmi.cpp
    jithost.cpp
    ../superpmi-shared/callutils.cpp
    ../superpmi-shared/compileresult.cpp
    ../superpmi-shared/errorhandling.cpp
    ../superpmi-shared/hash.cpp
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

_add_executable(superpmi
    ${SUPERPMI_SOURCES}
)

target_precompile_header(TARGET superpmi HEADER standardpch.h)

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi
        version.lib
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )

    _install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/superpmi.pdb DESTINATION PDB)
endif(CLR_CMAKE_HOST_UNIX)

_install (TARGETS superpmi DESTINATION .)
