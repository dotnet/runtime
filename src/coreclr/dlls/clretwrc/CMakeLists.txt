include_directories(${CMAKE_CURRENT_BINARY_DIR})

add_definitions(-DFX_VER_INTERNALNAME_STR=clretwrc.dll)

add_library_clr(clretwrc SHARED
    clretwrc.rc
)

# add the install targets
install_clr(TARGETS clretwrc DESTINATIONS . sharedFramework COMPONENT runtime)

add_dependencies(clretwrc eventing_headers)
