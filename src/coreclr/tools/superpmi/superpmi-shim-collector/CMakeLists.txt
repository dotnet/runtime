project(superpmi-shim-collector)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DFEATURE_ENABLE_HARDWARE_EXCEPTIONS)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

include_directories(.)
include_directories(../superpmi-shared)

set(SUPERPMI_SHIM_COLLECTOR_SOURCES
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo.cpp
    superpmi-shim-collector.cpp
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
    set(SPMI_COLLECTOR_EXPORTS ${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-collector.def)
    set(SPMI_COLLECTOR_EXPORTS_FINAL_FILE ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.def)
    preprocess_file(${SPMI_COLLECTOR_EXPORTS} ${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})

    list(APPEND SUPERPMI_SHIM_COLLECTOR_SOURCES ${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})
else()
    set(SPMI_COLLECTOR_EXPORTS ${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-collector.PAL.exports)
    set(SPMI_COLLECTOR_EXPORTS_FINAL_FILE ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.PAL.exports)

    generate_exports_file(${SPMI_COLLECTOR_EXPORTS} ${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})

    if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
        # This is required to force using our own PAL, not one that we are loaded with.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")
    endif()

    set_exports_linker_option(${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})
    set(SPMI_COLLECTOR_EXPORTS_LINKER_OPTION ${EXPORTS_LINKER_OPTION})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(superpmi-shim-collector
    SHARED
    ${SUPERPMI_SHIM_COLLECTOR_SOURCES}
)

target_precompile_headers(superpmi-shim-collector PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

add_custom_target(spmi_exports DEPENDS ${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})
add_dependencies(superpmi-shim-collector spmi_exports)
set_property(TARGET superpmi-shim-collector APPEND_STRING PROPERTY LINK_FLAGS ${SPMI_COLLECTOR_EXPORTS_LINKER_OPTION})
set_property(TARGET superpmi-shim-collector APPEND_STRING PROPERTY LINK_DEPENDS ${SPMI_COLLECTOR_EXPORTS_FINAL_FILE})

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi-shim-collector
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi-shim-collector
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS superpmi-shim-collector DESTINATIONS . COMPONENT spmi)
