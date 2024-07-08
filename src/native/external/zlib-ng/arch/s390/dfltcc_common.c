/* dfltcc_deflate.c - IBM Z DEFLATE CONVERSION CALL general support. */

#include "zbuild.h"
#include "dfltcc_common.h"
#include "dfltcc_detail.h"

/*
   Memory management.

   DFLTCC requires parameter blocks and window to be aligned. zlib-ng allows
   users to specify their own allocation functions, so using e.g.
   `posix_memalign' is not an option. Thus, we overallocate and take the
   aligned portion of the buffer.
*/

static const int PAGE_ALIGN = 0x1000;

void Z_INTERNAL *PREFIX(dfltcc_alloc_window)(PREFIX3(streamp) strm, uInt items, uInt size) {
    void *p;
    void *w;

    /* To simplify freeing, we store the pointer to the allocated buffer right
     * before the window. Note that DFLTCC always uses HB_SIZE bytes.
     */
    p = ZALLOC(strm, sizeof(void *) + MAX(items * size, HB_SIZE) + PAGE_ALIGN, sizeof(unsigned char));
    if (p == NULL)
        return NULL;
    w = ALIGN_UP((char *)p + sizeof(void *), PAGE_ALIGN);
    *(void **)((char *)w - sizeof(void *)) = p;
    return w;
}

void Z_INTERNAL PREFIX(dfltcc_copy_window)(void *dest, const void *src, size_t n) {
    memcpy(dest, src, MAX(n, HB_SIZE));
}

void Z_INTERNAL PREFIX(dfltcc_free_window)(PREFIX3(streamp) strm, void *w) {
    if (w)
        ZFREE(strm, *(void **)((unsigned char *)w - sizeof(void *)));
}
