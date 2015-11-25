project(ildasm)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILDASM__)

add_definitions(-DFEATURE_CORECLR)

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
    coreclr
)

if(CLR_CMAKE_PLATFORM_UNIX)
    # TODO
    target_link_libraries(ildasm
        mscorrc_debug
        coreclrpal
        palrt
    )
else()
    target_link_libraries(ildasm
        ${ILDASM_LINK_LIBRARIES}
         msvcrt
         ole32
         oleaut32
         shell32
    )

    # We will generate PDB only for the debug configuration
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/ildasm.pdb DESTINATION PDB)

endif(CLR_CMAKE_PLATFORM_UNIX)

install (TARGETS ildasm DESTINATION .)
