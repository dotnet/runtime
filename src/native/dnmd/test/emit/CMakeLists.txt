set(SOURCES
	typeref.cpp
	import.cpp
	module.cpp
	typedef.cpp
	moduleref.cpp
	methoddef.cpp
	standalonesig.cpp
	memberref.cpp
	typespec.cpp
	assembly.cpp
	assemblyref.cpp
	param.cpp
	fieldmarshal.cpp
	customattribute.cpp
	fieldrva.cpp
	duplicates.cpp
	initialsize.cpp
	updatemode.cpp)

if (DNMD_ENABLE_INTERNAL_INTERFACES)
	list(APPEND SOURCES metadataemithelper.cpp)
endif()

set(HEADERS emit.hpp)

add_executable(emit ${SOURCES} ${HEADERS})
target_include_directories(emit PRIVATE "${CLR_SRC_NATIVE_DIR}/../coreclr/minipal")
target_link_libraries(emit PRIVATE dnmd::interfaces_static gtest_main gmock)
if (WIN32)
    target_compile_definitions(emit PRIVATE COM_NO_WINDOWS_H NOMINMAX _CRT_SECURE_NO_WARNINGS)
endif()

if (DNMD_ENABLE_SANITIZERS)
    set(SANITIZER_SUPP_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../sanitizers")
    gtest_discover_tests(emit PROPERTIES ENVIRONMENT
        "ASAN_OPTIONS=detect_odr_violation=0;LSAN_OPTIONS=suppressions=${SANITIZER_SUPP_DIR}/lsan.supp;UBSAN_OPTIONS=suppressions=${SANITIZER_SUPP_DIR}/ubsan.supp")
else()
    gtest_discover_tests(emit)
endif()