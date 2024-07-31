#include <stdio.h>
#include <assert.h>
#include <benchmark/benchmark.h>
#include "benchmark_png_shared.h"

#define IMWIDTH 1024
#define IMHEIGHT 1024

class png_encode: public benchmark::Fixture {
private:
    png_dat outpng;

    /* Backing this on the heap is a more realistic benchmark */
    uint8_t *input_img_buf = NULL;

public:
    /* Let's make the vanilla version have something extremely compressible */
    virtual void init_img(png_bytep img_bytes, size_t width, size_t height) {
        init_compressible(img_bytes, width * height);
    }

    void SetUp(const ::benchmark::State& state) {
        input_img_buf = (uint8_t*)malloc(IMWIDTH * IMHEIGHT * 3);
        outpng.buf = (uint8_t*)malloc(IMWIDTH * IMHEIGHT * 3);
        /* Using malloc rather than zng_alloc so that we can call realloc.
         * IMWIDTH * IMHEIGHT is likely to be more than enough bytes, though,
         * given that a simple run length encoding already pretty much can
         * reduce to this */
        outpng.len = 0;
        outpng.buf_rem = IMWIDTH * IMHEIGHT * 3;
        assert(input_img_buf != NULL);
        assert(outpng.buf != NULL);
        init_img(input_img_buf, IMWIDTH, IMHEIGHT);
    }

    /* State in this circumstance will convey the compression level */
    void Bench(benchmark::State &state) {
        for (auto _ : state) {
            encode_png((png_bytep)input_img_buf, &outpng, state.range(0), IMWIDTH, IMHEIGHT);
            outpng.buf_rem = outpng.len;
            outpng.len = 0;
        }
    }

    void TearDown(const ::benchmark::State &state) {
        free(input_img_buf);
        free(outpng.buf);
    }
};

BENCHMARK_DEFINE_F(png_encode, encode_compressible)(benchmark::State &state) {
    Bench(state);
}
BENCHMARK_REGISTER_F(png_encode, encode_compressible)->DenseRange(0, 9, 1)->Unit(benchmark::kMicrosecond);
