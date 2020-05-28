remove_definitions(-DTARGET_X86)
add_definitions(-DTARGET_ARM)

add_library_clr(gcinfo_arm
    STATIC
    ${GCINFO_SOURCES}
)
