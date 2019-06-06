cmake_minimum_required(VERSION 3.8) # This project is only included on Win32 platforms so we can have a higher CMake version requirement since we already require a newer CMake version on Windows for various reasons.
# Quick note: The CMake C# support is using the CSC bundled with the MSBuild that the native build runs on, not the one supplied by the local .NET SDK.

project(DacTableGen LANGUAGES CSharp)

set(DACTABLEGEN_SOURCES
    cvconst.cs
    diautil.cs
    main.cs
    MapSymbolProvider.cs
)
# DacTableGen doesn't use the defines from the rest of the build tree, so clear all of the compile definitions
set_directory_properties(PROPERTIES COMPILE_DEFINITIONS "")

set(CMAKE_CSharp_FLAGS "/platform:anycpu32bitpreferred")
add_executable(dactablegen ${DACTABLEGEN_SOURCES})

set_target_properties(dactablegen PROPERTIES VS_DOTNET_REFERENCES "System")
set_target_properties(dactablegen PROPERTIES VS_DOTNET_REFERENCE_DIALib ${CMAKE_CURRENT_SOURCE_DIR}/DIALib.dll)
