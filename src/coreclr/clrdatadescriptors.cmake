# cDAC contract descriptor

function(generate_data_descriptors)
  set(options EXPORT_VISIBLE)
  set(oneValueArgs LIBRARY_NAME CONTRACT_FILE CONTRACT_NAME INTERFACE_TARGET)
  set(multiValueArgs "")
  cmake_parse_arguments(DATA_DESCRIPTORS "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGV})

  # INTERMEDIARY_LIBRARY is used as part of the build and not linked into the final product.
  set(INTERMEDIARY_LIBRARY ${DATA_DESCRIPTORS_LIBRARY_NAME}_temp)
  set(LIBRARY ${DATA_DESCRIPTORS_LIBRARY_NAME})

  set(DATA_DESCRIPTOR_SHARED_SOURCE_DIR "${CMAKE_CURRENT_FUNCTION_LIST_DIR}/debug/datadescriptor-shared")
  set(GENERATED_CDAC_DESCRIPTOR_DIR "${CMAKE_CURRENT_BINARY_DIR}/cdac-${LIBRARY}")

  # configure contract export name
  set(POINTER_DATA_NAME ${DATA_DESCRIPTORS_CONTRACT_NAME}PointerData)
  set(CONTRACT_NAME ${DATA_DESCRIPTORS_CONTRACT_NAME})
  if (DATA_DESCRIPTORS_EXPORT_VISIBLE)
    set(EXPORT_CONTRACT 1)
  else()
    set(EXPORT_CONTRACT 0)
  endif()
  configure_file("${DATA_DESCRIPTOR_SHARED_SOURCE_DIR}/contractconfiguration.h.in" "${GENERATED_CDAC_DESCRIPTOR_DIR}/contractconfiguration.h")

  if (NOT CDAC_BUILD_TOOL_BINARY_PATH)
    # if CDAC_BUILD_TOOL_BINARY_PATH is unspecified (for example for a build without a .NET SDK or msbuild),
    # link a stub contract descriptor into the runtime
    add_library_clr(${LIBRARY} OBJECT "${DATA_DESCRIPTOR_SHARED_SOURCE_DIR}/contractdescriptorstub.c")
    target_include_directories(${LIBRARY} PRIVATE ${GENERATED_CDAC_DESCRIPTOR_DIR})
    message(STATUS "Using a stub cDAC contract descriptor")
  else()
    # generate a contract descriptor using cdac-build-tool from a data descriptor and contract json file

    if(NOT EXISTS "${CDAC_BUILD_TOOL_BINARY_PATH}")
      message(FATAL_ERROR "${CDAC_BUILD_TOOL_BINARY_PATH} does not exist")
    endif()

    add_library(${INTERMEDIARY_LIBRARY} OBJECT "${DATA_DESCRIPTOR_SHARED_SOURCE_DIR}/datadescriptor.cpp")

    if(CLR_CMAKE_TARGET_WIN32)
      # turn off whole program optimization:
      # 1. it creates object files that cdac-build-tool can't read
      # 2. we never link INTERMEDIARY_LIBRARY into the final product - it's only job is to be scraped
      set_target_properties(${INTERMEDIARY_LIBRARY} PROPERTIES
          INTERPROCEDURAL_OPTIMIZATION_RELEASE OFF
          INTERPROCEDURAL_OPTIMIZATION_RELWITHDEBINFO OFF)
    endif()

    # inherit definitions, include directories, and dependencies from the INTERFACE target
    target_link_libraries(${INTERMEDIARY_LIBRARY} PRIVATE ${DATA_DESCRIPTORS_INTERFACE_TARGET})

    set(CONTRACT_BASELINE_DIR "${CLR_REPO_ROOT_DIR}/docs/design/datacontracts/data")
    set(CONTRACT_DESCRIPTOR_INPUT "${DATA_DESCRIPTOR_SHARED_SOURCE_DIR}/contract-descriptor.c.in")
    set(CONTRACT_DESCRIPTOR_OUTPUT "${GENERATED_CDAC_DESCRIPTOR_DIR}/contract-descriptor.c")
    set(CONTRACT_FILE "${DATA_DESCRIPTORS_CONTRACT_FILE}")

    # generate the contract descriptor by running cdac-build-tool
    # n.b. this just uses `dotnet` from the PATH.  InitializeDotNetCli adds the appropriate directory
    add_custom_command(
      OUTPUT "${CONTRACT_DESCRIPTOR_OUTPUT}"
      VERBATIM
      COMMAND ${CLR_DOTNET_HOST_PATH} ${CDAC_BUILD_TOOL_BINARY_PATH} compose -i "${CONTRACT_DESCRIPTOR_INPUT}" -o "${CONTRACT_DESCRIPTOR_OUTPUT}" -b "${CONTRACT_BASELINE_DIR}" -c "${CONTRACT_FILE}" $<TARGET_OBJECTS:${INTERMEDIARY_LIBRARY}>
      DEPENDS ${INTERMEDIARY_LIBRARY} ${DATA_DESCRIPTORS_DEPENDENCIES} $<TARGET_OBJECTS:${INTERMEDIARY_LIBRARY}> "${CONTRACT_FILE}" "${CONTRACT_DESCRIPTOR_INPUT}"
      USES_TERMINAL
    )

    # It is important that LIBRARY is an object library;
    # if it was static, linking it into the final dll would not export
    # ${CONTRACT_NAME} since it is not referenced anywhere.
    add_library_clr(${LIBRARY} OBJECT
      "${CONTRACT_DESCRIPTOR_OUTPUT}"
      "${DATA_DESCRIPTOR_SHARED_SOURCE_DIR}/contractpointerdata.cpp"
    )
    add_dependencies(${LIBRARY} ${INTERMEDIARY_LIBRARY})

    target_include_directories(${LIBRARY} PRIVATE ${GENERATED_CDAC_DESCRIPTOR_DIR})

    # inherit definitions, include directories, and dependencies from the INTERFACE target
    target_link_libraries(${LIBRARY} PRIVATE ${DATA_DESCRIPTORS_INTERFACE_TARGET})

    if(MSVC)
      # /Zc:externConstexpr is required to export constexpr variables
      # from the object file, otherwise the linker will not export them.
      # See https://learn.microsoft.com/en-us/cpp/build/reference/zc-externconstexpr
      # for more details.
      target_compile_options(${LIBRARY} PRIVATE /Zc:externConstexpr)
    endif(MSVC)
  endif()
endfunction(generate_data_descriptors)
