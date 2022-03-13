#include <stdarg.h>

int sum(int n, ...)
{
    int result = 0;
    va_list ptr;
    va_start(ptr, n);

    for (int i = 0; i < n; i++)
        result += va_arg(ptr, int);

    va_end(ptr);
    return result;
}
