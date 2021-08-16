include_directories(${CMAKE_CURRENT_BINARY_DIR})

if(CLR_CMAKE_HOST_WIN32)
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})

    # remove /guard:cf, /guard:ehcont, and /CETCOMPAT from resource-only libraries
    string(REPLACE "/guard:cf" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
    if (CLR_CMAKE_HOST_ARCH_AMD64)
        string(REPLACE "/guard:ehcont" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
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
