set(LTTNG_PROVIDER_SOURCES ${CORECLR_EVENT_DATA_OUTPUTS})
list(FILTER LTTNG_PROVIDER_SOURCES INCLUDE REGEX "/lttng/eventprov[a-z]+\\.cpp$")
set(TRACEPOINT_PROVIDER_SOURCES ${CORECLR_EVENT_DATA_OUTPUTS})
list(FILTER TRACEPOINT_PROVIDER_SOURCES INCLUDE REGEX "/lttng/traceptprov[a-z]+\\.cpp$")

include_directories(${GENERATED_INCLUDE_DIR}/lttng)

add_library(eventprovider
  STATIC
  ${LTTNG_PROVIDER_SOURCES}
  eventproviderhelpers.cpp
)

add_library_clr(coreclrtraceptprovider
  SHARED
  ${TRACEPOINT_PROVIDER_SOURCES}
)

# Loads coreclrtraceptprovider into the process that hosts the runtime.
add_library(tracepointprovider OBJECT tracepointprovider.cpp)

add_dependencies(eventprovider eventing_headers)
add_dependencies(coreclrtraceptprovider eventing_headers)

find_library(LTTNG NAMES lttng-ust)

# --noinhibit-exec works around error produced by lld from LLVM 16
# that is caused by invalid symbols in the lttng we are linking against:
# ld.lld: error: liblttng-ust.so: invalid local symbol '__stop___tracepoints_ptrs' in global part of symbol table
# see https://github.com/llvm/llvm-project/commit/3ac94280245415be66cb1b603367c5f4f6d498e7
target_link_libraries(coreclrtraceptprovider
                      PRIVATE
                      ${LTTNG}
                      -Wl,--noinhibit-exec
)

set_target_properties(coreclrtraceptprovider PROPERTIES LINKER_LANGUAGE CXX)

# Install the static eventprovider library
install_clr(TARGETS eventprovider DESTINATIONS lib COMPONENT runtime)
install_clr(TARGETS coreclrtraceptprovider DESTINATIONS . sharedFramework COMPONENT runtime)
