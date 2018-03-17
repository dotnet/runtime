set(DACTABLEGEN_SOURCES
    cvconst.cs
    diautil.cs
    main.cs
    MapSymbolProvider.cs
)

# Cmake does not support csharp sources so add custom command
add_custom_target(dactablegen ALL 
    COMMAND csc.exe /t:exe /platform:anycpu32bitpreferred /r:System.dll /r:DiaLib.dll /out:${CMAKE_CURRENT_BINARY_DIR}/dactablegen.exe ${DACTABLEGEN_SOURCES}
    COMMAND ${CMAKE_COMMAND} -E copy DIAlib.dll ${CMAKE_CURRENT_BINARY_DIR}
    DEPENDS ${DACTABLEGEN_SOURCES} DIAlib.dll
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
)

# In order to use dactablegen as an executable target it needs to be imported. 
# Target is used in dll/mscoree/coreclr/cmakelists.txt
add_executable(dactablegen_exe IMPORTED GLOBAL)
set_property(TARGET dactablegen_exe PROPERTY IMPORTED_LOCATION ${CMAKE_CURRENT_BINARY_DIR}/dactablegen.exe)
