include_directories(${CMAKE_CURRENT_BINARY_DIR})

if(WIN32)
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})

    # remove /guard:cf from resource-only libraries
    string(REPLACE "/guard:cf" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
    
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
endif(WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=clretwrc.dll)

add_library_clr(clretwrc SHARED
    clretwrc.rc
)

# add the install targets
install_clr(clretwrc)

add_dependencies(clretwrc eventing_headers)
