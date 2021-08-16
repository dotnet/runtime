project(superpmi-shim-counter)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

include_directories(.)
include_directories(../superpmi-shared)

set(SUPERPMI_SHIM_COUNTER_SOURCES
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo.cpp
    methodcallsummarizer.cpp
    superpmi-shim-counter.cpp
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

if (CLR_CMAKE_TARGET_WIN32)
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-counter.def ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-counter.def)

    list(APPEND SUPERPMI_SHIM_COUNTER_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-counter.def)
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(superpmi-shim-counter
    SHARED
    ${SUPERPMI_SHIM_COUNTER_SOURCES}
)

target_precompile_headers(superpmi-shim-counter PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi-shim-counter
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi-shim-counter
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS superpmi-shim-counter DESTINATIONS .)
