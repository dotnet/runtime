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
target_link_libraries(regtest PRIVATE dnmd::interfaces gtest gmock minipal_com regpal) # Reference gmock for better collection assertions

target_compile_definitions(regtest PRIVATE COM_NO_WINDOWS_H)

if (NOT WIN32)
    target_link_libraries(regtest PRIVATE minipal_comhdrs)
else()
    target_link_libraries(regtest PRIVATE WIL)
endif()

add_custom_target(Regression.TargetAssembly
  dotnet build ${CMAKE_CURRENT_SOURCE_DIR}/../Regression.TargetAssembly/Regression.TargetAssembly.ilproj -c $<CONFIG>
  BYPRODUCTS ${CMAKE_SOURCE_DIR}/artifacts/managed/bin/Regression.TargetAssembly/$<LOWER_CASE:$<CONFIG>>/Regression.TargetAssembly.dll
  COMMENT "Building Regression.TargetAssembly.dll"
  )

add_dependencies(regtest Regression.TargetAssembly)

add_custom_command(TARGET regtest POST_BUILD
                    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:dnmd::interfaces> $<TARGET_FILE_DIR:regtest>
                    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_SOURCE_DIR}/artifacts/managed/bin/Regression.TargetAssembly/$<LOWER_CASE:$<CONFIG>>/Regression.TargetAssembly.dll $<TARGET_FILE_DIR:regtest>)

if(WIN32)
    add_custom_command(TARGET regtest POST_BUILD
                        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:nethost> $<TARGET_FILE_DIR:regtest>)
endif()

# gtest_discover_tests(regtest DISCOVERY_TIMEOUT 1200)
