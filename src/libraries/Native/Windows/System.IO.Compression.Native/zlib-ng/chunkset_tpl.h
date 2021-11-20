/* chunkset_tpl.h -- inline functions to copy small data chunks.
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/* Returns the chunk size */
Z_INTERNAL uint32_t CHUNKSIZE(void) {
    return sizeof(chunk_t);
}

/* Behave like memcpy, but assume that it's OK to overwrite at least
   chunk_t bytes of output even if the length is shorter than this,
   that the length is non-zero, and that `from` lags `out` by at least
   sizeof chunk_t bytes (or that they don't overlap at all or simply that
   the distance is less than the length of the copy).

   Aside from better memory bus utilisation, this means that short copies
   (chunk_t bytes or fewer) will fall straight through the loop
   without iteration, which will hopefully make the branch prediction more
   reliable. */
Z_INTERNAL uint8_t* CHUNKCOPY(uint8_t *out, uint8_t const *from, unsigned len) {
    Assert(len > 0, "chunkcopy should never have a length 0");
    chunk_t chunk;
    int32_t align = (--len % sizeof(chunk_t)) + 1;
    loadchunk(from, &chunk);
    storechunk(out, &chunk);
    out += align;
    from += align;
    len /= sizeof(chunk_t);
    while (len > 0) {
        loadchunk(from, &chunk);
        storechunk(out, &chunk);
        out += sizeof(chunk_t);
        from += sizeof(chunk_t);
        --len;
    }
    return out;
}

/* Behave like chunkcopy, but avoid writing beyond of legal output. */
Z_INTERNAL uint8_t* CHUNKCOPY_SAFE(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe) {
    unsigned safelen = (unsigned)((safe - out) + 1);
    len = MIN(len, safelen);
#if CHUNK_SIZE >= 32
    while (len >= 32) {
        memcpy(out, from, 32);
        out += 32;
        from += 32;
        len -= 32;
    }
#endif
#if CHUNK_SIZE >= 16
    while (len >= 16) {
        memcpy(out, from, 16);
        out += 16;
        from += 16;
        len -= 16;
    }
#endif
#if CHUNK_SIZE >= 8
    while (len >= 8) {
        memcpy(out, from, 8);
        out += 8;
        from += 8;
        len -= 8;
    }
#endif
    if (len >= 4) {
        memcpy(out, from, 4);
        out += 4;
        from += 4;
        len -= 4;
    }
    if (len >= 2) {
        memcpy(out, from, 2);
        out += 2;
        from += 2;
        len -= 2;
    }
    if (len == 1) {
        *out++ = *from++;
    }
    return out;
}

/* Perform short copies until distance can be rewritten as being at least
   sizeof chunk_t.

   This assumes that it's OK to overwrite at least the first
   2*sizeof(chunk_t) bytes of output even if the copy is shorter than this.
   This assumption holds because inflate_fast() starts every iteration with at
   least 258 bytes of output space available (258 being the maximum length
   output from a single token; see inflate_fast()'s assumptions below). */
Z_INTERNAL uint8_t* CHUNKUNROLL(uint8_t *out, unsigned *dist, unsigned *len) {
    unsigned char const *from = out - *dist;
    chunk_t chunk;
    while (*dist < *len && *dist < sizeof(chunk_t)) {
        loadchunk(from, &chunk);
        storechunk(out, &chunk);
        out += *dist;
        *len -= *dist;
        *dist += *dist;
    }
    return out;
}

/* Copy DIST bytes from OUT - DIST into OUT + DIST * k, for 0 <= k < LEN/DIST.
   Return OUT + LEN. */
Z_INTERNAL uint8_t* CHUNKMEMSET(uint8_t *out, unsigned dist, unsigned len) {
    /* Debug performance related issues when len < sizeof(uint64_t):
       Assert(len >= sizeof(uint64_t), "chunkmemset should be called on larger chunks"); */
    Assert(dist > 0, "chunkmemset cannot have a distance 0");

    unsigned char *from = out - dist;
    chunk_t chunk;
    unsigned sz = sizeof(chunk);
    if (len < sz) {
        do {
            *out++ = *from++;
            --len;
        } while (len != 0);
        return out;
    }

#ifdef HAVE_CHUNKMEMSET_1
    if (dist == 1) {
        chunkmemset_1(from, &chunk);
    } else
#endif
#ifdef HAVE_CHUNKMEMSET_2
    if (dist == 2) {
        chunkmemset_2(from, &chunk);
    } else
#endif
#ifdef HAVE_CHUNKMEMSET_4
    if (dist == 4) {
        chunkmemset_4(from, &chunk);
    } else
#endif
#ifdef HAVE_CHUNKMEMSET_8
    if (dist == 8) {
        chunkmemset_8(from, &chunk);
    } else
#endif
    if (dist == sz) {
        loadchunk(from, &chunk);
    } else if (dist < sz) {
        unsigned char *end = out + len - 1;
        while (len > dist) {
            out = CHUNKCOPY_SAFE(out, from, dist, end);
            len -= dist;
        }
        if (len > 0) {
            out = CHUNKCOPY_SAFE(out, from, len, end);
        }
        return out;
    } else {
        out = CHUNKUNROLL(out, &dist, &len);
        return CHUNKCOPY(out, out - dist, len);
    }

    unsigned rem = len % sz;
    len -= rem;
    while (len) {
        storechunk(out, &chunk);
        out += sz;
        len -= sz;
    }

    /* Last, deal with the case when LEN is not a multiple of SZ. */
    if (rem) {
        memcpy(out, from, rem);
        out += rem;
    }

    return out;
}

Z_INTERNAL uint8_t* CHUNKMEMSET_SAFE(uint8_t *out, unsigned dist, unsigned len, unsigned left) {
    len = MIN(len, left);
    if (left < (unsigned)(3 * sizeof(chunk_t))) {
        uint8_t *from = out - dist;
        while (len > 0) {
            *out++ = *from++;
            --len;
        }
        return out;
    }
    return CHUNKMEMSET(out, dist, len);
}
