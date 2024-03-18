# Add targets to the crosscomponents subcomponent build
if (CLR_CMAKE_HOST_OS STREQUAL CLR_CMAKE_TARGET_OS OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST)
    install_clr (TARGETS
        jitinterface_${ARCH_HOST_NAME}
        DESTINATIONS .
        COMPONENT crosscomponents
    )

    if (NOT (CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST))
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
            if (CLR_CMAKE_TARGET_ARCH_I386)
                install_clr (TARGETS
                    clrjit_win_aot_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
                    DESTINATIONS .
                    COMPONENT crosscomponents
                )
            endif()
        endif()
    endif()
endif()

if(NOT CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_APPLE AND NOT FEATURE_CROSSBITNESS)
    install_clr (TARGETS
        mscordaccore
        mscordbi
        DESTINATIONS . sharedFramework
        COMPONENT crosscomponents
    )
endif()
