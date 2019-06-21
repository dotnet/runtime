include_directories("inc")
include_directories("strongname/inc")
include_directories("inc/winrt")
include_directories("debug/inc")
include_directories("debug/inc/${ARCH_SOURCES_DIR}")
include_directories("debug/inc/dump")
include_directories("md/inc")
include_directories("classlibnative/bcltype")
include_directories("classlibnative/cryptography")
include_directories("classlibnative/inc")
include_directories("${GENERATED_INCLUDE_DIR}")

if(WIN32 AND FEATURE_EVENT_TRACE)
    include_directories("${GENERATED_INCLUDE_DIR}/etw")
endif(WIN32 AND FEATURE_EVENT_TRACE)


if(CLR_CMAKE_PLATFORM_UNIX)
  if(CLR_CMAKE_PLATFORM_LINUX AND NOT CLR_CMAKE_PLATFORM_UNIX_X86)
    add_subdirectory(debug/createdump)
  endif(CLR_CMAKE_PLATFORM_LINUX AND NOT CLR_CMAKE_PLATFORM_UNIX_X86)

  # Include the dummy c++ include files
  include_directories("pal/inc/rt/cpp")

  # This prevents inclusion of standard C compiler headers
  add_compile_options(-nostdinc)

  set (NATIVE_RESOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/nativeresources)
  include_directories(${NATIVE_RESOURCE_DIR})
  set (RC_TO_CPP ${NATIVE_RESOURCE_DIR}/rctocpp.awk)
  set (PROCESS_RC ${NATIVE_RESOURCE_DIR}/processrc.awk)
  set (RESOURCE_STRING_HEADER_DIR ${NATIVE_RESOURCE_DIR})

  # Create a command to create a C++ source file containing an array of
  # NativeStringResource structs which represent the information from a
  # given Windows .rc file. The target C++ file path is returned in the
  # variable specified by the TARGET_FILE parameter.
  function(build_resources SOURCE TARGET_NAME TARGET_FILE)

    get_compile_definitions(PREPROCESS_DEFINITIONS)
    get_include_directories(INCLUDE_DIRECTORIES)

    set(PREPROCESSED_SOURCE ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.rc.i)
    set(RESOURCE_ENTRY_ARRAY_CPP ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.cpp)

    add_custom_command(
      OUTPUT ${RESOURCE_ENTRY_ARRAY_CPP}
      # Preprocess the windows .rc file
      COMMAND ${CMAKE_CXX_COMPILER} -E -P ${PREPROCESS_DEFINITIONS} ${INCLUDE_DIRECTORIES} -o ${PREPROCESSED_SOURCE} -x c ${SOURCE}
      # Convert the preprocessed .rc file to a C++ file which will be used to make a static lib.
      COMMAND ${AWK} -v name=${TARGET_NAME} -f ${RC_TO_CPP} -f ${PROCESS_RC} ${PREPROCESSED_SOURCE} >${RESOURCE_ENTRY_ARRAY_CPP}
            DEPENDS ${SOURCE} ${RC_TO_CPP} ${PROCESS_RC}
    )

    include_directories(${RESOURCE_STRING_HEADER_DIR})
    set(${TARGET_FILE} ${RESOURCE_ENTRY_ARRAY_CPP} PARENT_SCOPE)

  endfunction()

  add_subdirectory(nativeresources)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(utilcode)
add_subdirectory(gcinfo)
add_subdirectory(coreclr)
add_subdirectory(jit)
add_subdirectory(vm)
add_subdirectory(md)
add_subdirectory(debug)
add_subdirectory(inc)
add_subdirectory(strongname)
add_subdirectory(binder)
add_subdirectory(classlibnative)
add_subdirectory(dlls)
add_subdirectory(ToolBox)
add_subdirectory(tools)
add_subdirectory(unwinder)
add_subdirectory(ildasm)
add_subdirectory(ilasm)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(palrt)
endif(CLR_CMAKE_PLATFORM_UNIX)
