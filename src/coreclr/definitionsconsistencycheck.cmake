get_directory_property( DirDefs COMPILE_DEFINITIONS )

# Reset the definition file
file(WRITE ${CMAKE_CURRENT_BINARY_DIR}/cmake.definitions "")
foreach( d ${DirDefs} )
    if($ENV{VERBOSE})
        message( STATUS "Compiler Definition: " ${d} )
    endif($ENV{VERBOSE})
    file(APPEND ${CMAKE_CURRENT_BINARY_DIR}/cmake.definitions ${d})
    file(APPEND ${CMAKE_CURRENT_BINARY_DIR}/cmake.definitions "\n")
endforeach()
