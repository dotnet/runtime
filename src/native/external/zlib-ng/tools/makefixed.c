#include <stdio.h>
#include "zbuild.h"
#include "zutil.h"
#include "inftrees.h"
#include "inflate.h"

// Build and return state with length and distance decoding tables and index sizes set to fixed code decoding.
void Z_INTERNAL buildfixedtables(struct inflate_state *state) {
    static code *lenfix, *distfix;
    static code fixed[544];

    // build fixed huffman tables
    unsigned sym, bits;
    static code *next;

    // literal/length table
    sym = 0;
    while (sym < 144) state->lens[sym++] = 8;
    while (sym < 256) state->lens[sym++] = 9;
    while (sym < 280) state->lens[sym++] = 7;
    while (sym < 288) state->lens[sym++] = 8;
    next = fixed;
    lenfix = next;
    bits = 9;
    zng_inflate_table(LENS, state->lens, 288, &(next), &(bits), state->work);

    // distance table
    sym = 0;
    while (sym < 32) state->lens[sym++] = 5;
    distfix = next;
    bits = 5;
    zng_inflate_table(DISTS, state->lens, 32, &(next), &(bits), state->work);

    state->lencode = lenfix;
    state->lenbits = 9;
    state->distcode = distfix;
    state->distbits = 5;
}


//  Create fixed tables on the fly and write out a inffixed_tbl.h file that is #include'd above.
//  makefixed() writes those tables to stdout, which would be piped to inffixed_tbl.h.
void makefixed(void) {
    unsigned low, size;
    struct inflate_state state;

    memset(&state, 0, sizeof(state));
    buildfixedtables(&state);
    puts("/* inffixed_tbl.h -- table for decoding fixed codes");
    puts(" * Generated automatically by makefixed().");
    puts(" */");
    puts("");
    puts("/* WARNING: this file should *not* be used by applications.");
    puts(" * It is part of the implementation of this library and is");
    puts(" * subject to change. Applications should only use zlib.h.");
    puts(" */");
    puts("");
    size = 1U << 9;
    printf("static const code lenfix[%u] = {", size);
    low = 0;
    for (;;) {
        if ((low % 7) == 0)
            printf("\n    ");
        printf("{%u,%u,%d}", (low & 127) == 99 ? 64 : state.lencode[low].op,
            state.lencode[low].bits, state.lencode[low].val);
        if (++low == size)
            break;
        putchar(',');
    }
    puts("\n};");
    size = 1U << 5;
    printf("\nstatic const code distfix[%u] = {", size);
    low = 0;
    for (;;) {
        if ((low % 6) == 0)
            printf("\n    ");
        printf("{%u,%u,%d}", state.distcode[low].op, state.distcode[low].bits, state.distcode[low].val);
        if (++low == size)
            break;
        putchar(',');
    }
    puts("\n};");
}

// The output of this application can be piped out to recreate inffixed_tbl.h
int main(void) {
    makefixed();
    return 0;
}
