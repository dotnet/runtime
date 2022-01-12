project (MonoEmbeddingApiTest)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES mono-embedding-api-test.cpp )

# add the executable
add_library (mono-embedding-api-test SHARED ${SOURCES})
target_link_libraries(mono-embedding-api-test ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS mono-embedding-api-test DESTINATION bin)
