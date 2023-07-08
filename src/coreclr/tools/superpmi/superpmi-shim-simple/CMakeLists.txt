project(superpmi-shim-simple)

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

set(SUPERPMI_SHIM_SIMPLE_SOURCES
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo_generated.cpp
    superpmi-shim-simple.cpp
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
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-simple.def ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-simple.def)

    list(APPEND SUPERPMI_SHIM_SIMPLE_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-simple.def)
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(superpmi-shim-simple
    SHARED
    ${SUPERPMI_SHIM_SIMPLE_SOURCES}
)

target_precompile_headers(superpmi-shim-simple PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi-shim-simple
        PRIVATE
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
        coreclrminipal
    )
else()
    target_link_libraries(superpmi-shim-simple
        PRIVATE
        advapi32.lib
        coreclrminipal
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS superpmi-shim-simple DESTINATIONS . COMPONENT spmi)
