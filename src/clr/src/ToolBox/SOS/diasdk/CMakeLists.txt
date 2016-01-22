# replace backslashes in path with forward slash
STRING(REGEX REPLACE "\\\\" "/" VSINSTALLDIR $ENV{VSINSTALLDIR} )

# Generate managed type lib from dia2.idl
add_custom_target(dialib ALL
    # generate tlb file from idl
    COMMAND midl.exe /I"${VSINSTALLDIR}DIA SDK/include" /tlb ${CMAKE_CURRENT_BINARY_DIR}/dia2.tlb  /Zp8  "${VSINSTALLDIR}DIA SDK/idl/dia2.idl"
    # run tlbimp to generate managed type library
    COMMAND TlbImp.exe /silent /namespace:Dia /unsafe /strictref:nopia ${CMAKE_CURRENT_BINARY_DIR}/dia2.tlb /out:${CMAKE_CURRENT_BINARY_DIR}/dialib.dll
    COMMENT Generating managed type library from dia2.idl
)

# In order to use dialib.dll as library target it needs to be imported into cmake
# Target is used in ToolBox/SOS/dactablegen/cmakelists.txt
add_library_clr(dialib_dll SHARED IMPORTED GLOBAL)
set_property(TARGET dialib_dll PROPERTY IMPORTED_LOCATION ${CMAKE_CURRENT_BINARY_DIR}/dialib.dll)
