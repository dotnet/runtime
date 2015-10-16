// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509_name.h"

extern "C" int32_t GetX509NameStackFieldCount(STACK_OF(X509_NAME) * sk)
{
    return sk_X509_NAME_num(sk);
}

extern "C" X509_NAME* GetX509NameStackField(STACK_OF(X509_NAME) * sk, int32_t loc)
{
    return sk_X509_NAME_value(sk, loc);
}

extern "C" X509_NAME* DecodeX509Name(const unsigned char* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_X509_NAME(nullptr, &buf, len);
}

extern "C" STACK_OF(X509_NAME)* NewX509NameStack()
{
    return sk_X509_NAME_new_null();
}

extern "C" int32_t PushX509NameStackField(STACK_OF(X509_NAME)* stack, X509_NAME* x509Name)
{
    if (!stack)
    {
        return 0;
    }

    return sk_X509_NAME_push(stack, x509Name);
}

extern "C" void RecursiveFreeX509NameStack(STACK_OF(X509_NAME)* stack)
{
    sk_X509_NAME_pop_free(stack, X509_NAME_free);
}

extern "C" X509_NAME* DuplicateX509Name(X509_NAME* x509Name)
{
    return X509_NAME_dup(x509Name);
}
