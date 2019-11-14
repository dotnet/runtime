// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdarg.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))

#if __i386__
#define _cdecl __attribute__((cdecl))
#else
#define _cdecl
#endif

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#ifdef BIT64
#define __int64     long
#else // BIT64
#define __int64     long long
#endif // BIT64

#endif // !_MSC_VER

/* Structures */

/*
 * struct one_byte_struct (4 bytes)
*/
typedef struct 
{
    int one;
} one_int_struct;

/*
 * struct two_int_struct (8 bytes)
*/
typedef struct 
{
    int one;
    int two;
} two_int_struct;

/*
 * struct one_long_long_struct (8 bytes)
*/
typedef struct 
{
    __int64 one;
} one_long_long_struct;

/*
 * struct two_long_long_struct (16 bytes)
*/
typedef struct 
{
    __int64 one;
    __int64 two;
} two_long_long_struct;

/*
 * struct four_int_struct (16 bytes)
*/
typedef struct 
{
    int one;
    int two;
    int three;
    int four;
} four_int_struct;

/*
 * struct four_long_long_struct (32 bytes)
*/
typedef struct 
{
    __int64 one;
    __int64 two;
    __int64 three;
    __int64 four;
} four_long_long_struct;

/*
 * struct one_float_struct (4 bytes)
*/
typedef struct 
{
    float one;
} one_float_struct;

/*
 * struct two_float_struct (8 bytes)
*/
typedef struct 
{
    float one;
    float two;
} two_float_struct;

/*
 * struct one_double_struct (8 bytes)
*/
typedef struct 
{
    double one;
} one_double_struct;

/*
 * struct two_double_struct (16 bytes)
*/
typedef struct 
{
    double one;
    double two;
} two_double_struct;

/*
 * struct three_double_struct (24 bytes)
*/
typedef struct 
{
    double one;
    double two;
    double three;
} three_double_struct;

/*
 * struct four_float_struct (16 bytes)
*/
typedef struct 
{
    float one;
    float two;
    float three;
    float four;
} four_float_struct;

/*
 * struct four_double_struct (32 bytes)
*/
typedef struct 
{
    double one;
    double two;
    double three;
    double four;
} four_double_struct;

/*
 * struct eight_byte_struct (8 bytes)
*/
typedef struct
{
    char one;
    char two;
    char three;
    char four;
    char five;
    char six;
    char seven;
    char eight;
} eight_byte_struct;

/*
 * struct sixteen_byte_struct (8 bytes)
*/
typedef struct
{
    char one;
    char two;
    char three;
    char four;
    char five;
    char six;
    char seven;
    char eight;
    char nine;
    char ten;
    char eleven;
    char twelve;
    char thirteen;
    char fourteen;
    char fifteen;
    char sixteen;
} sixteen_byte_struct;

/* Tests */

DLLEXPORT int _cdecl test_passing_ints(int count, ...)
{
    va_list ap;
    int index, sum;

    va_start(ap, count);

    sum = 0;
    for (index = 0; index < count; ++index)
    {
        sum += va_arg(ap, int);
    }

    va_end(ap);
    return sum;
}

DLLEXPORT __int64 _cdecl test_passing_longs(int count, ...)
{
    va_list ap;
    int index;
    __int64 sum;

    va_start(ap, count);

    sum = 0;
    for (index = 0; index < count; ++index)
    {
        sum += va_arg(ap, __int64);
    }

    va_end(ap);
    return sum;
}

DLLEXPORT float _cdecl test_passing_floats(int count, ...)
{
    va_list ap;
    int index;
    double sum;

    va_start(ap, count);

    sum = 0;
    for (index = 0; index < count; ++index)
    {
        sum += va_arg(ap, double);
    }

    va_end(ap);
    return (float)sum;
}

DLLEXPORT double _cdecl test_passing_doubles(int count, ...)
{
    va_list ap;
    int index;
    double sum;

    va_start(ap, count);

    sum = 0;
    for (index = 0; index < count; ++index)
    {
        sum += va_arg(ap, double);
    }

    va_end(ap);
    return sum;
}

DLLEXPORT __int64 _cdecl test_passing_int_and_longs(int int_count, int long_count, ...)
{
    va_list ap;
    int index, count;
    __int64 sum;

    count = int_count + long_count;
    va_start(ap, long_count);

    sum = 0;
    for (index = 0; index < int_count; ++index)
    {
        sum += va_arg(ap, int);
    }

    for (index = 0; index < long_count; ++index)
    {
        sum += va_arg(ap, __int64);
    }

    va_end(ap);
    return sum;
}

DLLEXPORT double _cdecl test_passing_floats_and_doubles(int float_count, int double_count, ...)
{
    va_list ap;
    int index, count;
    double sum;

    count = float_count + double_count;
    va_start(ap, double_count);


    sum = 0;
    for (index = 0; index < float_count; ++index)
    {
        // Read a double, C ABI defines reading a float as undefined, or
        // an error on unix. However, the managed side will correctly pass a
        // float.
        sum += va_arg(ap, double);
    }

    for (index = 0; index < double_count; ++index)
    {
        sum += va_arg(ap, double);
    }

    va_end(ap);
    return sum;
}

/*
    Args:
        expected_value (double) : expected sum
        int                     : first value
        double                  : second value
        int                     : third value
        double                  : fourth value
        int                     : fifth value
        double                  : sixth value
*/
DLLEXPORT double _cdecl test_passing_int_and_double(double expected_value, ...)
{
    va_list ap;
    int index, count;
    double sum;

    count = 6;
    va_start(ap, expected_value);

    sum = 0;
    for (index = 0; index < 6; ++index)
    {
        if (index % 2 == 0) {
            sum += va_arg(ap, int);
        }
        else
        {
            sum += va_arg(ap, double);
        }
    }

    va_end(ap);
    return sum;
}

/*
    Args:
        expected_value (double) : expected sum
        __int64                 : first value
        double                  : second value
        __int64                 : third value
        double                  : fourth value
        __int64                 : fifth value
        double                  : sixth value
*/
DLLEXPORT double _cdecl test_passing_long_and_double(double expected_value, ...)
{
    va_list ap;
    int index, count;
    double sum;

    count = 6;
    va_start(ap, expected_value);

    sum = 0;
    for (index = 0; index < 6; ++index)
    {
        if (index % 2 == 0) {
            sum += va_arg(ap, __int64);
        }
        else
        {
            sum += va_arg(ap, double);
        }
    }

    va_end(ap);
    return sum;
}

/*
    Args:
        count (int)         : count of args
        is_int_structs(int) : first value
        is_float_value(int) : second value
        is_mixed (int)      : third value
        byte_count (int)    : fourth value
        struct_count (int)  : fifth value
*/
DLLEXPORT int _cdecl check_passing_struct(int count, ...)
{
    va_list ap;
    int is_b, is_floating, is_mixed, byte_count, struct_count;
    
    int expected_value_i;
    __int64 expected_value_l;
    double expected_value_f;
    double expected_value_d;

    int passed = 0;

    va_start(ap, count);

    is_b = va_arg(ap, int);
    is_floating = va_arg(ap, int);
    is_mixed = va_arg(ap, int);
    byte_count = va_arg(ap, int);
    struct_count = va_arg(ap, int);

    if (!is_floating)
    {
        if (byte_count == 8)
        {
            // Eight byte structs.
            if (is_b)
            {
                // This is one_long_long_struct
                one_long_long_struct s;
                __int64 sum;

                expected_value_l = va_arg(ap, __int64);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, one_long_long_struct);
                    sum += s.one;
                }

                if (sum != expected_value_l) passed = 1;
            }
            else
            {
                // This is two_int_struct
                two_int_struct s;
                int sum;

                expected_value_i = va_arg(ap, int);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, two_int_struct);
                    sum += s.one + s.two;
                }

                if (sum != expected_value_i) passed = 1;
            }
        }
        else if (byte_count == 16)
        {
            // 16 byte structs.
            if (is_b)
            {
                // This is four_int_struct
                four_int_struct s;
                int sum;

                expected_value_i = va_arg(ap, int);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, four_int_struct);
                    sum += s.one + s.two + s.three + s.four;
                }

                if (sum != expected_value_i) passed = 1;
            }
            else
            {
                // This is two_long_long_struct
                two_long_long_struct s;
                __int64 sum;

                expected_value_l = va_arg(ap, __int64);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, two_long_long_struct);
                    sum += s.one + s.two;
                }

                if (sum != expected_value_l) passed = 1;
            }
        }

        else if (byte_count == 32)
        {
            // This is sixteen_byte_struct
            four_long_long_struct s;
            __int64 sum;

            expected_value_l = va_arg(ap, __int64);
            sum = 0;

            while (struct_count--) {
                s = va_arg(ap, four_long_long_struct);
                sum += s.one + s.two + s.three + s.four;
            }

            if (sum != expected_value_l) passed = 1;
        }
    }
    else
    {
        if (byte_count == 8)
        {
            // Eight byte structs.
            if (is_b)
            {
                // This is one_double_struct
                one_double_struct s;
                double sum;

                expected_value_d = va_arg(ap, double);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, one_double_struct);
                    sum += s.one;
                }

                if (sum != expected_value_d) passed = 1;
            }
            else
            {
                // This is two_float_struct
                two_float_struct s;
                float sum;

                expected_value_f = va_arg(ap, double);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, two_float_struct);
                    sum += s.one + s.two;
                }

                if (sum != expected_value_f) passed = 1;
            }
        }
        else if (byte_count == 16)
        {
            // 16 byte structs.
            if (is_b)
            {
                // This is four_float_struct
                four_float_struct s;
                float sum;

                expected_value_f = va_arg(ap, double);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, four_float_struct);
                    sum += s.one + s.two + s.three + s.four;
                }

                if (sum != expected_value_f) passed = 1;
            }
            else
            {
                // This is two_double_struct
                two_double_struct s;
                double sum;

                expected_value_d = va_arg(ap, double);
                sum = 0;

                while (struct_count--) {
                    s = va_arg(ap, two_double_struct);
                    sum += s.one + s.two;
                }

                if (sum != expected_value_d) passed = 1;
            }
        }

        else if (byte_count == 32)
        {
            // This is four_double_struct
            four_double_struct s;
            double sum;

            expected_value_d = va_arg(ap, double);
            sum = 0;

            while (struct_count--) {
                s = va_arg(ap, four_double_struct);
                sum += s.one + s.two + s.three + s.four;
            }

            if (sum != expected_value_d) passed = 1;
        }
    }

    va_end(ap);
    return passed;
}

DLLEXPORT double _cdecl check_passing_four_three_double_struct(three_double_struct one, three_double_struct two, three_double_struct three, three_double_struct four, ...)
{
    double sum;

    sum = 0;

    sum += one.one + one.two + one.three;
    sum += two.one + two.two + two.three;
    sum += three.one + three.two + three.three;
    sum += four.one + four.two + four.three;

    return sum;
}

/*
    Args:
        count (int)             : count of args
        two_long_long_struct    : first value
        two_long_long_struct    : second value
        two_long_long_struct    : third value
        two_long_long_struct    : fourth value
*/
DLLEXPORT int _cdecl check_passing_four_sixteen_byte_structs(int count, ...)
{
    va_list ap;
    int passed, index;
    two_long_long_struct s;
    __int64 expected_value, calculated_value;

    passed = 0;
    calculated_value = 0;

    va_start(ap, count);

    expected_value = va_arg(ap, __int64);

    for (index = 0; index < 4; ++index) {
        s = va_arg(ap, two_long_long_struct);

        calculated_value += s.one + s.two;
    }

    va_end(ap);

    passed = expected_value == calculated_value ? 0 : 1;
    return passed;
}

DLLEXPORT char _cdecl echo_byte(char arg, ...)
{
    return arg;
}

DLLEXPORT char _cdecl echo_char(char arg, ...)
{
    return arg;
}

DLLEXPORT __int16 _cdecl echo_short(__int16 arg, ...)
{
    return arg;
}

DLLEXPORT __int32 _cdecl echo_int(__int32 arg, ...)
{
    return arg;
}

DLLEXPORT __int64 _cdecl echo_int64(__int64 arg, ...)
{
    return arg;
}

DLLEXPORT float _cdecl echo_float(float arg, ...)
{
    return arg;
}

DLLEXPORT double _cdecl echo_double(double arg, ...)
{
    return arg;
}

DLLEXPORT one_int_struct _cdecl echo_one_int_struct(one_int_struct arg, ...)
{
    return arg;
}

DLLEXPORT two_int_struct _cdecl echo_two_int_struct(two_int_struct arg, ...)
{
    return arg;
}

DLLEXPORT one_long_long_struct _cdecl echo_one_long_struct(one_long_long_struct arg, ...)
{
    return arg;
}

DLLEXPORT two_long_long_struct _cdecl echo_two_long_struct(two_long_long_struct arg, ...)
{
    return arg;
}

DLLEXPORT four_long_long_struct _cdecl echo_four_long_struct(four_long_long_struct arg)
{
    return arg;
}

DLLEXPORT four_long_long_struct _cdecl echo_four_long_struct_with_vararg(four_long_long_struct arg, ...)
{
    return arg;
}

DLLEXPORT eight_byte_struct _cdecl echo_eight_byte_struct(eight_byte_struct arg, ...)
{
    return arg;
}

DLLEXPORT four_int_struct _cdecl echo_four_int_struct(four_int_struct arg, ...)
{
    return arg;
}

DLLEXPORT sixteen_byte_struct _cdecl echo_sixteen_byte_struct(sixteen_byte_struct arg, ...)
{
    return arg;
}

DLLEXPORT one_float_struct _cdecl echo_one_float_struct(one_float_struct arg, ...)
{
    return arg;
}

DLLEXPORT two_float_struct _cdecl echo_two_float_struct(two_float_struct arg, ...)
{
    return arg;
}

DLLEXPORT one_double_struct _cdecl echo_one_double_struct(one_double_struct arg, ...)
{
    return arg;
}

DLLEXPORT two_double_struct _cdecl echo_two_double_struct(two_double_struct arg, ...)
{
    return arg;
}

DLLEXPORT three_double_struct _cdecl echo_three_double_struct(three_double_struct arg, ...)
{
    return arg;
}

DLLEXPORT four_float_struct _cdecl echo_four_float_struct(four_float_struct arg, ...)
{
    return arg;
}

DLLEXPORT four_double_struct _cdecl echo_four_double_struct(four_double_struct arg, ...)
{
    return arg;
}

DLLEXPORT __int8 _cdecl short_in_byte_out(__int16 arg, ...)
{
    return (__int8)arg;
}

DLLEXPORT __int16 _cdecl byte_in_short_out(__int8 arg, ...)
{
    return (__int16)arg;
}
