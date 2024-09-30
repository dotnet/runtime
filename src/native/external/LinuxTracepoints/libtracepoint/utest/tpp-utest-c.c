#define C_OR_CPP C
#include "tpp-utest.h"

#include <stdio.h>

int TestC(void)
{
    int err = TPP_REGISTER_PROVIDER(TestProvider);
    printf("TestProviderC register: %d\n", err);

    int ok = TestCommon();

    TPP_UNREGISTER_PROVIDER(TestProvider);
    return ok != 0 && err == 0;
}

void PrintErr(char const* operation, int err)
{
    printf("%s: %d\n", operation, err);
}

int main()
{
    TestC();
    TestCpp();
    return 0;
}
