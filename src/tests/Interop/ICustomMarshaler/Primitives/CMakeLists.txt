project (CustomMarshalersPrimitives)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES ICustomMarshalerNative.cpp )

# add the executable
add_library (CustomMarshalersPrimitives ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(CustomMarshalersPrimitives PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS CustomMarshalersPrimitives DESTINATION bin)
