add_executable(internal_conversion conversion.cpp translatesig.cpp)
target_include_directories(internal_conversion PRIVATE
    "${PROJECT_SOURCE_DIR}/src/interfaces"
    "${CLR_SRC_NATIVE_DIR}/../coreclr/minipal")
target_link_libraries(internal_conversion PRIVATE dnmd::interfaces_static gtest_main)
target_compile_definitions(internal_conversion PRIVATE COM_NO_WINDOWS_H)
if (WIN32)
    target_compile_definitions(internal_conversion PRIVATE NOMINMAX _CRT_SECURE_NO_WARNINGS)
endif()
gtest_discover_tests(internal_conversion)
