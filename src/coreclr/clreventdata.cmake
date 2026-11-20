# Native eventing sources (FireEtw* entry points, EventPipe writers, LTTng and ETW providers) are
# generated from ClrEtwAll.man by scripts/EventDataGenerator.cs, a file-based app run with the build's .NET host.

set(EVENT_DATA_GENERATOR ${CMAKE_CURRENT_LIST_DIR}/scripts/EventDataGenerator.cs)
set(EVENT_MANIFEST ${CMAKE_CURRENT_LIST_DIR}/vm/ClrEtwAll.man)
set(EVENT_EXCLUSIONS ${CMAKE_CURRENT_LIST_DIR}/vm/ClrEtwAllMeta.lst)

if(CLR_CMAKE_SKIP_DOTNET)
  # build-runtime.sh/.cmd -skipdotnet: CoreCLR is built without eventing and NativeAOT ships its disabled
  # EventPipe as both eventpipe-enabled and eventpipe-disabled.
  message(STATUS "Building without eventing (-skipdotnet)")
  set(FEATURE_EVENT_TRACE 0)
  set(FEATURE_PERFTRACING 0)
  set(FEATURE_EVENTSOURCE_XPLAT 0)
elseif(NOT CLR_DOTNET_HOST_PATH)
  message(FATAL_ERROR "CLR_DOTNET_HOST_PATH is required to generate the eventing sources. Build with "
    "build.sh/build.cmd or build-runtime.sh/.cmd, which pass it, or pass -skipdotnet to build-runtime.sh/.cmd.")
endif()

function(_get_event_data_provider FLAVOR PROVIDER_VAR)
  # eventtracebase.h picks the ETW implementation by host, which includes the Windows-hosted cross-OS DAC.
  if(CLR_CMAKE_HOST_WIN32)
    set(${PROVIDER_VAR} etw PARENT_SCOPE)
  elseif(FEATURE_EVENTSOURCE_XPLAT AND FLAVOR STREQUAL "coreclr")
    set(${PROVIDER_VAR} lttng PARENT_SCOPE)
  else()
    set(${PROVIDER_VAR} none PARENT_SCOPE)
  endif()
endfunction()

function(_get_event_data_command FLAVOR OUTPUT_DIR INCLUSION_LIST COMMAND_VAR)
  _get_event_data_provider(${FLAVOR} provider)

  # The Visual Studio environment sets Platform to the target architecture, which MSBuild would build the tool for.
  set(command "${CMAKE_COMMAND}" -E env --unset=Platform
    "${CLR_DOTNET_HOST_PATH}" run --file "${EVENT_DATA_GENERATOR}" -c Release --
    --flavor ${FLAVOR} --provider ${provider}
    --manifest "${EVENT_MANIFEST}" --exclusion-list "${EVENT_EXCLUSIONS}" --output "${OUTPUT_DIR}")
  if(INCLUSION_LIST)
    list(APPEND command --inclusion-list "${INCLUSION_LIST}")
  endif()
  set(${COMMAND_VAR} ${command} PARENT_SCOPE)
endfunction()

# get_event_data_outputs(FLAVOR <coreclr|nativeaot> OUTPUT_DIR <dir> OUTPUTS <var>)
# Sets <var> to the files generated for FLAVOR into OUTPUT_DIR. The list is fixed here rather than queried from the
# generator at configure time, which would cost a `dotnet run` per flavor; keep it in sync with EventDataGenerator.cs
# (its --dry-run option prints what it writes).
function(get_event_data_outputs)
  cmake_parse_arguments(ARG "" "FLAVOR;OUTPUT_DIR;OUTPUTS" "" ${ARGN})
  _get_event_data_provider(${ARG_FLAVOR} provider)

  # One file set per provider in ClrEtwAll.man; NativeAOT's inclusion list leaves out DotNETRuntimeStress.
  set(providers dotnetruntime dotnetruntimerundown dotnetruntimeprivate)
  if(ARG_FLAVOR STREQUAL "coreclr")
    list(APPEND providers dotnetruntimestress)
  endif()

  set(files etmdummy.h clretwallmain.h clreventpipewriteevents.h eventpipe/eventpipehelpers.cpp)
  if(ARG_FLAVOR STREQUAL "nativeaot")
    list(APPEND files clretwallmain.cpp disabledclretwallmain.cpp)
  elseif(NOT provider STREQUAL "etw")
    list(APPEND files clrproviders.h)
  endif()
  if(ARG_FLAVOR STREQUAL "coreclr" OR provider STREQUAL "etw")
    list(APPEND files clrxplatevents.h)
  endif()
  foreach(name IN LISTS providers)
    list(APPEND files eventpipe/${name}.cpp)
    if(provider STREQUAL "lttng")
      list(APPEND files lttng/tp${name}.h lttng/eventprov${name}.cpp lttng/traceptprov${name}.cpp)
    endif()
  endforeach()
  if(provider STREQUAL "etw")
    list(APPEND files etw/ClrEtwAll.h etw/ClrEtwAll.rc etw/ClrEtwAll_MSG00001.bin etw/ClrEtwAllTEMP.bin etw/etwmacros.h)
  endif()

  list(TRANSFORM files PREPEND "${ARG_OUTPUT_DIR}/" OUTPUT_VARIABLE outputs)
  set_source_files_properties(${outputs} PROPERTIES GENERATED TRUE)
  set(${ARG_OUTPUTS} ${outputs} PARENT_SCOPE)
endfunction()

# add_event_data_target(<target> FLAVOR <coreclr|nativeaot> OUTPUT_DIR <dir> [INCLUSION_LIST <file>] OUTPUTS <var>)
# Adds <target>, which generates the eventing sources for FLAVOR into OUTPUT_DIR, and sets <var> to the generated files.
function(add_event_data_target TARGET)
  cmake_parse_arguments(ARG "" "FLAVOR;OUTPUT_DIR;INCLUSION_LIST;OUTPUTS" "" ${ARGN})

  get_event_data_outputs(FLAVOR ${ARG_FLAVOR} OUTPUT_DIR "${ARG_OUTPUT_DIR}" OUTPUTS outputs)
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
