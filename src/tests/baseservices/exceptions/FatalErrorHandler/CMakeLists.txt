project (FatalErrorHandlerNative)

include_directories(${INC_PLATFORM_DIR})
include_directories(${CLR_SRC_NATIVE_DIR}/public)

set(SOURCES FatalErrorHandlerNative.cpp)

# add the shared library
add_library (FatalErrorHandlerNative SHARED ${SOURCES})

# add the install targets
install (TARGETS FatalErrorHandlerNative DESTINATION bin)
