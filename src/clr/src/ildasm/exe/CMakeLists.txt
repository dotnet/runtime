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
endif(CLR_CMAKE_PLATFORM_UNIX)

set(ILDASM_SOURCES
    ../ceeload.cpp
    ../dasm.cpp
    ../dasm_formattype.cpp
    ../dasm_mi.cpp
    ../dasm_pr.cpp
    ../dasm_sz.cpp
    ../dis.cpp
    ../dman.cpp
    ../dres.cpp
    ../gui.cpp
    ../ildasmpch.cpp
    ../windasm.cpp
)

add_executable(ildasm
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
        msvcrt
        ole32
        oleaut32
        shell32
    )

    # We will generate PDB only for the debug configuration
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/ildasm.pdb DESTINATION PDB)

endif(CLR_CMAKE_PLATFORM_UNIX)

install (TARGETS ildasm DESTINATION .)
