#include <stdio.h>

typedef struct {
    float value;
} TRes;

TRes accept_double_struct_and_return_float_struct (
    struct { struct { double value; } value; } arg
) {
    printf (
        "&arg=%x (ulonglong)arg=%llx arg.value.value=%lf\n",
        (unsigned int)&arg, *(unsigned long long*)&arg, (double)arg.value.value
    );
    TRes result = { arg.value.value };
    return result;
}

typedef struct {
    long long value;
} TResI64;

TResI64 accept_and_return_i64_struct (TResI64 arg) {
    printf (
        "&arg=%x (ulonglong)arg=%llx\n",
        (unsigned int)&arg, *(unsigned long long*)&arg
    );
    TResI64 result = { ~arg.value };
    return result;
}
