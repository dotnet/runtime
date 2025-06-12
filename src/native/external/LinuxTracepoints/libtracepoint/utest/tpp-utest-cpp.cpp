#define C_OR_CPP CPP
#include "tpp-utest.h"

#include <stdio.h>

int TestCpp(void)
{
    int err = TPP_REGISTER_PROVIDER(TestProvider);
    printf("TestProviderCpp register: %d\n", err);

    int ok = TestCommon();

    TPP_UNREGISTER_PROVIDER(TestProvider);
    return ok != 0 && err == 0;
}

#include <errno.h>
static_assert(EBADF == 9, "EBADF != 9");
