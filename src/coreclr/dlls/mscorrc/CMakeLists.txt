include_directories("../../pal/prebuilt/corerror")

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

if(CLR_CMAKE_HOST_WIN32)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})

    # remove /guard:cf, /guard:ehcont, and /CETCOMPAT from resource-only libraries
    set_property(DIRECTORY PROPERTY CLR_CONTROL_FLOW_GUARD OFF)

    if (CLR_CMAKE_HOST_ARCH_AMD64)
        set_property(DIRECTORY PROPERTY CLR_EH_CONTINUATION OFF)
        string(REPLACE "/CETCOMPAT" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
    endif (CLR_CMAKE_HOST_ARCH_AMD64)

    add_library_clr(mscorrc SHARED
        include.rc
    )
    install_clr(TARGETS mscorrc DESTINATIONS . sharedFramework COMPONENT runtime)

else()
    build_resources(${CMAKE_CURRENT_SOURCE_DIR}/include.rc mscorrc TARGET_CPP_FILE)
    add_library_clr(mscorrc OBJECT ${TARGET_CPP_FILE})

endif(CLR_CMAKE_HOST_WIN32)
