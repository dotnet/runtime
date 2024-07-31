/* minigzip.c -- simulate gzip using the zlib compression library
 * Copyright (C) 1995-2006, 2010, 2011, 2016 Jean-loup Gailly
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/*
 * minigzip is a minimal implementation of the gzip utility. This is
 * only an example of using zlib and isn't meant to replace the
 * full-featured gzip. No attempt is made to deal with file systems
 * limiting names to 14 or 8+3 characters, etc... Error checking is
 * very limited. So use minigzip only for testing; use gzip for the
 * real thing.
 */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif
#include <stdio.h>
#include <assert.h>

#ifdef USE_MMAP
#  include <sys/types.h>
#  include <sys/mman.h>
#  include <sys/stat.h>
#endif

#if defined(_WIN32) || defined(__CYGWIN__)
#  include <fcntl.h>
#  include <io.h>
#  define SET_BINARY_MODE(file) setmode(fileno(file), O_BINARY)
#else
#  define SET_BINARY_MODE(file)
#endif

#if defined(_MSC_VER) && _MSC_VER < 1900
#  define snprintf _snprintf
#endif

#if !defined(Z_HAVE_UNISTD_H) && !defined(_LARGEFILE64_SOURCE)
#ifndef _WIN32 /* unlink already in stdio.h for Win32 */
extern int unlink (const char *);
#endif
#endif

#ifndef GZ_SUFFIX
#  define GZ_SUFFIX ".gz"
#endif
#define SUFFIX_LEN (sizeof(GZ_SUFFIX)-1)

#define BUFLEN      16384        /* read buffer size */
#define BUFLENW     (BUFLEN * 3) /* write buffer size */
#define MAX_NAME_LEN 1024

static const char *prog = "minigzip_fuzzer";

/* ===========================================================================
 * Display error message and exit
 */
static void error(const char *msg) {
    fprintf(stderr, "%s: %s\n", prog, msg);
    exit(1);
}

#ifdef USE_MMAP /* MMAP version, Miguel Albrecht <malbrech@eso.org> */
/* ===========================================================================
 * Try compressing the input file at once using mmap. Return Z_OK if
 * success, Z_ERRNO otherwise.
 */
static int gz_compress_mmap(FILE *in, gzFile out) {
    int len;
    int err;
    int ifd = fileno(in);
    char *buf;      /* mmap'ed buffer for the entire input file */
    off_t buf_len;  /* length of the input file */
    struct stat sb;

    /* Determine the size of the file, needed for mmap: */
    if (fstat(ifd, &sb) < 0) return Z_ERRNO;
    buf_len = sb.st_size;
    if (buf_len <= 0) return Z_ERRNO;

    /* Now do the actual mmap: */
    buf = mmap((void *)0, buf_len, PROT_READ, MAP_SHARED, ifd, (off_t)0);
    if (buf == (char *)(-1)) return Z_ERRNO;

    /* Compress the whole file at once: */
    len = PREFIX(gzwrite)(out, (char *)buf, (unsigned)buf_len);

    if (len != (int)buf_len) error(PREFIX(gzerror)(out, &err));

    munmap(buf, buf_len);
    fclose(in);
    if (PREFIX(gzclose)(out) != Z_OK) error("failed gzclose");
    return Z_OK;
}
#endif /* USE_MMAP */

/* ===========================================================================
 * Compress input to output then close both files.
 */

static void gz_compress(FILE *in, gzFile out) {
    char buf[BUFLEN];
    int len;
    int err;

#ifdef USE_MMAP
    /* Try first compressing with mmap. If mmap fails (minigzip used in a
     * pipe), use the normal fread loop.
     */
    if (gz_compress_mmap(in, out) == Z_OK) return;
#endif
    /* Clear out the contents of buf before reading from the file to avoid
       MemorySanitizer: use-of-uninitialized-value warnings. */
    memset(buf, 0, sizeof(buf));
    for (;;) {
        len = (int)fread(buf, 1, sizeof(buf), in);
        if (ferror(in)) {
            perror("fread");
            exit(1);
        }
        if (len == 0) break;

        if (PREFIX(gzwrite)(out, buf, (unsigned)len) != len) error(PREFIX(gzerror)(out, &err));
    }
    fclose(in);
    if (PREFIX(gzclose)(out) != Z_OK) error("failed gzclose");
}

/* ===========================================================================
 * Uncompress input to output then close both files.
 */
static void gz_uncompress(gzFile in, FILE *out) {
    char buf[BUFLENW];
    int len;
    int err;

    for (;;) {
        len = PREFIX(gzread)(in, buf, sizeof(buf));
        if (len < 0) error (PREFIX(gzerror)(in, &err));
        if (len == 0) break;

        if ((int)fwrite(buf, 1, (unsigned)len, out) != len) {
            error("failed fwrite");
        }
    }
    if (fclose(out)) error("failed fclose");

    if (PREFIX(gzclose)(in) != Z_OK) error("failed gzclose");
}


/* ===========================================================================
 * Compress the given file: create a corresponding .gz file and remove the
 * original.
 */
static void file_compress(char *file, char *mode) {
    char outfile[MAX_NAME_LEN];
    FILE *in;
    gzFile out;

    if (strlen(file) + strlen(GZ_SUFFIX) >= sizeof(outfile)) {
        fprintf(stderr, "%s: filename too long\n", prog);
        exit(1);
    }

    snprintf(outfile, sizeof(outfile), "%s%s", file, GZ_SUFFIX);

    in = fopen(file, "rb");
    if (in == NULL) {
        perror(file);
        exit(1);
    }
    out = PREFIX(gzopen)(outfile, mode);
    if (out == NULL) {
        fprintf(stderr, "%s: can't gzopen %s\n", prog, outfile);
        exit(1);
    }
    gz_compress(in, out);

    unlink(file);
}

/* ===========================================================================
 * Uncompress the given file and remove the original.
 */
static void file_uncompress(char *file) {
    char buf[MAX_NAME_LEN];
    char *infile, *outfile;
    FILE *out;
    gzFile in;
    size_t len = strlen(file);

    if (len + strlen(GZ_SUFFIX) >= sizeof(buf)) {
        fprintf(stderr, "%s: filename too long\n", prog);
        exit(1);
    }

    snprintf(buf, sizeof(buf), "%s", file);

    if (len > SUFFIX_LEN && strcmp(file+len-SUFFIX_LEN, GZ_SUFFIX) == 0) {
        infile = file;
        outfile = buf;
        outfile[len-3] = '\0';
    } else {
        outfile = file;
        infile = buf;
        snprintf(buf + len, sizeof(buf) - len, "%s", GZ_SUFFIX);
    }
    in = PREFIX(gzopen)(infile, "rb");
    if (in == NULL) {
        fprintf(stderr, "%s: can't gzopen %s\n", prog, infile);
        exit(1);
    }
    out = fopen(outfile, "wb");
    if (out == NULL) {
        perror(file);
        exit(1);
    }

    gz_uncompress(in, out);

    unlink(infile);
}

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t dataLen) {
    char *inFileName = "minigzip_fuzzer.out";
    char *outFileName = "minigzip_fuzzer.out.gz";
    char outmode[20];
    FILE *in;
    char buf[BUFLEN];
    uint32_t offset = 0;

    /* Discard inputs larger than 1Mb. */
    static size_t kMaxSize = 1024 * 1024;
    if (dataLen < 1 || dataLen > kMaxSize)
        return 0;

    in = fopen(inFileName, "wb");
    if (fwrite(data, 1, (unsigned)dataLen, in) != dataLen)
        error("failed fwrite");
    if (fclose(in))
        error("failed fclose");

    memset(outmode, 0, sizeof(outmode));
    snprintf(outmode, sizeof(outmode), "%s", "wb");

    /* Compression level: [0..9]. */
    outmode[2] = '0' + (data[0] % 10);

    switch (data[dataLen-1] % 6) {
    default:
    case 0:
        outmode[3] = 0;
        break;
    case 1:
        /* compress with Z_FILTERED */
        outmode[3] = 'f';
        break;
    case 2:
        /* compress with Z_HUFFMAN_ONLY */
        outmode[3] = 'h';
        break;
    case 3:
        /* compress with Z_RLE */
        outmode[3] = 'R';
        break;
    case 4:
        /* compress with Z_FIXED */
        outmode[3] = 'F';
        break;
    case 5:
        /* direct */
        outmode[3] = 'T';
        break;
    }

    file_compress(inFileName, outmode);

    /* gzopen does not support reading in direct mode */
    if (outmode[3] == 'T')
        inFileName = outFileName;
    else
        file_uncompress(outFileName);

    /* Check that the uncompressed file matches the input data. */
    in = fopen(inFileName, "rb");
    if (in == NULL) {
        perror(inFileName);
        exit(1);
    }

    memset(buf, 0, sizeof(buf));
    for (;;) {
        int len = (int)fread(buf, 1, sizeof(buf), in);
        if (ferror(in)) {
            perror("fread");
            exit(1);
        }
        if (len == 0)
            break;
        assert(0 == memcmp(data + offset, buf, len));
        offset += len;
    }

    if (fclose(in))
        error("failed fclose");

    /* This function must return 0. */
    return 0;
}
