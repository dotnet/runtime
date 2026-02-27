#include <algorithm>
#include <numeric>
#include <random>
#include <cassert>
#include <stdio.h>
#include <sys/time.h>

using namespace std;

#include "do_vxsort.h"
#include "../../../introsort.h"

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

long demo_vxsort(std::vector<uint8_t*>* v, size_t vector_size)
{
    // make a full copy of the list
    auto vtemp = *v;

    auto begin = vtemp.data();
    auto end = begin + vector_size - 1;

    uint8_t* range_low =  START;
    uint8_t* range_high = (uint8_t*)0xffffffffffffffff;

    // Ensure not sorted
    bool sorted = true;
    auto prev = START - (1 << SHIFT);
    for (auto & element : vtemp) {
        // fprintf(stderr, "%p\n", element);
        if (element != prev + (1 << SHIFT))
        {
            sorted = false;
        }
        prev = element;
    }
    assert(!sorted);

    struct timeval t0, t1;
    gettimeofday(&t0, 0);

#if defined(CPU_FEATURES_ARCH_X86)
    if (IsSupportedInstructionSet (InstructionSet::AVX2)) {
        do_vxsort_avx2(begin, end, range_low, range_high);
    }
    else if (IsSupportedInstructionSet (InstructionSet::AVX512F))
    {
        do_vxsort_avx512(begin, end, range_low, range_high);
    }
    else
#elif defined(CPU_FEATURES_ARCH_AARCH64)
    if (IsSupportedInstructionSet (InstructionSet::NEON))
    {
        do_vxsort_neon(begin, end, range_low, range_high);
    }
    else
#endif
    {
        fprintf(stderr, "CPU doesn't seem to support any vectorized ISA, bye-bye\n");
        exit(-2);
    }

    gettimeofday(&t1, 0);
    long elapsed = (t1.tv_sec - t0.tv_sec) * 1000000 + t1.tv_usec - t0.tv_usec;

    // Ensure sorted
    prev = START - (1 << SHIFT);
    for (auto & element : vtemp) {
        // fprintf(stderr, "%p\n", element);
        assert(element == prev + (1 << SHIFT));
        prev = element;
    }

    return elapsed;
}


long demo_insertsort(std::vector<uint8_t*>* v, size_t vector_size)
{
    // make a full copy of the list
    auto vtemp = *v;

    auto begin = vtemp.data();
    auto end = begin + vector_size - 1;

    uint8_t* range_low =  START;
    uint8_t* range_high = (uint8_t*)0xffffffffffffffff;

    // Ensure not sorted
    bool sorted = true;
    auto prev = START - (1 << SHIFT);
    for (auto & element : vtemp) {
        if (element != prev + (1 << SHIFT))
        {
            sorted = false;
        }
        prev = element;
    }
    assert(!sorted);

    struct timeval t0, t1;
    gettimeofday(&t0, 0);

    introsort::sort(begin, end, vector_size);

    gettimeofday(&t1, 0);
    long elapsed = (t1.tv_sec - t0.tv_sec) * 1000000 + t1.tv_usec - t0.tv_usec;

    // Ensure sorted
    prev = START - (1 << SHIFT);
    for (auto & element : vtemp) {
        assert(element == prev + (1 << SHIFT));
        prev = element;
    }

    return elapsed;
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

    long elapsed;

    // Run once to ensure any global setup is done
    elapsed = demo_vxsort(&v, vector_size);
    elapsed = demo_insertsort(&v, vector_size);

    elapsed = demo_vxsort(&v, vector_size);
    printf("vxsort: Time= %lu us\n", elapsed);

    elapsed = demo_vxsort(&v, vector_size);
    printf("vxsort: Time= %lu us\n", elapsed);

    elapsed = demo_vxsort(&v, vector_size);
    printf("vxsort: Time= %lu us\n", elapsed);

    elapsed = demo_insertsort(&v, vector_size);
    printf("insertsort: Time= %lu us\n", elapsed);

    elapsed = demo_insertsort(&v, vector_size);
    printf("insertsort: Time= %lu us\n", elapsed);

    elapsed = demo_insertsort(&v, vector_size);
    printf("insertsort: Time= %lu us\n", elapsed);

    return 0;
}
