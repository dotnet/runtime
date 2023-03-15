set (SHARED_CONTAINER_SOURCES "")
set (SHARED_CONTAINER_HEADERS "")

list(APPEND SHARED_CONTAINER_SOURCES
    dn-allocator.c
    dn-fwd-list.c
    dn-list.c
    dn-queue.c
    dn-umap.c
    dn-vector.c
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
)
