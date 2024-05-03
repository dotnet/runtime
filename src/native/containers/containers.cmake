set (SHARED_CONTAINER_SOURCES "")
set (SHARED_CONTAINER_HEADERS "")

list(APPEND SHARED_CONTAINER_SOURCES
    dn-allocator.c
    dn-fwd-list.c
    dn-list.c
    dn-queue.c
    dn-umap.c
    dn-vector.c
    # FIXME: Including these here causes a linker collision with sgen metadata
    # dn-simdhash.c
    # dn-simdhash-string-ptr.c
    # dn-simdhash-u32-ptr.c
    # dn-simdhash-ptr-ptr.c
    # dn-simdhash-ght-compatible.c
    # dn-simdhash-ptrpair-ptr.c
)

list(APPEND SHARED_CONTAINER_HEADERS
    dn-allocator.h
    dn-fwd-list.h
    dn-list.h
    dn-queue.h
    dn-sort-frag.inc
    dn-umap.h
    dn-umap-t.h
    dn-utils.h
    dn-vector.h
    dn-vector-priv.h
    dn-vector-ptr.h
    dn-vector-t.h
    dn-vector-types.h
    dn-simdhash.h
    dn-simdhash-specialization.h
    dn-simdhash-specialization-declarations.h
    dn-simdhash-specializations.h
    dn-simdhash-arch.h
    dn-simdhash-string-ptr.h
    dn-simdhash-utils.h
)
