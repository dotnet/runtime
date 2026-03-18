include_directories(${CMAKE_CURRENT_BINARY_DIR})

if(CLR_CMAKE_HOST_WIN32)
    # remove /guard:cf, /guard:ehcont, and /CETCOMPAT from resource-only libraries
    set_property(DIRECTORY PROPERTY CLR_CONTROL_FLOW_GUARD OFF)

    if (CLR_CMAKE_HOST_ARCH_AMD64)
        set_property(DIRECTORY PROPERTY CLR_EH_CONTINUATION OFF)
        string(REPLACE "/CETCOMPAT" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
    endif (CLR_CMAKE_HOST_ARCH_AMD64)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
endif(CLR_CMAKE_HOST_WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=clretwrc.dll)

add_library_clr(clretwrc SHARED
    clretwrc.rc
)

# add the install targets
install_clr(TARGETS clretwrc DESTINATIONS . sharedFramework COMPONENT runtime)

add_dependencies(clretwrc eventing_headers)
