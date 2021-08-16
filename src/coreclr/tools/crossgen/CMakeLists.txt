project(crossgen)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(../util)
include_directories(../../pal/prebuilt/corerror)

set(crossgen_SOURCES crossgen.cpp ../util/consoleargs.cpp)
if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND crossgen_SOURCES ../util/consoleargs.h)
    set(crossgen_RESOURCES Native.rc)
endif()

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-D_CRT_NON_CONFORMING_WCSTOK)
endif()

if(CLR_CMAKE_HOST_UNIX)
  add_definitions(-DNO_NGENPDB)
endif(CLR_CMAKE_HOST_UNIX)

add_definitions(-DFX_VER_INTERNALNAME_STR=crossgen.exe)

add_executable_clr(crossgen
  ${crossgen_SOURCES}
  ${crossgen_RESOURCES}
)
set_target_properties(crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)

if(FEATURE_MERGE_JIT_AND_ENGINE)
    set(CLRJIT_CROSSGEN clrjit_crossgen)
endif(FEATURE_MERGE_JIT_AND_ENGINE)

if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
    # The following linked options can be inserted into the linker libraries list to
    # ensure proper resolving of circular references between a subset of the libraries.
    set(START_LIBRARY_GROUP -Wl,--start-group)
    set(END_LIBRARY_GROUP -Wl,--end-group)
endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

target_link_libraries(crossgen
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cee_crossgen
    v3binder_crossgen
    mdcompiler_crossgen
    mdruntime_crossgen
    mdruntimerw_crossgen
    mdhotdata_crossgen
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
    corguids
    ${CLRJIT_CROSSGEN}
    gcinfo_crossgen
    corzap_crossgen
    corelib_crossgen
    utilcode_crossgen
)

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(crossgen
        mscorrc
        palrt
        coreclrpal
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
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )

endif(CLR_CMAKE_HOST_UNIX)

add_subdirectory(../../zap ../../zap)
add_subdirectory(../../vm/crossgen ../../vm/crossgen)

# add the install targets
install_clr(TARGETS crossgen DESTINATIONS . sharedFramework COMPONENT runtime)
