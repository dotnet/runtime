cmake_minimum_required (VERSION 2.6)
project (RefCharArrayNative)
set(SOURCES RefCharArrayNative.cpp )

# add the executable
add_library (RefCharArrayNative SHARED ${SOURCES})

#get_cmake_property(_variableNames VARIABLES)
#foreach (_variableName ${_variableNames})
#    message(STATUS "${_variableName}=${${_variableName}}")
#endforeach()
 

#SET(LIBRARY_OUTPUT_PATH ${PROJECT_BINARY_DIR}/lib)

# add the install targets
install (TARGETS RefCharArrayNative DESTINATION bin)