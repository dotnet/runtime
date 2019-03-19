add_subdirectory(apphost)
add_subdirectory(dotnet)
add_subdirectory(fxr)
add_subdirectory(hostpolicy)

add_subdirectory(test)

if(WIN32)
    add_subdirectory(comhost)
endif()