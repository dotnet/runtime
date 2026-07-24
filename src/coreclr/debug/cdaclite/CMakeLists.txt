set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(BEFORE ${VM_DIR})
include_directories(${CLR_DIR}/debug/datadescriptor-shared/inc)

add_subdirectory(json)
add_subdirectory(data)
add_subdirectory(contracts)

set(CDACLITE_SOURCES
    cdaclite.cpp
    datatarget.cpp
    enumerate.cpp
)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=cdaclite.dll)

    # Embed the FX version resource so the shared framework manifest validation
    # (sharedfx.targets) finds a FileVersion, matching mscordaccore/mscordbi.
    list(APPEND CDACLITE_SOURCES Native.rc)

    set(DEF_SOURCES cdaclite.src)
    set(CURRENT_BINARY_DIR_FOR_CONFIG ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR})

    # Preprocess exports definition file
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/${DEF_SOURCES} ${CURRENT_BINARY_DIR_FOR_CONFIG}/cdaclite.def)
    add_custom_target(cdaclite_def DEPENDS ${CURRENT_BINARY_DIR_FOR_CONFIG}/cdaclite.def)
else(CLR_CMAKE_HOST_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/cdaclite_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/cdaclite.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
    add_custom_target(cdaclite_exports DEPENDS ${EXPORTS_FILE})
    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

add_library_clr(cdaclite SHARED ${CDACLITE_SOURCES})

if(CLR_CMAKE_HOST_WIN32)
    add_dependencies(cdaclite cdaclite_def)
    set_property(TARGET cdaclite APPEND_STRING PROPERTY LINK_FLAGS " /DEF:\"${CURRENT_BINARY_DIR_FOR_CONFIG}/cdaclite.def\"")
else(CLR_CMAKE_HOST_WIN32)
    add_dependencies(cdaclite cdaclite_exports)
    set_property(TARGET cdaclite APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET cdaclite APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

set(CDACLITE_LIBRARIES
    cdacgc
    cdacdata
    corguids
    dbgutil
    coreclrminipal
)

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND CDACLITE_LIBRARIES
        kernel32.lib
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
    )
else(CLR_CMAKE_HOST_WIN32)
    list(APPEND CDACLITE_LIBRARIES
        coreclrpal
    )
endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(cdaclite PRIVATE ${CDACLITE_LIBRARIES})

install_clr(TARGETS cdaclite DESTINATIONS . sharedFramework COMPONENT debug)

# Standalone test harness (Windows-only): drives cdaclite against a live process.
if(CLR_CMAKE_HOST_WIN32)
    add_executable_clr(cdaclitetest cdaclitetest.cpp)
    target_link_libraries(cdaclitetest
        PRIVATE
        corguids
        kernel32.lib
        advapi32.lib
        dbghelp.lib
        ${STATIC_MT_CRT_LIB}
    )
endif(CLR_CMAKE_HOST_WIN32)
