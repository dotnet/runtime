project(System.Native.Browser C)

set(BROWSER_SOURCES)

if (NOT GEN_PINVOKE)
    set (BROWSER_SOURCES ${BROWSER_SOURCES} entrypoints.c)
endif()

add_library(System.Native.Browser-Static
    STATIC
    ${BROWSER_SOURCES}
)
set_target_properties(System.Native.Browser-Static PROPERTIES OUTPUT_NAME System.Native.Browser CLEAN_DIRECT_OUTPUT 1)
install(TARGETS System.Native.Browser-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
install(TARGETS System.Native.Browser-Static DESTINATION sharedFramework COMPONENT runtime)

add_subdirectory(../Common/JavaScript common-js)
add_dependencies(System.Native.Browser-Static System.Native.Browser-Rollup)
