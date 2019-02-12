add_subdirectory(apphost)
add_subdirectory(dotnet)
add_subdirectory(fxr)
add_subdirectory(hostpolicy)

if(WIN32)
    add_subdirectory(comhost)
endif()