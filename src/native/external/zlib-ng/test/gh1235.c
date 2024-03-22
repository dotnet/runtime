#include <assert.h>
#include <stdio.h>
#include <string.h>
#include "zutil.h"

int main(void) {
    unsigned char plain[32];
    unsigned char compressed[130];
    PREFIX3(stream) strm;
    z_size_t bound;
    z_size_t bytes;

    for (int i = 0; i <= 32; i++) {
        memset(plain, 6, i);
        memset(&strm, 0, sizeof(strm));
        PREFIX(deflateInit2)(&strm, 0, 8, 31, 1, Z_DEFAULT_STRATEGY);
        bound = PREFIX(deflateBound)(&strm, i);
        strm.next_in = plain;
        strm.next_out = compressed;
        strm.avail_in = i;
        strm.avail_out = sizeof(compressed);
        if (PREFIX(deflate)(&strm, Z_FINISH) != Z_STREAM_END) return -1;
        if (strm.avail_in != 0) return -1;
        printf("bytes = %2i, deflateBound = %2zi, total_out = %2zi\n", i, (size_t)bound, (size_t)strm.total_out);
        if (bound < strm.total_out) return -1;
        if (PREFIX(deflateEnd)(&strm) != Z_OK) return -1;
    }
    for (int i = 0; i <= 32; i++) {
        bytes = sizeof(compressed);
        for (int j = 0; j < i; j++) {
            plain[j] = j;
        }
        bound = PREFIX(compressBound)(i);
        if (PREFIX(compress2)(compressed, &bytes, plain, i, 1) != Z_OK) return -1;
        printf("bytes = %2i, compressBound = %2zi, total_out = %2zi\n", i, (size_t)bound, (size_t)bytes);
        if (bytes > bound) return -1;
    }
    return 0;
}
