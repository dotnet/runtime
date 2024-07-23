project (CustomMainWithStubExeNative)
include_directories(${INC_PLATFORM_DIR})

add_executable (CustomMainWithStubExeNative CustomMainWithStubExeNative.cpp)

# add the install targets
install (TARGETS CustomMainWithStubExeNative DESTINATION bin)
