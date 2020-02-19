remove_definitions(-DTARGET_AMD64)
add_definitions(-DTARGET_ARM64)

add_library_clr(gcinfo_arm64
    STATIC
    ${GCINFO_SOURCES}
)
