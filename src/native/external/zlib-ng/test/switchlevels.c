/* Compresses a user-specified number of chunks from stdin into stdout as a single gzip stream.
 * Each chunk is compressed with a user-specified level.
 */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#include <stdio.h>

#if defined(_WIN32) || defined(__CYGWIN__)
#  include <fcntl.h>
#  include <io.h>
#  define SET_BINARY_MODE(file) setmode(fileno(file), O_BINARY)
#else
#  define SET_BINARY_MODE(file)
#endif

static int read_all(unsigned char *buf, size_t size) {
    size_t total_read = 0;
    while (total_read < size) {
        size_t n_read = fread(buf + total_read, 1, size - total_read, stdin);
        if (ferror(stdin)) {
            perror("fread\n");
            return 1;
        }
        if (n_read == 0) {
            fprintf(stderr, "Premature EOF\n");
            return 1;
        }
        total_read += n_read;
    }
    return 0;
}

static int write_all(unsigned char *buf, size_t size) {
    size_t total_written = 0;
    while (total_written < size) {
        size_t n_written = fwrite(buf + total_written, 1, size - total_written, stdout);
        if (ferror(stdout)) {
            perror("fwrite\n");
            return 1;
        }
        total_written += n_written;
    }
    return 0;
}

static int compress_chunk(PREFIX3(stream) *strm, int level, int size, int last) {
    int ret = 1;
    int err = 0;
    unsigned long compsize;
    unsigned char *buf;

    if (size <= 0) {
        fprintf(stderr, "compress_chunk() invalid size %d\n", size);
        goto done;
    }
    if (level < 0 || level > 9) {
        fprintf(stderr, "compress_chunk() invalid level %d\n", level);
        goto done;
    }

    compsize = PREFIX(deflateBound)(strm, size);
    buf = malloc(size + compsize);
    if (buf == NULL) {
        fprintf(stderr, "Out of memory\n");
        goto done;
    }
    if (read_all(buf, size) != 0) {
        goto free_buf;
    }

    /* Provide only output buffer to deflateParams(). It might need some space to flush the leftovers from the last
     * deflate(), but we don't want it to compress anything new. */
    strm->next_in = NULL;
    strm->avail_in = 0;
    strm->next_out = buf + size;
    strm->avail_out = compsize;
    err = PREFIX(deflateParams)(strm, level, Z_DEFAULT_STRATEGY);
    if (err != Z_OK) {
        fprintf(stderr, "deflateParams() failed with code %d\n", err);
        goto free_buf;
    }

    /* Provide input buffer to deflate(). */
    strm->next_in = buf;
    strm->avail_in = size;
    err = PREFIX(deflate)(strm, last ? Z_FINISH : Z_SYNC_FLUSH);
    if ((!last && err != Z_OK) || (last && err != Z_STREAM_END)) {
        fprintf(stderr, "deflate() failed with code %d\n", err);
        goto free_buf;
    }
    if (strm->avail_in != 0) {
        fprintf(stderr, "deflate() did not consume %d bytes of input\n", strm->avail_in);
        goto free_buf;
    }
    if (write_all(buf + size, compsize - strm->avail_out) != 0) {
        goto free_buf;
    }
    ret = 0;

free_buf:
    free(buf);
done:
    return ret;
}

void show_help(void)
{
    printf("Usage: switchlevels [-w bits] level1 size1 [level2 size2 ...]\n\n"
           "  -w : window bits (8 to 15 for gzip, -8 to -15 for zlib)\n\n");
}

int main(int argc, char **argv) {
    int ret = EXIT_FAILURE;
    int err = 0;
    int size = 0;
    int level = Z_DEFAULT_COMPRESSION;
    int level_arg = 1;
    int window_bits = MAX_WBITS + 16;
    PREFIX3(stream) strm;


    if ((argc == 1) || (argc == 2 && strcmp(argv[1], "--help") == 0)) {
        show_help();
        return 0;
    }

    SET_BINARY_MODE(stdin);
    SET_BINARY_MODE(stdout);

    memset(&strm, 0, sizeof(strm));

    for (int i = 1; i < argc - 1; i++) {
        if (strcmp(argv[i], "-w") == 0 && i+1 < argc) {
            window_bits = atoi(argv[++i]);
        } else {
            level_arg = i;
            level = atoi(argv[i]);
            break;
        }
    }

    err = PREFIX(deflateInit2)(&strm, level, Z_DEFLATED, window_bits, 8, Z_DEFAULT_STRATEGY);
    if (err != Z_OK) {
        fprintf(stderr, "deflateInit() failed with code %d\n", err);
        goto done;
    }

    for (int i = level_arg; i < argc - 1; i += 2) {
        level = atoi(argv[i]);
        size = atoi(argv[i + 1]);
        if (compress_chunk(&strm, level, size, i + 2 >= argc - 1) != 0) {
            goto deflate_end;
        }
    }
    ret = EXIT_SUCCESS;

deflate_end:
    PREFIX(deflateEnd)(&strm);
done:
    return ret;
}
