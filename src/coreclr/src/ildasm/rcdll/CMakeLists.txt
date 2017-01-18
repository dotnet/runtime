project(ildasmrc)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILDASM__)

add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFX_VFT=VFT_DLL)

add_definitions(-DFX_VER_INTERNALNAME_STR=ildasmrc.dll)

set(ILDASM_RESOURCES
    ../dasm.rc
)

add_library_clr(ildasmrc
    SHARED
    ${ILDASM_RESOURCES}
)

target_link_libraries(ildasmrc
	${STATIC_MT_CRT_LIB}
	${STATIC_MT_VCRT_LIB}
)

install_clr (ildasmrc)
