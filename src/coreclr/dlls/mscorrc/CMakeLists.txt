include_directories("../../pal/prebuilt/corerror")

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

if(CLR_CMAKE_HOST_WIN32)
    add_library_clr(mscorrc MODULE
        include.rc
    )
    install_clr(TARGETS mscorrc DESTINATIONS . sharedFramework COMPONENT runtime)

else()
    build_resources(${CMAKE_CURRENT_SOURCE_DIR}/include.rc mscorrc TARGET_CPP_FILE)
    add_library_clr(mscorrc OBJECT ${TARGET_CPP_FILE})

endif(CLR_CMAKE_HOST_WIN32)
