# Add targets to the crosscomponents subcomponent build

if (CLR_CMAKE_HOST_OS STREQUAL CLR_CMAKE_TARGET_OS)
    install_clr (TARGETS
        clrjit
        jitinterface_${ARCH_HOST_NAME}
        DESTINATIONS .
        COMPONENT crosscomponents
    )

    if(CLR_CMAKE_HOST_LINUX OR NOT FEATURE_CROSSBITNESS)
        install_clr (TARGETS
            crossgen
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    endif()

    if (CLR_CMAKE_TARGET_UNIX)
        install_clr (TARGETS
            clrjit_unix_${ARCH_TARGET_NAME}_${ARCH_HOST_NAME}
            DESTINATIONS .
            COMPONENT crosscomponents
        )
    endif(CLR_CMAKE_TARGET_UNIX)
endif()

if(NOT CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_OSX AND NOT FEATURE_CROSSBITNESS)
    install_clr (TARGETS
        mscordaccore
        mscordbi
        DESTINATIONS .
        COMPONENT crosscomponents
    )
endif()

if (CLR_CMAKE_TARGET_WIN32 AND NOT CLR_CMAKE_CROSS_ARCH)
    add_dependencies(crosscomponents InjectResource GenClrDebugResource)
endif()
