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
