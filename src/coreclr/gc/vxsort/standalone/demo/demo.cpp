#include <algorithm>
#include <numeric>
#include <random>
#include <cassert>

using namespace std;

#include "do_vxsort.h"

uint8_t* START = (uint8_t*)0x1234567812345678;
uint8_t  SHIFT = 3;

std::vector<uint8_t*> generate_random_garbage(const uint64_t size) {

    auto vec = std::vector<uint8_t*>(size);

    for (uint64_t i = 0; i < size; ++i) {
        vec[i] = reinterpret_cast<uint8_t*>(START + (i << SHIFT));
    }

    std::random_device rd;
    std::mt19937 g(rd());

    std::shuffle(vec.begin(), vec.end(), g);
    return vec;
}

int main(int argc, char** argv) {
    if (argc != 2) {
        fprintf(stderr, "demo array size must be specified\n");
        return -1;
    }

#if defined(TARGET_AMD64)
    InitSupportedInstructionSet(1 << (int)InstructionSet::AVX2);
#elif defined(TARGET_ARM64)
    InitSupportedInstructionSet(1 << (int)InstructionSet::NEON);
#endif

    const size_t vector_size = atoi(argv[1]);
    auto v = generate_random_garbage(vector_size);

    auto begin = v.data();
    auto end = begin + vector_size - 1;

    uint8_t* range_low =  START;
    uint8_t* range_high = START + vector_size + 2;

    for (auto & element : v) {
        fprintf(stderr, "%p\n", element);
    }

#if defined(CPU_FEATURES_ARCH_X86)
    if (IsSupportedInstructionSet (InstructionSet::AVX2)) {
        fprintf(stderr, "Sorting with AVX2...");
        do_vxsort_avx2(begin, end, range_low, range_high);
        fprintf(stderr, "...done!\n");
    }
    else if (IsSupportedInstructionSet (InstructionSet::AVX512F))
    {
        fprintf(stderr, "Sorting with AVX512...");
        do_vxsort_avx512(begin, end, range_low, range_high);
        fprintf(stderr, "...done!\n");
    }
    else
#elif defined(CPU_FEATURES_ARCH_AARCH64)
    if (IsSupportedInstructionSet (InstructionSet::NEON))
    {
        fprintf(stderr, "Sorting with NEON...");
        do_vxsort_neon(begin, end, range_low, range_high);
        fprintf(stderr, "...done!\n");
    }
    else
#endif
    {
        fprintf(stderr, "CPU doesn't seem to support any vectorized ISA, bye-bye\n");
        return -2;
    }

    auto prev = START - (1 << SHIFT);
    for (auto & element : v) {
        fprintf(stderr, "%p\n", element);
        assert(element == prev + (1 << SHIFT));
        prev = element;
    }

    return 0;
}
