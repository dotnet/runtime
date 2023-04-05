project(containers C)

add_library(shared_container_objects OBJECT ${shared_container_sources})
target_include_directories(shared_container_objects PUBLIC ${SHARED_CONTAINERS_SOURCE_PATH})

