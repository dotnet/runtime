/* minideflate.c -- test deflate/inflate under specific conditions
 * Copyright (C) 2020 Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include "zbuild.h"

#include <stdio.h>
#include <assert.h>

#include "zutil.h"

#if defined(_WIN32) || defined(__CYGWIN__)
#  include <fcntl.h>
#  include <io.h>
#  define SET_BINARY_MODE(file) setmode(fileno(file), O_BINARY)
#else
#  define SET_BINARY_MODE(file)
#endif

#ifdef _MSC_VER
#  include <string.h>
#  define strcasecmp _stricmp
#else
#  include <strings.h>
#endif

#define CHECK_ERR(err, msg) { \
    if (err != Z_OK) { \
        fprintf(stderr, "%s error: %d\n", msg, err); \
        exit(1); \
    } \
}

/* Default read/write i/o buffer size based on GZBUFSIZE */
#define BUFSIZE 131072

/* ===========================================================================
 * deflate() using specialized parameters
 */
static void deflate_params(FILE *fin, FILE *fout, int32_t read_buf_size, int32_t write_buf_size, int32_t level,
    int32_t window_bits, int32_t mem_level, int32_t strategy, int32_t flush) {
    PREFIX3(stream) c_stream; /* compression stream */
    uint8_t *read_buf;
    uint8_t *write_buf;
    int32_t read;
    int err;

    read_buf = (uint8_t *)malloc(read_buf_size);
    if (read_buf == NULL) {
        fprintf(stderr, "failed to create read buffer (%d)\n", read_buf_size);
        return;
    }
    write_buf = (uint8_t *)malloc(write_buf_size);
    if (write_buf == NULL) {
        fprintf(stderr, "failed to create write buffer (%d)\n", write_buf_size);
        free(read_buf);
        return;
    }

    c_stream.zalloc = NULL;
    c_stream.zfree = NULL;
    c_stream.opaque = (void *)0;
    c_stream.total_in = 0;
    c_stream.total_out = 0;
    c_stream.next_out = write_buf;
    c_stream.avail_out = write_buf_size;

    err = PREFIX(deflateInit2)(&c_stream, level, Z_DEFLATED, window_bits, mem_level, strategy);
    CHECK_ERR(err, "deflateInit2");

    /* Process input using our read buffer and flush type,
     * output to stdout only once write buffer is full */
    do {
        read = (int32_t)fread(read_buf, 1, read_buf_size, fin);
        if (read <= 0)
            break;

        c_stream.next_in  = (z_const uint8_t *)read_buf;
        c_stream.avail_in = read;

        do {
            err = PREFIX(deflate)(&c_stream, flush);
            if (err == Z_STREAM_END) break;
            CHECK_ERR(err, "deflate");

            if (c_stream.next_out == write_buf + write_buf_size) {
                fwrite(write_buf, 1, write_buf_size, fout);
                c_stream.next_out = write_buf;
                c_stream.avail_out = write_buf_size;
            }
        } while (c_stream.next_in < read_buf + read);
    } while (err == Z_OK);

    /* Finish the stream if necessary */
    if (flush != Z_FINISH) {
        c_stream.avail_in = 0;
        do {
            if (c_stream.next_out == write_buf + write_buf_size) {
                fwrite(write_buf, 1, write_buf_size, fout);
                c_stream.next_out = write_buf;
                c_stream.avail_out = write_buf_size;
            }

            err = PREFIX(deflate)(&c_stream, Z_FINISH);
            if (err == Z_STREAM_END) break;
            CHECK_ERR(err, "deflate");
        } while (1);
    }

    /* Output remaining data in write buffer */
    if (c_stream.next_out != write_buf) {
        fwrite(write_buf, 1, c_stream.next_out - write_buf, fout);
    }

    err = PREFIX(deflateEnd)(&c_stream);
    CHECK_ERR(err, "deflateEnd");

    free(read_buf);
    free(write_buf);
}

/* ===========================================================================
 * inflate() using specialized parameters
 */
static void inflate_params(FILE *fin, FILE *fout, int32_t read_buf_size, int32_t write_buf_size, int32_t window_bits,
    int32_t flush) {
    PREFIX3(stream) d_stream; /* decompression stream */
    uint8_t *read_buf;
    uint8_t *write_buf;
    int32_t read;
    int err;


    read_buf = (uint8_t *)malloc(read_buf_size);
    if (read_buf == NULL) {
        fprintf(stderr, "failed to create read buffer (%d)\n", read_buf_size);
        return;
    }
    write_buf = (uint8_t *)malloc(write_buf_size);
    if (write_buf == NULL) {
        fprintf(stderr, "failed to create write buffer (%d)\n", write_buf_size);
        free(read_buf);
        return;
    }

    d_stream.zalloc = NULL;
    d_stream.zfree = NULL;
    d_stream.opaque = (void *)0;
    d_stream.total_in = 0;
    d_stream.total_out = 0;
    d_stream.next_out = write_buf;
    d_stream.avail_out = write_buf_size;

    err = PREFIX(inflateInit2)(&d_stream, window_bits);
    CHECK_ERR(err, "inflateInit2");

    /* Process input using our read buffer and flush type,
     * output to stdout only once write buffer is full */
    do {
        read = (int32_t)fread(read_buf, 1, read_buf_size, fin);
        if (read <= 0)
            break;

        d_stream.next_in  = (z_const uint8_t *)read_buf;
        d_stream.avail_in = read;

        do {
            err = PREFIX(inflate)(&d_stream, flush);

            /* Ignore Z_BUF_ERROR if we are finishing and read buffer size is
             * purposefully limited */
            if (flush == Z_FINISH && err == Z_BUF_ERROR && read_buf_size != BUFSIZE)
                err = Z_OK;

            if (err == Z_STREAM_END) break;
            CHECK_ERR(err, "inflate");

            if (d_stream.next_out == write_buf + write_buf_size) {
                fwrite(write_buf, 1, write_buf_size, fout);
                d_stream.next_out = write_buf;
                d_stream.avail_out = write_buf_size;
            }
        } while (d_stream.next_in < read_buf + read);
    } while (err == Z_OK);

    /* Finish the stream if necessary */
    if (flush != Z_FINISH) {
        d_stream.avail_in = 0;
        do {
            if (d_stream.next_out == write_buf + write_buf_size) {
                fwrite(write_buf, 1, write_buf_size, fout);
                d_stream.next_out = write_buf;
                d_stream.avail_out = write_buf_size;
            }

            err = PREFIX(inflate)(&d_stream, Z_FINISH);
            if (err == Z_STREAM_END) break;
            CHECK_ERR(err, "inflate");
        } while (1);
    }

    /* Output remaining data in write buffer */
    if (d_stream.next_out != write_buf) {
        fwrite(write_buf, 1, d_stream.next_out - write_buf, fout);
    }

    err = PREFIX(inflateEnd)(&d_stream);
    CHECK_ERR(err, "inflateEnd");

    free(read_buf);
    free(write_buf);
}

static void show_help(void) {
    printf("Usage: minideflate [-c][-d][-k] [-f|-h|-R|-F] [-m level] [-r/-t size] [-s flush] [-w bits] [-0 to -9] [input file]\n\n"
           "  -c : write to standard output\n"
           "  -d : decompress\n"
           "  -k : keep input file\n"
           "  -f : compress with Z_FILTERED\n"
           "  -h : compress with Z_HUFFMAN_ONLY\n"
           "  -R : compress with Z_RLE\n"
           "  -F : compress with Z_FIXED\n"
           "  -m : memory level (1 to 8)\n"
           "  -w : window bits..\n"
           "     :   -1 to -15 for raw deflate\n"
           "     :    0 to  15 for deflate (adler32)\n"
           "     :   16 to  31 for gzip (crc32)\n"
           "  -s : flush type (0 to 5)\n"
           "  -r : read buffer size\n"
           "  -t : write buffer size\n"
           "  -0 to -9 : compression level\n\n");
}

int main(int argc, char **argv) {
    int32_t i;
    int32_t mem_level = DEF_MEM_LEVEL;
    int32_t window_bits = INT32_MAX;
    int32_t strategy = Z_DEFAULT_STRATEGY;
    int32_t level = Z_DEFAULT_COMPRESSION;
    int32_t read_buf_size = BUFSIZE;
    int32_t write_buf_size = BUFSIZE;
    int32_t flush = Z_NO_FLUSH;
    uint8_t copyout = 0;
    uint8_t uncompr = 0;
    uint8_t keep = 0;
    FILE *fin = stdin;
    FILE *fout = stdout;


    if (argc == 1) {
        show_help();
        return 64;   /* EX_USAGE */
    }

    for (i = 1; i < argc; i++) {
        if ((strcmp(argv[i], "-m") == 0) && (i + 1 < argc))
            mem_level = atoi(argv[++i]);
        else if ((strcmp(argv[i], "-w") == 0) && (i + 1 < argc))
            window_bits = atoi(argv[++i]);
        else if ((strcmp(argv[i], "-r") == 0) && (i + 1 < argc))
            read_buf_size = atoi(argv[++i]);
        else if ((strcmp(argv[i], "-t") == 0) && (i + 1 < argc))
            write_buf_size = atoi(argv[++i]);
        else if ((strcmp(argv[i], "-s") == 0) && (i + 1 < argc))
            flush = atoi(argv[++i]);
        else if (strcmp(argv[i], "-c") == 0)
            copyout = 1;
        else if (strcmp(argv[i], "-d") == 0)
            uncompr = 1;
        else if (strcmp(argv[i], "-k") == 0)
            keep = 1;
        else if (strcmp(argv[i], "-f") == 0)
            strategy = Z_FILTERED;
        else if (strcmp(argv[i], "-F") == 0)
            strategy = Z_FIXED;
        else if (strcmp(argv[i], "-h") == 0)
            strategy = Z_HUFFMAN_ONLY;
        else if (strcmp(argv[i], "-R") == 0)
            strategy = Z_RLE;
        else if (argv[i][0] == '-' && argv[i][1] >= '0' && argv[i][1] <= '9' && argv[i][2] == 0)
            level = argv[i][1] - '0';
        else if (strcmp(argv[i], "--help") == 0) {
            show_help();
            return 0;
        } else if (argv[i][0] == '-') {
            show_help();
            return 64;   /* EX_USAGE */
        } else
            break;
    }

    SET_BINARY_MODE(stdin);
    SET_BINARY_MODE(stdout);

    if (i != argc) {
        fin = fopen(argv[i], "rb+");
        if (fin == NULL) {
            fprintf(stderr, "Failed to open file: %s\n", argv[i]);
            exit(1);
        }
        if (!copyout) {
            char *out_file = (char *)calloc(1, strlen(argv[i]) + 6);
            if (out_file == NULL) {
                fprintf(stderr, "Not enough memory\n");
                exit(1);
            }
            strcat(out_file, argv[i]);
            if (!uncompr) {
                if (window_bits < 0) {
                    strcat(out_file, ".zraw");
                } else if (window_bits > MAX_WBITS) {
                    strcat(out_file, ".gz");
                } else {
                    strcat(out_file, ".z");
                }
            } else {
                char *out_ext = strrchr(out_file, '.');
                if (out_ext != NULL) {
                    if (strcasecmp(out_ext, ".zraw") == 0 && window_bits == INT32_MAX) {
                        fprintf(stderr, "Must specify window bits for raw deflate stream\n");
                        exit(1);
                    }
                    *out_ext = 0;
                }
            }
            fout = fopen(out_file, "wb");
            if (fout == NULL) {
                fprintf(stderr, "Failed to open file: %s\n", out_file);
                exit(1);
            }
            free(out_file);
        }
    }

    if (window_bits == INT32_MAX) {
        window_bits = MAX_WBITS;
        /* Auto-detect wrapper for inflateInit */
        if (uncompr)
            window_bits += 32;
    }

    if (window_bits == INT32_MAX) {
        window_bits = MAX_WBITS;
        /* Auto-detect wrapper for inflateInit */
        if (uncompr)
            window_bits += 32;
    }

    if (uncompr) {
        inflate_params(fin, fout, read_buf_size, write_buf_size, window_bits, flush);
    } else {
        deflate_params(fin, fout, read_buf_size, write_buf_size, level, window_bits, mem_level, strategy, flush);
    }

    if (fin != stdin) {
        fclose(fin);
        if (!copyout && !keep) {
            unlink(argv[i]);
        }
    }
    if (fout != stdout) {
        fclose(fout);
    }

    return 0;
}
