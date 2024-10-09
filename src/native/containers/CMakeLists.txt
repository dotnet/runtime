set(SOURCES
    dn-allocator.c
    dn-fwd-list.c
    dn-list.c
    dn-queue.c
    dn-umap.c
    dn-vector.c
    dn-simdhash.c
    dn-simdhash-string-ptr.c
    dn-simdhash-u32-ptr.c
    dn-simdhash-ptr-ptr.c
    dn-simdhash-ght-compatible.c
    dn-simdhash-ptrpair-ptr.c
    dn-simdhash-utils.c
)

# Build two variants of the container library as some of our scenarios
# are not compatible with LTO, (i.e. NativeAOT).
add_library(dn-containers OBJECT ${SOURCES})
add_library(dn-containers-no-lto OBJECT ${SOURCES})

configure_file(dn-config.h.in ${CMAKE_CURRENT_BINARY_DIR}/dn-config.h)

target_include_directories(dn-containers PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(dn-containers PRIVATE ${CMAKE_CURRENT_BINARY_DIR})
set_target_properties(dn-containers PROPERTIES CLR_CONTROL_FLOW_GUARD ON)

target_include_directories(dn-containers-no-lto PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(dn-containers-no-lto PRIVATE ${CMAKE_CURRENT_BINARY_DIR})
set_target_properties(dn-containers-no-lto
    PROPERTIES
    CLR_CONTROL_FLOW_GUARD ON
    INTERPROCEDURAL_OPTIMIZATION OFF)
