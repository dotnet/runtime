// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

typedef struct
{
    int32_t value;
} single_int;

extern int32_t managed_echo(int32_t value);
extern single_int managed_echo_single_int_struct(int32_t value);
extern double managed_echo_mixed_scalars(int64_t value, float addend, double scale);
extern void managed_echo_pointer(int32_t* value, int32_t delta);

int32_t echo(int32_t value)
{
    return managed_echo(value);
}

single_int echo_single_int_struct(int32_t value)
{
    return managed_echo_single_int_struct(value);
}

double echo_mixed_scalars(int64_t value, float addend, double scale)
{
    return managed_echo_mixed_scalars(value, addend, scale);
}

void echo_pointer(int32_t* value, int32_t delta)
{
    managed_echo_pointer(value, delta);
}
