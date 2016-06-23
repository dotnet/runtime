get_directory_property( DirDefs COMPILE_DEFINITIONS )

# Reset the definition file
file(WRITE cmake.definitions "")
foreach( d ${DirDefs} )
    if($ENV{VERBOSE})
        message( STATUS "Compiler Definition: " ${d} )
    endif($ENV{VERBOSE})
    file(APPEND cmake.definitions ${d})
    file(APPEND cmake.definitions "\n")
endforeach()
