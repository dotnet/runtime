#include <stdio.h>

#define TRACING 0

typedef struct {
    float value;
} TRes;

TRes accept_double_struct_and_return_float_struct (
    struct { struct { double value; } value; } arg
) {
#if TRACING
    printf (
        "&arg=%x (ulonglong)arg=%llx arg.value.value=%lf\n",
        (unsigned int)&arg, *(unsigned long long*)&arg, (double)arg.value.value
    );
#endif
    TRes result = { arg.value.value };
    return result;
}

typedef struct {
    long long value;
} TResI64;

TResI64 accept_and_return_i64_struct (TResI64 arg) {
#if TRACING
    printf (
        "&arg=%x (ulonglong)arg=%llx\n",
        (unsigned int)&arg, *(unsigned long long*)&arg
    );
#endif
    TResI64 result = { ~arg.value };
    return result;
}

typedef struct {
    int A, B;
} PairStruct;

PairStruct accept_and_return_pair (PairStruct arg) {
#if TRACING
    printf (
        "&arg=%d arg.A=%d arg.B=%d\n",
        (unsigned int)&arg, arg.A, arg.B
    );
#endif
    arg.A = 32;
    arg.B *= 2;
    return arg;
}

typedef struct {
    int elements[2];
} MyInlineArray;

MyInlineArray accept_and_return_inlinearray (MyInlineArray arg) {
#if TRACING
    printf (
        "&arg=%d arg.elements[0]=%d arg.elements[1]=%d\n",
        (unsigned int)&arg, arg.elements[0], arg.elements[1]
    );
#endif
    arg.elements[0] = 32;
    arg.elements[1] *= 2;
    return arg;
}

MyInlineArray accept_and_return_fixedarray (MyInlineArray arg) {
    return accept_and_return_inlinearray (arg);
}
