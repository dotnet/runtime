#include <stdio.h>

int ManagedFunc(int number);

void UnmanagedFunc()
{
    int ret = 0;
    printf("UnmanagedFunc calling ManagedFunc\n");
    ret = ManagedFunc(123);
    printf("ManagedFunc returned %d\n", ret);
}


#ifdef PARSING_FUNCTION_POINTERS_IS_SUPPORTED
/*
 warning WASM0001: Could not get pinvoke, or callbacks for method 'Test::ReferenceFuncPtr' because 'Parsing function pointe
r types in signatures is not supported.'
warning WASM0001: Skipping pinvoke 'Test::ReferenceFuncPtr' because 'Parsing function pointer types in signatures is not s
upported.'
*/
typedef int (*ManagedFuncPtr)(int);

void ReferenceFuncPtr(ManagedFuncPtr funcPtr) {
    printf("ReferenceFuncPtr %p\n", funcPtr);
}
#endif