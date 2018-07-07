set(DACTABLEGEN_SOURCES
    cvconst.cs
    diautil.cs
    main.cs
    MapSymbolProvider.cs
)

# Cmake does not support C# sources so add custom command
# Disable:
#   warning CS1668: Invalid search path 'C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\VC\Tools\MSVC\14.14.26428\atlmfc\lib\ARM64' specified in 'LIB environment variable' -- 'The system cannot find the path specified. '
# There are several problems here: (1) the C++ project files are adding non-existent directories to the LIB path, especially for ARM and ARM64,
# (2) This CSC is invoking the desktop .NET Framework CSC, not the buildtools version.
add_custom_target(dactablegen ALL 
    COMMAND csc.exe /t:exe /platform:anycpu32bitpreferred /r:System.dll /r:DiaLib.dll /nowarn:1668 /out:${CMAKE_CURRENT_BINARY_DIR}/dactablegen.exe ${DACTABLEGEN_SOURCES}
    COMMAND ${CMAKE_COMMAND} -E copy DIAlib.dll ${CMAKE_CURRENT_BINARY_DIR}
    DEPENDS ${DACTABLEGEN_SOURCES} DIAlib.dll
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
)

# In order to use dactablegen as an executable target it needs to be imported. 
# Target is used in dll/mscoree/coreclr/cmakelists.txt
add_executable(dactablegen_exe IMPORTED GLOBAL)
set_property(TARGET dactablegen_exe PROPERTY IMPORTED_LOCATION ${CMAKE_CURRENT_BINARY_DIR}/dactablegen.exe)
