# Add targets to the crosscomponents subcomponent build
if (CLR_CMAKE_HOST_OS STREQUAL CLR_CMAKE_TARGET_OS)
    install_clr (TARGETS
        jitinterface_${ARCH_HOST_NAME}
        DESTINATIONS .
        COMPONENT crosscomponents
    )

    if (CLR_CMAKE_TARGET_OSX AND ARCH_TARGET_NAME STREQUAL arm64)
        install_clr (TARGETS
            clrjit_universal_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    elseif (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
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
endif()

if(NOT CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_OSX AND NOT FEATURE_CROSSBITNESS)
    install_clr (TARGETS
        mscordaccore
        mscordbi
        DESTINATIONS . sharedFramework
        COMPONENT crosscomponents
    )
endif()
