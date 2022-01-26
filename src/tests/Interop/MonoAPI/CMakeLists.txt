project (MonoEmbeddingApiTest)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES mono-embedding-api-test.cpp )

# add the executable
add_library (mono-embedding-api-test SHARED ${SOURCES})
target_link_libraries(mono-embedding-api-test ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS mono-embedding-api-test DESTINATION bin)

# from artifacts/tests/coreclr/obj/OSX.x64.Release/Native/Interop/MonoAPI/
include_directories(${PROJECT_BINARY_DIR}/../../../../../../../obj/mono/OSX.x64.Release/
  ${PROJECT_BINARY_DIR}/../../../../../../../obj/mono/OSX.x64.Release/out/include/mono-2.0/
  )

# from src/tests/Interop/MonoAPI/
include_directories(${PROJECT_SOURCE_DIR}/../../../mono/mono/eglib/
  ${PROJECT_SOURCE_DIR}/../../../mono/)
