project (CustomMainNative)
include_directories(${INC_PLATFORM_DIR})

add_library (CustomMainNative STATIC CustomMainNative.cpp)

# add the install targets
install (TARGETS CustomMainNative DESTINATION bin)
