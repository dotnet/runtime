set(HEADERS
    ./baseline.h
    ./fixtures.h
    ./asserts.h
)
set(SOURCES
    ./main.cpp
    ./discovery.cpp
    ./metadata.cpp
)

add_executable(regtest ${SOURCES} ${HEADERS})
target_link_libraries(regtest PRIVATE dnmd::interfaces gtest gmock dncp::dncp regpal) # Reference gmock for better collection assertions
set_target_properties(regtest PROPERTIES CXX_STANDARD 17 CXX_STANDARD_REQUIRED ON) # Require C++17 for the tests so we can use std::filesystem.

target_compile_definitions(regtest PRIVATE COM_NO_WINDOWS_H)

if (NOT WIN32)
    target_link_libraries(regtest PRIVATE dncp::winhdrs)
endif()

if (WIN32)
    FetchContent_Declare(
        wil
        GIT_REPOSITORY
        https://github.com/microsoft/wil.git
        GIT_TAG
        v1.0.231216.1
    )

    set(WIL_BUILD_PACKAGING OFF CACHE BOOL "" FORCE)
    set(WIL_BUILD_TESTS OFF CACHE BOOL "" FORCE)

    FetchContent_MakeAvailable(wil)
    target_link_libraries(regtest PRIVATE WIL)
endif()

add_custom_target(Regression.TargetAssembly
  dotnet build ${CMAKE_CURRENT_SOURCE_DIR}/../Regression.TargetAssembly/Regression.TargetAssembly.ilproj -c $<CONFIG>
  BYPRODUCTS ${CMAKE_BINARY_DIR}/managed/bin/Regression.TargetAssembly/$<LOWER_CASE:$<CONFIG>>/Regression.TargetAssembly.dll
  COMMENT "Building Regression.TargetAssembly.dll"
  )

add_dependencies(regtest Regression.TargetAssembly)

add_custom_command(TARGET regtest POST_BUILD
                    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:dnmd::interfaces> $<TARGET_FILE_DIR:regtest>
                    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_BINARY_DIR}/managed/bin/Regression.TargetAssembly/$<LOWER_CASE:$<CONFIG>>/Regression.TargetAssembly.dll $<TARGET_FILE_DIR:regtest>)

if(WIN32)
    add_custom_command(TARGET regtest POST_BUILD
                        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:nethost> $<TARGET_FILE_DIR:regtest>)
endif()

gtest_discover_tests(regtest)