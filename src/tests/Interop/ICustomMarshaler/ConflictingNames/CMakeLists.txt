project (CustomMarshalersConflictingNames)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES CustomMarshalerNative.cpp )

# add the executable
add_library (CustomMarshalerNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(CustomMarshalerNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS CustomMarshalerNative DESTINATION bin)
