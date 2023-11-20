#include <stdio.h>

int ManagedFunc(int number);

void UnmanagedFunc()
{
    int ret = 0;
    printf("UnmanagedFunc calling ManagedFunc\n");
    ret = ManagedFunc(123);
    printf("ManagedFunc returned %d\n", ret);
}