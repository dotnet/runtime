set(DOTNET_AOT_SOURCES
    %AotSources%
)
set(DOTNET_AOT_OBJECTS
    %AotObjects%
)
set(DOTNET_EXTRA_SOURCES
    %ExtraSources%
)

add_library(
    aot_library STATIC
    ${DOTNET_AOT_SOURCES}   
)
target_link_libraries(
    aot_library
    ${DOTNET_AOT_OBJECTS}
)

add_library(
    lib-%LIBRARY_NAME% %LIBRARY_TYPE%
    ${DOTNET_EXTRA_SOURCES}
)
target_link_libraries(
    lib-%LIBRARY_NAME%
    aot_library
    %LIBRARY_LINKER_ARGS%
)
