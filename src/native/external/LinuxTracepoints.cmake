# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

include_directories(${CMAKE_CURRENT_LIST_DIR}/LinuxTracepoints/libtracepoint/include)
include_directories(${CMAKE_CURRENT_LIST_DIR}/LinuxTracepoints/libeventheader-tracepoint/include)

set(LinuxTracepoints_sources_base
	libtracepoint/src/tracepoint.c
	libeventheader-tracepoint/src/eventheader-tracepoint.c)


addprefix(LinuxTracepoints_sources "${CMAKE_CURRENT_LIST_DIR}/LinuxTracepoints" "${LinuxTracepoints_sources_base}")