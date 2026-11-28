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
  return()
endif()

if(NOT CLR_DOTNET_HOST_PATH)
  message(FATAL_ERROR "CLR_DOTNET_HOST_PATH is required to generate the eventing sources. Build with "
    "build.sh/build.cmd or build-runtime.sh/.cmd, which pass it, or pass -skipdotnet to build-runtime.sh/.cmd.")
endif()

# The Visual Studio environment sets Platform to the target architecture, which MSBuild would build the tool for.
set(EVENT_DATA_DOTNET "${CMAKE_COMMAND}" -E env --unset=Platform "${CLR_DOTNET_HOST_PATH}")

# Build the generator once, so the per-flavor generation steps (which may run in parallel) only run it.
set(EVENT_DATA_GENERATOR_TIMESTAMP ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/event_data_generator.timestamp)
add_custom_command(
  OUTPUT ${EVENT_DATA_GENERATOR_TIMESTAMP}
  COMMAND ${EVENT_DATA_DOTNET} build "${EVENT_DATA_GENERATOR}" -c Release
  COMMAND ${CMAKE_COMMAND} -E touch ${EVENT_DATA_GENERATOR_TIMESTAMP}
  DEPENDS ${EVENT_DATA_GENERATOR}
  COMMENT "Building the eventing source generator"
  VERBATIM)
add_custom_target(event_data_generator DEPENDS ${EVENT_DATA_GENERATOR_TIMESTAMP})

# add_event_data_target(<target> FLAVOR <coreclr|nativeaot> OUTPUT_DIR <dir> [INCLUSION_LIST <file>] OUTPUTS <var>)
# Adds <target>, which generates the eventing sources for FLAVOR into OUTPUT_DIR, and sets <var> to the generated files.
function(add_event_data_target TARGET)
  cmake_parse_arguments(ARG "" "FLAVOR;OUTPUT_DIR;INCLUSION_LIST;OUTPUTS" "" ${ARGN})

  # eventtracebase.h picks the ETW implementation by host, which includes the Windows-hosted cross-OS DAC.
  if(CLR_CMAKE_HOST_WIN32)
    set(provider etw)
  elseif(FEATURE_EVENTSOURCE_XPLAT AND ARG_FLAVOR STREQUAL "coreclr")
    set(provider lttng)
  else()
    set(provider none)
  endif()

  # The generated files are listed here rather than queried from the generator at configure time, which would cost a
  # `dotnet run` per flavor; keep them in sync with EventDataGenerator.cs (its --dry-run option prints what it writes).
  # There is one file set per provider in ClrEtwAll.man; NativeAOT's inclusion list leaves out DotNETRuntimeStress.
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

  set(command ${EVENT_DATA_DOTNET} run --file "${EVENT_DATA_GENERATOR}" -c Release --no-build --
    --flavor ${ARG_FLAVOR} --provider ${provider}
    --manifest "${EVENT_MANIFEST}" --exclusion-list "${EVENT_EXCLUSIONS}" --output "${ARG_OUTPUT_DIR}")
  if(ARG_INCLUSION_LIST)
    list(APPEND command --inclusion-list "${ARG_INCLUSION_LIST}")
  endif()

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
  add_dependencies(${TARGET} event_data_generator)
  set_source_files_properties(${outputs} PROPERTIES GENERATED TRUE)
  set(${ARG_OUTPUTS} ${outputs} PARENT_SCOPE)
endfunction()
