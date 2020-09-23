project(mcs)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
endif(CLR_CMAKE_HOST_WIN32)

include_directories(.)
include_directories(../superpmi-shared)

set(MCS_SOURCES
    commandline.cpp
    mcs.cpp
    removedup.cpp
    verbasmdump.cpp
    verbconcat.cpp
    verbdump.cpp
    verbdumpmap.cpp
    verbdumptoc.cpp
    verbfracture.cpp
    verbildump.cpp
    verbinteg.cpp
    verbmerge.cpp
    verbprintjiteeversion.cpp
    verbremovedup.cpp
    verbstat.cpp
    verbstrip.cpp
    verbtoc.cpp
    ../superpmi-shared/asmdumper.cpp
    ../superpmi-shared/callutils.cpp
    ../superpmi-shared/compileresult.cpp
    ../superpmi-shared/errorhandling.cpp
    ../superpmi-shared/hash.cpp
    ../superpmi-shared/logging.cpp
    ../superpmi-shared/mclist.cpp
    ../superpmi-shared/methodcontext.cpp
    ../superpmi-shared/methodcontextiterator.cpp
    ../superpmi-shared/methodcontextreader.cpp
    ../superpmi-shared/simpletimer.cpp
    ../superpmi-shared/spmiutil.cpp
    ../superpmi-shared/tocfile.cpp
    ../superpmi-shared/typeutils.cpp
    ../superpmi-shared/spmidumphelper.cpp
)

_add_executable(mcs
    ${MCS_SOURCES}
)
target_precompile_header(TARGET mcs HEADER standardpch.h)

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(mcs
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
    )
else()
    target_link_libraries(mcs
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )

    _install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mcs.pdb DESTINATION PDB)
endif(CLR_CMAKE_HOST_UNIX)

_install (TARGETS mcs DESTINATION .)
