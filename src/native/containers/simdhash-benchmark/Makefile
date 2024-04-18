.DEFAULT_GOAL := default_target

dn_deps := $(wildcard ../*.c) $(wildcard ../*.h)
benchmark_deps := $(wildcard ./*.c) $(wildcard ./*.h)

benchmark_sources := ../dn-simdhash.c ../dn-vector.c ./benchmark.c ../dn-simdhash-u32-ptr.c ../dn-simdhash-string-ptr.c ./ghashtable.c ./all-measurements.c
common_options := -g -O3 -DNO_CONFIG_H -lm -DNDEBUG

benchmark-native: $(dn_deps) $(benchmark_deps)
	clang $(benchmark_sources) $(common_options) -DSIZEOF_VOID_P=8

benchmark-wasm: $(dn_deps) $(benchmark_deps)
	~/Projects/emscripten/emcc $(benchmark_sources) $(common_options) -DSIZEOF_VOID_P=4 -mbulk-memory -msimd128

disassemble-benchmark: benchmark-native benchmark-wasm
	objdump -d ./a.out > ./a.dis
	~/wabt/bin/wasm2wat ./a.out.wasm > ./a.wat

run-native: benchmark-native
	./a.out

run-wasm: benchmark-wasm
	node ./a.out.js

default_target: disassemble-benchmark

