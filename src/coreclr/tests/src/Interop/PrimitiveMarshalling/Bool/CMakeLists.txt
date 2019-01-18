cmake_minimum_required (VERSION 2.6)
project (NativeBool)
set(SOURCES BoolNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 

# add the executable
add_library (BoolNative SHARED ${SOURCES})
target_link_libraries(BoolNative ${LINK_LIBRARIES_ADDITIONAL}) 

#get_cmake_property(_variableNames VARIABLES)
#foreach (_variableName ${_variableNames})
#    message(STATUS "${_variableName}=${${_variableName}}")
#endforeach()
 

#SET(LIBRARY_OUTPUT_PATH ${PROJECT_BINARY_DIR}/lib)

# add the install targets
install (TARGETS BoolNative DESTINATION bin)
