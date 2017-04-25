remove_definitions(-D_TARGET_X86_=1)
add_definitions(-D_TARGET_ARM_)

add_library_clr(gcinfo_arm
    STATIC
    ${GCINFO_SOURCES}
)
