project (CustomMarshalersPrimitives)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES ICustomMarshalerNative.cpp )

# add the executable
add_library (CustomMarshalersPrimitives SHARED ${SOURCES})
target_link_libraries(CustomMarshalersPrimitives ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS CustomMarshalersPrimitives DESTINATION bin)
