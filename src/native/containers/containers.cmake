set (SHARED_CONTAINER_SOURCES "")
set (SHARED_CONTAINER_HEADERS "")

list(APPEND SHARED_CONTAINER_SOURCES
    dn-allocator.c
    dn-vector.c
    dn-fwd-list.c
    dn-list.c
    dn-queue.c
    dn-umap.c
)

list(APPEND SHARED_CONTAINER_HEADERS
    dn-allocator.h
    dn-vector.h
    dn-vector-t.h
    dn-vector-ptr.h
    dn-fwd-list.h
    dn-list.h
    dn-queue.h
    dn-umap.h
    dn-umap-t.h
    dn-sort-frag.inc
    dn-utils.h
)
