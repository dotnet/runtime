cmake_minimum_required(VERSION 3.13.0)
project (MonoEmbeddingApiTest)
include_directories(${INC_PLATFORM_DIR})


add_subdirectory(${CLR_SRC_NATIVE_DIR}/monoapi monoapi)

# add the library
add_library (mono-embedding-api-test SHARED mono-embedding-api-test.cpp)
target_link_libraries(mono-embedding-api-test monoapi ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS mono-embedding-api-test DESTINATION bin)

