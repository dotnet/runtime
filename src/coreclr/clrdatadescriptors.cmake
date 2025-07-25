# cDAC contract descriptor

function(generate_data_descriptors)
  set(options "")
  set(oneValueArgs LIBRARY_NAME)
  set(multiValueArgs "")
  cmake_parse_arguments(DATA_DESCRIPTORS "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGV})

  if (NOT CDAC_BUILD_TOOL_BINARY_PATH)
    # if CDAC_BUILD_TOOL_BINARY_PATH is unspecified (for example for a build without a .NET SDK or msbuild),
    # link a stub contract descriptor into the runtime
    add_library_clr(${LIBRARY_NAME} OBJECT contractdescriptorstub.c)
    message(STATUS "Using a stub cDAC contract descriptor")
  else()
    # generate a contract descriptor using cdac-build-tool from a data descriptor and contract json file

    set(LIBRARY ${DATA_DESCRIPTORS_LIBRARY_NAME})
    set(INTERMEDIARY_LIBRARY ${DATA_DESCRIPTORS_LIBRARY_NAME}_INTERMEDIARY)

    add_library(${INTERMEDIARY_LIBRARY} OBJECT datadescriptor.cpp)

    # don't build the data descriptor before the VM (and any of its dependencies' generated headers)
    add_dependencies(${INTERMEDIARY_LIBRARY} cee_wks_core)

    if(CLR_CMAKE_TARGET_WIN32)
      # turn off whole program optimization:
      # 1. it creates object files that cdac-build-tool can't read
      # 2. we never link INTERMEDIARY_LIBRARY into the final product - it's only job is to be scraped
      set_target_properties(${INTERMEDIARY_LIBRARY} PROPERTIES
          INTERPROCEDURAL_OPTIMIZATION_RELEASE OFF
          INTERPROCEDURAL_OPTIMIZATION_RELWITHDEBINFO OFF)
    endif()

    target_include_directories(${INTERMEDIARY_LIBRARY} BEFORE PRIVATE ${VM_DIR})
    target_include_directories(${INTERMEDIARY_LIBRARY} BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
    target_include_directories(${INTERMEDIARY_LIBRARY} PRIVATE ${CLR_DIR}/interop/inc)

    set(GENERATED_CDAC_DESCRIPTOR_DIR "${CMAKE_CURRENT_BINARY_DIR}/cdac")
    set(CONTRACT_DESCRIPTOR_OUTPUT "${GENERATED_CDAC_DESCRIPTOR_DIR}/contract-descriptor.c")
    if(NOT EXISTS "${CDAC_BUILD_TOOL_BINARY_PATH}")
      message(FATAL_ERROR "${CDAC_BUILD_TOOL_BINARY_PATH} does not exist")
    endif()
    set(CONTRACT_DESCRIPTOR_INPUT "${CMAKE_CURRENT_SOURCE_DIR}/contract-descriptor.c.in")

    set(CONTRACT_BASELINE_DIR "${CLR_REPO_ROOT_DIR}/docs/design/datacontracts/data")
    set(CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc")

    # generate the contract descriptor by running cdac-build-tool
    # n.b. this just uses `dotnet` from the PATH.  InitializeDotNetCli adds the apropropriate directory
    add_custom_command(
      OUTPUT "${CONTRACT_DESCRIPTOR_OUTPUT}"
      VERBATIM
      COMMAND ${CLR_DOTNET_HOST_PATH} ${CDAC_BUILD_TOOL_BINARY_PATH} compose -i "${CONTRACT_DESCRIPTOR_INPUT}" -o "${CONTRACT_DESCRIPTOR_OUTPUT}" -b "${CONTRACT_BASELINE_DIR}" -c "${CONTRACT_FILE}" $<TARGET_OBJECTS:${INTERMEDIARY_LIBRARY}>
      DEPENDS ${INTERMEDIARY_LIBRARY} cee_wks_core $<TARGET_OBJECTS:${INTERMEDIARY_LIBRARY}> "${CONTRACT_FILE}" "${CONTRACT_DESCRIPTOR_INPUT}"
      USES_TERMINAL
    )

    # It is important that LIBRARY is an object library;
    # if it was static, linking it into the final dll would not export
    # DotNetRuntimeContractDescriptor since it is not referenced anywhere.
    add_library_clr(${LIBRARY} OBJECT
      "${CONTRACT_DESCRIPTOR_OUTPUT}"
      contractpointerdata.cpp
    )
    target_include_directories(${LIBRARY} BEFORE PRIVATE ${VM_DIR})
    target_include_directories(${LIBRARY} BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
    target_include_directories(${LIBRARY} PRIVATE ${CLR_DIR}/interop/inc)
    add_dependencies(${LIBRARY} ${INTERMEDIARY_LIBRARY} cee_wks_core)
  endif()
endfunction(generate_data_descriptors)
