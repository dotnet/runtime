project(ildasmrc)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILDASM__)

add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFX_VFT=VFT_DLL)

set(ILDASM_RESOURCES
    ../dasm.rc
)

add_library_clr(ildasmrc
    SHARED
    ${ILDASM_RESOURCES}
)

target_link_libraries(ildasmrc
     msvcrt
)

# We will generate PDB only for the debug configuration
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/ildasmrc.pdb DESTINATION PDB)

install (TARGETS ildasmrc DESTINATION .)
