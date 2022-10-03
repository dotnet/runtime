cmake_minimum_required(VERSION 3.13.0)
project (MonoEmbeddingApiTest)
include_directories(${INC_PLATFORM_DIR})


add_subdirectory(${CLR_SRC_NATIVE_DIR}/public public_api)

# add the library
add_library (mono-embedding-api-test SHARED mono-embedding-api-test.c api-types.h api-functions.h)
set_target_properties(mono-embedding-api-test PROPERTIES MSVC_WARNING_LEVEL 3)
target_link_libraries(mono-embedding-api-test monoapi ${LINK_LIBRARIES_ADDITIONAL})

if(CLR_CMAKE_HOST_OSX)
  target_compile_definitions(mono-embedding-api-test PRIVATE -DHOST_DARWIN)
elseif(CLR_CMAKE_HOST_WIN32)
  target_compile_definitions(mono-embedding-api-test PRIVATE -DHOST_WIN32 -D_CRT_SECURE_NO_WARNINGS)
else()
  target_compile_definitions(mono-embedding-api-test PRIVATE -DHOST_LINUX)
endif()

# add the install targets
install (TARGETS mono-embedding-api-test DESTINATION bin)

