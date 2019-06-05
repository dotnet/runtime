cmake_minimum_required (VERSION 2.8.12)
project (WinRTNativeComponent)
include_directories( ${INC_PLATFORM_DIR} )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("../Contracts")
include_directories(${CMAKE_CURRENT_SOURCE_DIR})

set(SOURCES
  Component.Contracts.ArrayTesting.cpp
  Component.Contracts.BindingProjectionsTesting.cpp
  Component.Contracts.BindingViewModel.cpp
  Component.Contracts.BooleanTesting.cpp
  Component.Contracts.EnumTesting.cpp
  Component.Contracts.ExceptionTesting.cpp
  Component.Contracts.KeyValuePairTesting.cpp
  Component.Contracts.NullableTesting.cpp
  Component.Contracts.StringTesting.cpp
  Component.Contracts.TypeTesting.cpp
  Component.Contracts.UriTesting.cpp
)

set (MIDL_GENERATED_WINMD ${CMAKE_CURRENT_BINARY_DIR}/WinMD/Component.Contracts.winmd)
file(TO_NATIVE_PATH ${MIDL_GENERATED_WINMD} MIDL_GENERATED_WINMD_WINDOWS_PATH)

if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
  message(FATAL_ERROR "Windows SDK is required for the WinRT interop tests build.")
else()
  message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION} for WinRT interop tests.")
endif()

file(TO_CMAKE_PATH "$ENV{WindowsSdkDir}" WINDOWS_KITS_DIR)

set (METADATA_DIR "${WINDOWS_KITS_DIR}/References/${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}/windows.foundation.foundationcontract/*" )
set (REFERENCE_WINMDS
  "${WINDOWS_KITS_DIR}/References/${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}/Windows.Foundation.FoundationContract/*/Windows.Foundation.FoundationContract.winmd"
  "${WINDOWS_KITS_DIR}/References/${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}/Windows.Foundation.UniversalApiContract/*/Windows.Foundation.UniversalApiContract.winmd")

file(GLOB RESOLVED_METADATA_DIR ${METADATA_DIR})
file(TO_NATIVE_PATH ${RESOLVED_METADATA_DIR} METADATA_DIR_ARGUMENT)

file(GLOB_RECURSE RESOLVED_REFERENCE_WINMDS ${REFERENCE_WINMDS})

set (REFERENCE_WINMD_ARGS "")
foreach(WINMD ${RESOLVED_REFERENCE_WINMDS})
  file(TO_NATIVE_PATH ${WINMD} WINMD_WINDOWS)
  set(WINMD_WINDOWS "${WINMD_WINDOWS}")
  string(PREPEND WINMD_WINDOWS "/reference")
  list(APPEND REFERENCE_WINMD_ARGS ${WINMD_WINDOWS})
endforeach()

set (MIDL_FILE ../Contracts/Component.Contracts.idl)
set (MIDL_COMMAND_LINE_ARGS
  /winrt
  /metadata_dir ${METADATA_DIR_ARGUMENT}
  /W1
  /enum_class 
  /ns_prefix 
  /target "NT60"  
  /nomidl 
  /h "nul"
  ${REFERENCE_WINMD_ARGS}
)

find_program(MIDLRT midlrt)

add_custom_command(
  OUTPUT ${MIDL_GENERATED_WINMD}
  COMMAND ${MIDLRT} ${MIDL_COMMAND_LINE_ARGS} ${MIDL_FILE} /winmd ${MIDL_GENERATED_WINMD_WINDOWS_PATH}
  WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
  DEPENDS ${MIDL_FILE}
)

set_source_files_properties(${MIDL_GENERATED_WINMD} PROPERTIES GENERATED TRUE)

set (CPP_WINRT_OUTPUT_FOLDER ${CMAKE_CURRENT_BINARY_DIR}/Generated)

set (CPP_WINRT_OUTPUT_FILES
  ${CPP_WINRT_OUTPUT_FOLDER}/winrt/impl/Component.Contracts.0.h
  ${CPP_WINRT_OUTPUT_FOLDER}/winrt/impl/Component.Contracts.1.h
  ${CPP_WINRT_OUTPUT_FOLDER}/winrt/impl/Component.Contracts.2.h
  ${CPP_WINRT_OUTPUT_FOLDER}/winrt/Component.Contracts.h
  ${CPP_WINRT_OUTPUT_FOLDER}/module.g.cpp
)

include_directories(${CPP_WINRT_OUTPUT_FOLDER})

find_program(CPPWINRT cppwinrt)

add_custom_command(
  OUTPUT ${CPP_WINRT_OUTPUT_FILES}
  COMMAND ${CPPWINRT} -name Contracts -comp ${CMAKE_CURRENT_SOURCE_DIR} -in ${MIDL_GENERATED_WINMD} -ref ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION} -out ${CPP_WINRT_OUTPUT_FOLDER}
  DEPENDS ${MIDL_GENERATED_WINMD}
)

set_source_files_properties(${CPP_WINRT_OUTPUT_FILES} PROPERTIES GENERATED TRUE)

list(APPEND SOURCES ${CPP_WINRT_OUTPUT_FOLDER}/module.g.cpp)

if (WIN32)
  list(APPEND SOURCES Exports.def)
endif()

# add the executable
add_library (WinRTNativeComponent SHARED ${SOURCES})
target_link_libraries(WinRTNativeComponent ${LINK_LIBRARIES_ADDITIONAL})
set_target_properties(WinRTNativeComponent PROPERTIES CXX_STANDARD 17)

if(WIN32)
  target_link_libraries(WinRTNativeComponent RuntimeObject.lib)
endif()

# add the install targets
install (TARGETS WinRTNativeComponent DESTINATION bin)
