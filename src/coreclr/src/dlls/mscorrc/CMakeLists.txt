include_directories("../../pal/prebuilt/corerror")
if(CLR_CMAKE_HOST_WIN32)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})

    # remove /guard:cf from resource-only libraries
    string(REPLACE "/guard:cf" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})
endif(CLR_CMAKE_HOST_WIN32)

add_subdirectory(full)

# Only add the small version of the resources if the platform is Windows.
if(CLR_CMAKE_TARGET_WIN32)
    add_subdirectory(small)
endif(CLR_CMAKE_TARGET_WIN32)

