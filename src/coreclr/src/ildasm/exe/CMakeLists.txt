project(ildasm)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILDASM__)

add_definitions(-DFEATURE_CORECLR)

include_directories(..)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIE)
    include_directories(../unixcoreclrloader)
    build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../dasm.rc dasm_rc TARGET_CPP_FILE)

    set(ILDASM_RESOURCES
        ${TARGET_CPP_FILE}
    )
else()
    set(ILDASM_RESOURCES ../dasm.rc)
    add_definitions(-DFX_VER_INTERNALNAME_STR=ildasm.exe)
endif(CLR_CMAKE_PLATFORM_UNIX)

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

if (WIN32)
    list(APPEND ILDASM_SOURCES ${ILDASM_HEADERS})
endif(WIN32)

_add_executable(ildasm
    ${ILDASM_SOURCES}
    ${ILDASM_RESOURCES}
)

set(ILDASM_LINK_LIBRARIES
    utilcodestaticnohost
    mdhotdata_full
    corguids
)

if(CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(ildasm
        ${ILDASM_LINK_LIBRARIES}
        unixcoreclrloader
        mscorrc_debug
        coreclrpal
        palrt
    )

    # FreeBSD and NetBSD implement dlopen(3) in libc
    if(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
        target_link_libraries(ildasm
            dl
        )
    endif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
else()
    target_link_libraries(ildasm
        ${ILDASM_LINK_LIBRARIES}
        coreclr
        ole32
        oleaut32
        shell32
    )
endif(CLR_CMAKE_PLATFORM_UNIX)

install_clr(ildasm)
