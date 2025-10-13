project(System.Runtime.InteropServices.JavaScript.Native C)

set(BROWSER_INTEROP_SOURCES)

if (NOT GEN_PINVOKE)
    set (BROWSER_INTEROP_SOURCES ${BROWSER_INTEROP_SOURCES} entrypoints.c)
endif()

add_library(System.Runtime.InteropServices.JavaScript.Native-Static
    STATIC
    ${BROWSER_INTEROP_SOURCES}
)
set_target_properties(System.Runtime.InteropServices.JavaScript.Native-Static PROPERTIES OUTPUT_NAME System.Runtime.InteropServices.JavaScript.Native CLEAN_DIRECT_OUTPUT 1)
install(TARGETS System.Runtime.InteropServices.JavaScript.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
install(TARGETS System.Runtime.InteropServices.JavaScript.Native-Static DESTINATION sharedFramework COMPONENT runtime)
