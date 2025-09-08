#include "wasmString.h"
#include <stdio.h>

void printString(char* someString)
{
    printf("C file got string: 0x%p: %s\n", someString, someString);
    fflush(stdout);
}
