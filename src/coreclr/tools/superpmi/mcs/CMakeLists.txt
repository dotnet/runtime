project(mcs)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
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
    verbjitflags.cpp
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

add_executable_clr(mcs
    ${MCS_SOURCES}
)
target_precompile_headers(mcs PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

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

endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS mcs DESTINATIONS . COMPONENT spmi)
