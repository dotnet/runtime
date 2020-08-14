cmake_minimum_required(VERSION 3.8)
# Quick note: The CMake C# support is using the CSC bundled with the MSBuild that the native build runs on, not the one supplied by the local .NET SDK.

project(DacTableGen LANGUAGES CSharp)

file(TO_CMAKE_PATH "$ENV{VSInstallDir}\\DIA SDK" DIASDK_DIR)
file(COPY "${DIASDK_DIR}/bin/msdia140.dll" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")

set (DIALib "${CMAKE_CURRENT_BINARY_DIR}/DIALib.dll")
FIND_PROGRAM(TLBIMP tlbimp.exe)
FIND_PROGRAM(MIDL midl.exe)
add_custom_command(
    OUTPUT "${DIALib}"
    COMMAND ${MIDL} /I "${DIASDK_DIR}/include" "${DIASDK_DIR}/idl/dia2.idl" /tlb "${CMAKE_CURRENT_BINARY_DIR}/dia2.tlb"
    COMMAND ${TLBIMP} dia2.tlb /out:"${DIALib}"
)
add_custom_target(gen_dialib DEPENDS "${DIALib}")

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
add_dependencies(dactablegen gen_dialib)

set_target_properties(dactablegen PROPERTIES VS_DOTNET_REFERENCES "System")
set_target_properties(dactablegen PROPERTIES VS_DOTNET_REFERENCE_DIALib "${DIALib}")
