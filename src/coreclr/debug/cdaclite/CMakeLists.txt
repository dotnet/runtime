set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(BEFORE ${VM_DIR})
include_directories(${CLR_DIR}/debug/datadescriptor-shared/inc)

add_subdirectory(json)
add_subdirectory(data)
add_subdirectory(contracts)

set(CDACLITE_SOURCES
    datatarget.cpp
    enumerate.cpp
)

add_library_clr(cdaclite STATIC ${CDACLITE_SOURCES})
target_include_directories(cdaclite PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

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

if(MSVC)
    # cDAC-lite is a cold path linked into createdump, so favor binary size over execution speed.
    foreach(CDACLITE_TARGET cdaclite cdacgc cdacdata cdacjson)
        target_compile_options(${CDACLITE_TARGET} PRIVATE /O1 /Ob1)
    endforeach()
endif(MSVC)
