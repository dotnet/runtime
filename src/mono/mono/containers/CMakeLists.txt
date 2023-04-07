project(containers C)

### Paths and Targets for Shared Containers
set(SHARED_CONTAINERS_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/containers/")
include (${SHARED_CONTAINERS_SOURCE_PATH}containers.cmake)

set(shared_container_sources "")

list(APPEND shared_container_sources
  ${SHARED_CONTAINER_SOURCES}
  ${SHARED_CONTAINER_HEADERS}
)

addprefix(shared_container_api_sources ${SHARED_CONTAINERS_SOURCE_PATH} "${SHARED_CONTAINER_HEADERS}")

addprefix(shared_container_sources ${SHARED_CONTAINERS_SOURCE_PATH} "${shared_container_sources}")

set(rt_mono_container_api_sources
  dn-rt-mono.h)

set(rt_mono_container_sources
  ${rn_mono_container_api_sources}
  dn-rt-utils.c)


add_library(shared_container_api INTERFACE)
target_sources(shared_container_api INTERFACE ${shared_container_api_sources} ${rt_mono_container_api_sources})
target_include_directories(shared_container_api INTERFACE ${SHARED_CONTAINERS_SOURCE_PATH} .)

add_library(shared_container_objects OBJECT ${shared_container_sources} ${rt_mono_container_sources})
target_link_libraries(shared_container_objects PRIVATE eglib_api shared_container_api)
target_include_directories(shared_container_objects PUBLIC ${SHARED_CONTAINERS_SOURCE_PATH} .)
# to pick up config.h
target_include_directories(shared_container_objects PRIVATE
  ${PROJECT_BINARY_DIR}/../..)

