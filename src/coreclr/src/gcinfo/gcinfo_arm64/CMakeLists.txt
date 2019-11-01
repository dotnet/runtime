remove_definitions(-D_TARGET_AMD64_)
add_definitions(-D_TARGET_ARM64_)

add_library_clr(gcinfo_arm64
    STATIC
    ${GCINFO_SOURCES}
)
