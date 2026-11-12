# Native eventing sources (FireEtw* entry points, EventPipe writers, LTTng and ETW providers) are
# generated from ClrEtwAll.man by scripts/EventDataGenerator.cs, a file-based app run with the build's .NET host.

set(EVENT_DATA_GENERATOR ${CMAKE_CURRENT_LIST_DIR}/scripts/EventDataGenerator.cs)
set(EVENT_MANIFEST ${CMAKE_CURRENT_LIST_DIR}/vm/ClrEtwAll.man)
set(EVENT_EXCLUSIONS ${CMAKE_CURRENT_LIST_DIR}/vm/ClrEtwAllMeta.lst)

if(NOT CLR_DOTNET_HOST_PATH)
  # build-runtime.sh/.cmd invoked directly does not pass the host, so the eventing sources cannot be generated.
  # CoreCLR is built without eventing and NativeAOT ships its disabled EventPipe as both eventpipe-enabled
  # and eventpipe-disabled.
  message(STATUS "CLR_DOTNET_HOST_PATH is not set; building without eventing")
  set(FEATURE_EVENT_TRACE 0)
  set(FEATURE_PERFTRACING 0)
  set(FEATURE_EVENTSOURCE_XPLAT 0)
endif()

function(_get_event_data_command FLAVOR OUTPUT_DIR INCLUSION_LIST COMMAND_VAR)
  if(CLR_CMAKE_TARGET_WIN32)
    set(provider etw)
  elseif(FEATURE_EVENTSOURCE_XPLAT AND FLAVOR STREQUAL "coreclr")
    set(provider lttng)
  else()
    set(provider none)
  endif()

  set(command "${CLR_DOTNET_HOST_PATH}" run --file "${EVENT_DATA_GENERATOR}" -c Release --
    --flavor ${FLAVOR} --provider ${provider}
    --manifest "${EVENT_MANIFEST}" --exclusion-list "${EVENT_EXCLUSIONS}" --output "${OUTPUT_DIR}")
  if(INCLUSION_LIST)
    list(APPEND command --inclusion-list "${INCLUSION_LIST}")
  endif()
  set(${COMMAND_VAR} ${command} PARENT_SCOPE)
endfunction()

# get_event_data_outputs(FLAVOR <coreclr|nativeaot> OUTPUT_DIR <dir> [INCLUSION_LIST <file>] OUTPUTS <var>)
# Sets <var> to the files generated for FLAVOR into OUTPUT_DIR.
function(get_event_data_outputs)
  cmake_parse_arguments(ARG "" "FLAVOR;OUTPUT_DIR;INCLUSION_LIST;OUTPUTS" "" ${ARGN})

  string(MAKE_C_IDENTIFIER "EVENT_DATA_OUTPUTS_${ARG_FLAVOR}_${ARG_OUTPUT_DIR}" cache_property)
  get_property(outputs GLOBAL PROPERTY ${cache_property})
  if(NOT outputs)
    _get_event_data_command(${ARG_FLAVOR} "${ARG_OUTPUT_DIR}" "${ARG_INCLUSION_LIST}" command)
    execute_process(
      COMMAND ${command} --dry-run
      RESULT_VARIABLE result
      OUTPUT_VARIABLE dry_run_output
      ERROR_VARIABLE dry_run_errors)
    if(NOT result EQUAL 0)
      message(FATAL_ERROR "Failed to run ${EVENT_DATA_GENERATOR}: ${dry_run_output}${dry_run_errors}")
    endif()

    # Only keep the listed paths; the first run may also print build output.
    string(REPLACE "\n" ";" lines "${dry_run_output}")
    set(outputs "")
    foreach(line IN LISTS lines)
      string(STRIP "${line}" line)
      string(FIND "${line}" "${ARG_OUTPUT_DIR}/" position)
      if(position EQUAL 0)
        list(APPEND outputs "${line}")
      endif()
    endforeach()

    set_property(GLOBAL PROPERTY ${cache_property} "${outputs}")
  endif()

  set_source_files_properties(${outputs} PROPERTIES GENERATED TRUE)
  set(${ARG_OUTPUTS} ${outputs} PARENT_SCOPE)
endfunction()

# add_event_data_target(<target> FLAVOR <coreclr|nativeaot> OUTPUT_DIR <dir> [INCLUSION_LIST <file>] OUTPUTS <var>)
# Adds <target>, which generates the eventing sources for FLAVOR into OUTPUT_DIR, and sets <var> to the generated files.
function(add_event_data_target TARGET)
  cmake_parse_arguments(ARG "" "FLAVOR;OUTPUT_DIR;INCLUSION_LIST;OUTPUTS" "" ${ARGN})

  get_event_data_outputs(FLAVOR ${ARG_FLAVOR} OUTPUT_DIR "${ARG_OUTPUT_DIR}" INCLUSION_LIST "${ARG_INCLUSION_LIST}" OUTPUTS outputs)
  _get_event_data_command(${ARG_FLAVOR} "${ARG_OUTPUT_DIR}" "${ARG_INCLUSION_LIST}" command)

  # Unchanged files are not rewritten, so track the run with a timestamp to avoid rerunning it on every build.
  set(timestamp ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/${TARGET}.timestamp)
  add_custom_command(
    OUTPUT ${timestamp}
    BYPRODUCTS ${outputs}
    COMMAND ${command}
    COMMAND ${CMAKE_COMMAND} -E touch ${timestamp}
    DEPENDS ${EVENT_DATA_GENERATOR} ${EVENT_MANIFEST} ${EVENT_EXCLUSIONS} ${ARG_INCLUSION_LIST}
    COMMENT "Generating ${ARG_FLAVOR} eventing sources"
    VERBATIM)

  add_custom_target(${TARGET} DEPENDS ${timestamp})
  set(${ARG_OUTPUTS} ${outputs} PARENT_SCOPE)
endfunction()
