project(crossgen)

include(${CLR_DIR}/crossgen.cmake)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(../util)
include_directories(../../pal/prebuilt/corerror)

set(crossgen_SOURCES crossgen.cpp ../util/consoleargs.cpp)
if(WIN32)
    list(APPEND crossgen_SOURCES ../util/consoleargs.h)
    set(crossgen_RESOURCES Native.rc)
    add_definitions(-D_CRT_NON_CONFORMING_WCSTOK)
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIE)
  add_definitions(-DNO_NGENPDB)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_definitions(-DFX_VER_INTERNALNAME_STR=crossgen.exe)

add_executable_clr(crossgen
  ${crossgen_SOURCES}
  ${crossgen_RESOURCES}
)

if(FEATURE_MERGE_JIT_AND_ENGINE)
    set(CLRJIT_CROSSGEN clrjit_crossgen)
endif(FEATURE_MERGE_JIT_AND_ENGINE)

target_link_libraries(crossgen
    cee_crossgen
    mdcompiler_crossgen
    mdruntime_crossgen
    mdruntimerw_crossgen
    mdhotdata_crossgen
    corguids
    ${CLRJIT_CROSSGEN}
    gcinfo_crossgen
    corzap_crossgen
    mscorlib_crossgen
    strongname_crossgen
    utilcode_crossgen
    v3binder_crossgen
)

if(CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(crossgen
        mscorrc_debug
        coreclrpal
        palrt
    )
else()
    target_link_libraries(crossgen
        advapi32
        ole32
        oleaut32
        uuid
        user32
        version
        shlwapi
        bcrypt
        mdwinmd_crossgen
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )

endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(../../zap/crossgen ../../zap/crossgen)
add_subdirectory(../../vm/crossgen ../../vm/crossgen)
add_subdirectory(../../vm/crossgen_mscorlib ../../vm/crossgen_mscorlib)

# add the install targets
install_clr(crossgen)
