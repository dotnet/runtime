cmake_minimum_required (VERSION 2.6)
project (CustomMarshalersNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES CustomMarshalersNative.cpp )

# add the executable
add_library (CustomMarshalersNative SHARED ${SOURCES})
target_link_libraries(CustomMarshalersNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS CustomMarshalersNative DESTINATION bin)
