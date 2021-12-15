project (ComWrappersNative)
include_directories(${INC_PLATFORM_DIR})

add_library (ComWrappersNative SHARED ComWrappersNative.cpp)

# add the install targets
install (TARGETS ComWrappersNative DESTINATION bin)
