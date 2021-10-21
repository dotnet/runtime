project(ildasm)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILDASM__)

add_definitions(-DFEATURE_CORECLR)

include_directories(..)

if(CLR_CMAKE_HOST_UNIX)
    build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../dasm.rc dasm_rc TARGET_CPP_FILE)

    set(ILDASM_RESOURCES
        ${TARGET_CPP_FILE}
    )
else()
    set(ILDASM_RESOURCES ../dasm.rc)
    add_definitions(-DFX_VER_INTERNALNAME_STR=ildasm.exe)
endif(CLR_CMAKE_HOST_UNIX)

set(ILDASM_SOURCES
    ../ceeload.cpp
    ../dasm.cpp
    ../dasm_formattype.cpp
    ../dasm_mi.cpp
    ../dasm_sz.cpp
    ../dis.cpp
    ../dman.cpp
    ../dres.cpp
    ../ildasmpch.cpp
    ../windasm.cpp
)

set(ILDASM_HEADERS
    ../ceeload.h
    ../dasm_sz.h
    ../dis.h
    ../dynamicarray.h
    ../ildasmpch.h
    ../resource.h
    ../util.hpp
)

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND ILDASM_SOURCES ${ILDASM_HEADERS})
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
    # This option is necessary to ensure that the overloaded delete operator defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")

    # The following linked options can be inserted into the linker libraries list to
    # ensure proper resolving of circular references between a subset of the libraries.
    set(START_LIBRARY_GROUP -Wl,--start-group)
    set(END_LIBRARY_GROUP -Wl,--end-group)
endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

add_executable_clr(ildasm
    ${ILDASM_SOURCES}
    ${ILDASM_RESOURCES}
)

set(ILDASM_LINK_LIBRARIES
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    utilcodestaticnohost
    mdcompiler_wks
    mdruntime_wks
    mdruntimerw_wks
    mdstaticapi
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
    corguids
)

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND ILDASM_LINK_LIBRARIES
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        bcrypt.lib
        RuntimeObject.lib
    )
else()
    list(APPEND ILDASM_LINK_LIBRARIES
        coreclrpal
        mscorrc
        palrt
    )
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(ildasm
        ${ILDASM_LINK_LIBRARIES}
        mscorrc
        coreclrpal
        palrt
        ${CMAKE_DL_LIBS}
    )
else()
    target_link_libraries(ildasm
        ${ILDASM_LINK_LIBRARIES}
        ole32
        oleaut32
        shell32
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS ildasm DESTINATIONS . COMPONENT iltools)
