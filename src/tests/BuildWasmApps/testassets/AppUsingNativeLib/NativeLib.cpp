#include "NativeLib.h"
#include <stdio.h>

int print_line(int x)
{
    printf("print_line: %d\n", x);
    return 42 + x;
}
