#pragma once

#include <stdlib.h>
#include <stdint.h>
#include <string.h>

#define IMWIDTH 1024
#define IMHEIGHT 1024

extern "C" {
#  include <png.h>
}

typedef struct _png_dat {
    uint8_t *buf;
    int64_t len;
    size_t buf_rem;
} png_dat;

typedef struct _png_parse_dat {
    uint8_t *cur_pos;
} png_parse_dat;

/* Write a customized write callback so that we write back to an in-memory buffer.
 * This allows the testing to not involve disk IO */
static void png_write_cb(png_structp pngp, png_bytep data, png_size_t len) {
    png_dat *dat = (png_dat*)png_get_io_ptr(pngp);
    size_t curSize = dat->len + len;

    /* realloc double the requested buffer size to prevent excessive reallocs */
    if (dat->buf_rem < len) {
        dat->buf = (uint8_t*)realloc(dat->buf, dat->len + dat->buf_rem + 2 * len);

        if (!dat->buf) {
            /* Pretty unlikely but we'll put it here just in case */
            fprintf(stderr, "realloc failed, exiting\n");
            exit(1);
        }

        dat->buf_rem += 2 * len;
    }

    memcpy(dat->buf + dat->len, data, len);
    dat->len = curSize;
    dat->buf_rem -= len;
}

static void init_compressible(png_bytep buf, size_t num_pix) {
    /* It doesn't actually matter what we make this, but for
     * the sake of a reasonable test image, let's make this
     * be a stripe of R, G, & B, with no alpha channel */
    int32_t i = 0;
    int32_t red_stop = num_pix / 3;
    int32_t blue_stop = 2 * num_pix / 3;
    int32_t green_stop = num_pix;

    for (int32_t x = 0; i < red_stop; x += 3, ++i) {
       buf[x] = 255;
       buf[x + 1] = 0;
       buf[x + 2] = 0;
    }

    for (int32_t x = 3 * i; i < blue_stop; x+= 3, ++i) {
       buf[x] = 0;
       buf[x + 1] = 255;
       buf[x + 2] = 0;
    }

    for (int32_t x = 3 * i; i < green_stop; x += 3, ++i) {
       buf[x] = 0;
       buf[x + 1] = 0;
       buf[x + 2] = 255;
    }
}

static inline void encode_png(png_bytep buf, png_dat *outpng, int32_t comp_level, uint32_t width, uint32_t height) {
    png_structp png = png_create_write_struct(PNG_LIBPNG_VER_STRING, NULL, NULL, NULL);

    /* Most of this error handling is _likely_ not necessary. Likewise it's likely
     * a lot of this stuff can be done in the setup function to avoid measuring this
     * fixed setup time, but for now we'll do it here */
    if (!png) abort();

    png_infop info  = png_create_info_struct(png);
    if (!info) abort();

    png_set_write_fn(png, outpng, png_write_cb, NULL);
    png_bytep *png_row_ptrs = new png_bytep[height];
    for (int i = 0; i < IMHEIGHT; ++i) {
        png_row_ptrs[i] = (png_bytep)&buf[3*i*width];
    }

    png_set_IHDR(png, info, IMWIDTH, IMHEIGHT, 8, PNG_COLOR_TYPE_RGB,
                 PNG_INTERLACE_NONE, PNG_COMPRESSION_TYPE_DEFAULT,
                 PNG_FILTER_TYPE_DEFAULT);

    png_write_info(png, info);
    png_set_compression_level(png, comp_level);
    png_set_filter(png, 0, PNG_FILTER_NONE);
    png_write_image(png, (png_bytepp)png_row_ptrs);
    png_write_end(png, NULL);
    png_destroy_write_struct(&png, &info);
    delete[] png_row_ptrs;
}

static void read_from_pngdat(png_structp png, png_bytep out, png_size_t bytes_to_read) {
    png_parse_dat *io = (png_parse_dat*)png_get_io_ptr(png);
    memcpy(out, io->cur_pos, bytes_to_read);
    io->cur_pos += bytes_to_read;
}

static inline int decode_png(png_parse_dat *dat, png_bytepp out_bytes, size_t in_size, uint32_t &width, uint32_t &height) {
    png_structp png = NULL;
    png = png_create_read_struct(PNG_LIBPNG_VER_STRING, NULL, NULL, NULL);

    if (!png) abort();
    png_infop info = NULL;
    info = png_create_info_struct(png);
    if (!info) abort();

    png_set_read_fn(png, dat, read_from_pngdat);
    png_read_info(png, info);

    int bit_depth = 0, color_type = -1;
    png_get_IHDR(png, info, &width, &height, &bit_depth, &color_type, NULL, NULL, NULL);

    size_t im_size = width * height * bit_depth/8 * 3;
    if (color_type != PNG_COLOR_TYPE_RGB) {
        fprintf(stderr, "expected an 8 bpp RGB image\n");
        abort();
    }

    if (im_size > in_size) {
       *out_bytes = (png_bytep)realloc(*out_bytes, im_size);
    }

    png_bytep *out_rows = new png_bytep[height];
    for (size_t i = 0; i < height; ++i)
        out_rows[i] = *out_bytes + (width*i*3);

    png_read_rows(png, out_rows, NULL, height);
    png_destroy_read_struct(&png, &info, NULL);
    delete[] out_rows;

    return im_size;
}
