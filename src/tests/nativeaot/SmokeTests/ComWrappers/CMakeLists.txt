project (ComWrappersNative)
include_directories(${INC_PLATFORM_DIR})

add_library (ComWrappersNative SHARED ComWrappersNative.cpp)

if(CLR_CMAKE_HOST_WIN32)
    target_link_options(ComWrappersNative PRIVATE /DEF:${CMAKE_CURRENT_SOURCE_DIR}/ComWrappersNative.def)
endif(CLR_CMAKE_HOST_WIN32)

# add the install targets
install (TARGETS ComWrappersNative DESTINATION bin)
