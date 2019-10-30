add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.debug.dll)

if(WIN32)

  add_library_clr(mscorrc.debug SHARED
    ../include.rc
  )

  install_clr (mscorrc.debug) 

else()
  build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../include.rc mscorrc_debug TARGET_CPP_FILE)

  add_library_clr(mscorrc_debug STATIC
    ${TARGET_CPP_FILE}
  )

endif(WIN32)

