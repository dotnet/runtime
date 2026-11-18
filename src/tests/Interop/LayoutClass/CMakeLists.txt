project (LayoutClassNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES LayoutClassNative.cpp )

# add the executable
add_library (LayoutClassNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(LayoutClassNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS LayoutClassNative DESTINATION bin)
