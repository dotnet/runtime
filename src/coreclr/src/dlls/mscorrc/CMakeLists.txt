include_directories("../../pal/prebuilt/corerror")

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

if(CLR_CMAKE_HOST_WIN32)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})

    # remove /guard:cf from resource-only libraries
    string(REPLACE "/guard:cf" "" CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS})

    add_library_clr(mscorrc SHARED
        include.rc
    )

    install_clr(TARGETS mscorrc ADDITIONAL_DESTINATION sharedFramework)
else()
    build_resources(${CMAKE_CURRENT_SOURCE_DIR}/include.rc mscorrc TARGET_CPP_FILE)

    add_library_clr(mscorrc_obj OBJECT
        ${TARGET_CPP_FILE}
    )
    add_library(mscorrc INTERFACE)
    target_sources(mscorrc INTERFACE $<TARGET_OBJECTS:mscorrc_obj>)
endif(CLR_CMAKE_HOST_WIN32)
