/* benchmark_compress.cc -- benchmark compress()
 * Copyright (C) 2024 Hans Kristian Rosbach
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include <assert.h>
#include <benchmark/benchmark.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil_p.h"
#  if defined(ZLIB_COMPAT)
#    include "zlib.h"
#  else
#    include "zlib-ng.h"
#  endif
}

#define MAX_SIZE (32 * 1024)

class compress_bench: public benchmark::Fixture {
private:
    size_t maxlen;
    uint8_t *inbuff;
    uint8_t *outbuff;

public:
    void SetUp(const ::benchmark::State& state) {
        const char teststr[42] = "Hello hello World broken Test tast mello.";
        maxlen = MAX_SIZE;

        inbuff = (uint8_t *)zng_alloc(MAX_SIZE + 1);
        assert(inbuff != NULL);

        outbuff = (uint8_t *)zng_alloc(MAX_SIZE + 1);
        assert(outbuff != NULL);

        int pos = 0;
        for (int32_t i = 0; i < MAX_SIZE - 42 ; i+=42){
           pos += sprintf((char *)inbuff+pos, "%s", teststr);
        }
    }

    void Bench(benchmark::State& state) {
        int err;

        for (auto _ : state) {
            err = PREFIX(compress)(outbuff, &maxlen, inbuff, (size_t)state.range(0));
        }

        benchmark::DoNotOptimize(err);
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(inbuff);
        zng_free(outbuff);
    }
};

#define BENCHMARK_COMPRESS(name) \
    BENCHMARK_DEFINE_F(compress_bench, name)(benchmark::State& state) { \
        Bench(state); \
    } \
    BENCHMARK_REGISTER_F(compress_bench, name)->Arg(1)->Arg(8)->Arg(16)->Arg(32)->Arg(64)->Arg(512)->Arg(4<<10)->Arg(32<<10);

BENCHMARK_COMPRESS(compress_bench);
