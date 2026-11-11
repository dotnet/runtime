find_package(Python COMPONENTS Interpreter REQUIRED)

set(MOCK_DAC_HEADER ${CMAKE_CURRENT_BINARY_DIR}/mockdac.generated.h)
set(DAC_DBI_INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/../../inc/dacdbiinterface.h)
add_custom_command(
    OUTPUT ${MOCK_DAC_HEADER}
    COMMAND ${Python_EXECUTABLE} ${CMAKE_CURRENT_SOURCE_DIR}/genmockdac.py ${DAC_DBI_INTERFACE} ${MOCK_DAC_HEADER}
    DEPENDS ${DAC_DBI_INTERFACE} ${CMAKE_CURRENT_SOURCE_DIR}/genmockdac.py
    VERBATIM)

add_executable_clr(dbi_value_tests value_tests.cpp ${MOCK_DAC_HEADER})
set_target_properties(dbi_value_tests PROPERTIES
    DBI_COMPONENT TRUE
    MSVC_RUNTIME_LIBRARY MultiThreaded)
target_include_directories(dbi_value_tests PRIVATE ${CMAKE_CURRENT_BINARY_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/..)
target_compile_definitions(dbi_value_tests PRIVATE FEATURE_NO_HOST FEATURE_METADATA_LOAD_TRUSTED_IMAGES)
target_compile_options(dbi_value_tests PRIVATE /wd4100)
if(CMAKE_SIZEOF_VOID_P EQUAL 4)
    target_link_options(dbi_value_tests PRIVATE /EXPORT:DacDbiInterfaceInstance)
endif()
target_link_libraries(dbi_value_tests PRIVATE ${COREDBI_LIBRARIES})

enable_testing()
foreach(pointerSize 4 8)
    foreach(testCase ReferenceSizes ReferenceReads PointerReadsAndWrites
                     HeapReferences HandleReferences HandleRefresh ReferenceAssignment
                     StringAssignment HandleReadsAndWrites RegisterLocals TypedReferenceDereference
                     NativeIntegers SingleRegisterHome RegisterPairHome RegisterMemoryHomes
                     FrameRegisterReads ReturnValueTypes
                     NativeIntegerSizeFailure NativeIntegerConstructionFailure NativeIntegerLiteralFailure
                     NativeIntegerAlignmentFailure BoxSizeFailure
                     HandleConstructionFailure ReferenceConstructionFailure)
        add_test(NAME dbi_value_${testCase}_${pointerSize} COMMAND dbi_value_tests ${testCase} ${pointerSize})
        set_tests_properties(dbi_value_${testCase}_${pointerSize} PROPERTIES TIMEOUT 30)
    endforeach()
endforeach()
