#include <stdio.h>
#include <benchmark/benchmark.h>
#include "benchmark_png_shared.h"
#include <assert.h>

class png_decode: public benchmark::Fixture {
protected:
    png_dat inpng[10];

    /* Backing this on the heap is a more realistic benchmark */
    uint8_t *output_img_buf = NULL;

public:
    /* Let's make the vanilla version have something extremely compressible */
    virtual void init_img(png_bytep img_bytes, size_t width, size_t height) {
        init_compressible(img_bytes, width*height);
    }

    void SetUp(const ::benchmark::State& state) {
        output_img_buf = (uint8_t*)malloc(IMWIDTH * IMHEIGHT * 3);
        assert(output_img_buf != NULL);
        init_img(output_img_buf, IMWIDTH, IMHEIGHT);

        /* First we need to author the png bytes to be decoded */
        for (int i = 0; i < 10; ++i) {
            inpng[i] = {NULL, 0, 0};
            encode_png(output_img_buf, &inpng[i], i, IMWIDTH, IMHEIGHT);
        }
    }

    /* State in this circumstance will convey the compression level */
    void Bench(benchmark::State &state) {
        for (auto _ : state) {
            int compress_lvl = state.range(0);
            png_parse_dat in = { inpng[compress_lvl].buf };
            uint32_t width, height;
            decode_png(&in, (png_bytepp)&output_img_buf, IMWIDTH * IMHEIGHT * 3, width, height);
        }
    }

    void TearDown(const ::benchmark::State &state) {
        free(output_img_buf);
        for (int i = 0; i < 10; ++i) {
            free(inpng[i].buf);
        }
    }
};

class png_decode_realistic: public png_decode {
private:
    bool test_files_found = false;

public:
    void SetUp(const ::benchmark::State &state) {
        output_img_buf = NULL;
        output_img_buf = (uint8_t*)malloc(IMWIDTH * IMHEIGHT * 3);
        /* Let's take all the images at different compression levels and jam their bytes into buffers */
        char test_fname[25];
        FILE *files[10];

        /* Set all to NULL */
        memset(files, 0, sizeof(FILE*));

        for (size_t i = 0; i < 10; ++i) {
            sprintf(test_fname, "test_pngs/%1lu.png", i);
            FILE *in_img = fopen(test_fname, "r");
            if (in_img == NULL) {
                for (size_t j = 0; j < i; ++j) {
                    if (files[j])
                        fclose(files[j]);
                }

                /* For proper cleanup */
                for (size_t j = i; j < 10; ++j) {
                    inpng[i] = { NULL, 0, 0 };
                }

                return;
            }
            files[i] = in_img;
        }

        test_files_found = true;
        /* Now that we've established we have all the png files, let's read all of their bytes into buffers */
        for (size_t i = 0; i < 10; ++i) {
            FILE *in_file = files[i];
            fseek(in_file, 0, SEEK_END);
            size_t num_bytes = ftell(in_file);
            rewind(in_file);

            uint8_t *raw_file = (uint8_t*)malloc(num_bytes);
            if (raw_file == NULL)
                abort();

            inpng[i].buf = raw_file;
            inpng[i].len = num_bytes;
            inpng[i].buf_rem = 0;

            size_t bytes_read = fread(raw_file, 1, num_bytes, in_file);
            if (bytes_read != num_bytes) {
                fprintf(stderr, "couldn't read all of the bytes for file test_pngs/%lu.png", i);
                abort();
            }

            fclose(in_file);
        }
    }

    void Bench(benchmark::State &state) {
        if (!test_files_found) {
            state.SkipWithError("Test imagery in test_pngs not found");
        }

        png_decode::Bench(state);
    }
};

BENCHMARK_DEFINE_F(png_decode, png_decode)(benchmark::State &state) {
    Bench(state);
}
BENCHMARK_REGISTER_F(png_decode, png_decode)->DenseRange(0, 9, 1)->Unit(benchmark::kMicrosecond);

BENCHMARK_DEFINE_F(png_decode_realistic, png_decode_realistic)(benchmark::State &state) {
    Bench(state);
}
BENCHMARK_REGISTER_F(png_decode_realistic, png_decode_realistic)->DenseRange(0, 9, 1)->Unit(benchmark::kMicrosecond);
