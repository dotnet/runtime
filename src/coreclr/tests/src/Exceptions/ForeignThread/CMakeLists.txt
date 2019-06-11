cmake_minimum_required (VERSION 2.6)

project (ForeignThreadExceptionsNative)

include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_PLATFORM_DARWIN)
  # Enable non-POSIX pthreads APIs, which by default are not included in the pthreads header
  add_definitions(-D_DARWIN_C_SOURCE)
endif(CLR_CMAKE_PLATFORM_DARWIN)

set(SOURCES ForeignThreadExceptionsNative.cpp)

if(NOT WIN32)
add_compile_options(-pthread)
endif()

# add the executable
add_library (ForeignThreadExceptionsNative SHARED ${SOURCES})

# add the install targets
install (TARGETS ForeignThreadExceptionsNative DESTINATION bin)
