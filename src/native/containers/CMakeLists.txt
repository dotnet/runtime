add_library(dn-containers OBJECT)

target_sources(dn-containers PRIVATE
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

configure_file(dn-config.h.in ${CMAKE_CURRENT_BINARY_DIR}/dn-config.h)

target_include_directories(dn-containers PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

target_include_directories(dn-containers PRIVATE ${CMAKE_CURRENT_BINARY_DIR})
