project (LayoutClassNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES LayoutClassNative.cpp )

# add the executable
add_library (LayoutClassNative SHARED ${SOURCES})
target_link_libraries(LayoutClassNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS LayoutClassNative DESTINATION bin)
