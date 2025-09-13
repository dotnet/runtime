project(Browser.Native C)

set(BROWSER_SOURCES
    entrypoints.c
)
add_library(Browser.Native-Static
    STATIC
    ${BROWSER_SOURCES}
)
set_target_properties(Browser.Native-Static PROPERTIES OUTPUT_NAME Browser.Native CLEAN_DIRECT_OUTPUT 1)
install(TARGETS Browser.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
