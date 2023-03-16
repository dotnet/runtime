# Add targets to the crosscomponents subcomponent build
if (CLR_CMAKE_HOST_OS STREQUAL CLR_CMAKE_TARGET_OS)
    install_clr (TARGETS
        jitinterface_${ARCH_HOST_NAME}
        DESTINATIONS .
        COMPONENT crosscomponents
    )

    if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
        install_clr (TARGETS
            clrjit_universal_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    elseif (CLR_CMAKE_TARGET_UNIX)
        install_clr (TARGETS
            clrjit_unix_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    else()
        install_clr (TARGETS
            clrjit_win_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    endif()

    if (CLR_CMAKE_HOST_ARCH STREQUAL CLR_CMAKE_TARGET_ARCH)
        install_clr (TARGETS
            mscordaccore
            mscordbi
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    endif()
endif()

# We support the DAC as a cross-component for a few specific cases:
# - Same OS and architecture, sanitized runtime with unsanitized DAC
# - Windows Host OS, any target OS, different architectures with the same bitness
if((NOT CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_APPLE AND NOT FEATURE_CROSSBITNESS) OR (CLR_CMAKE_HOST_OS STREQUAL CLR_CMAKE_TARGET_OS AND CLR_CMAKE_HOST_ARCH STREQUAL CLR_CMAKE_TARGET_ARCH))
    install_clr (TARGETS
        mscordaccore
        mscordbi
        DESTINATIONS . sharedFramework
        COMPONENT crosscomponents
    )
endif()
