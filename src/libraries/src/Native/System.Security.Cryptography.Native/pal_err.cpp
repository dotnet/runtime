// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_err.h"

#include <openssl/err.h>

extern "C" uint64_t ErrGetError()
{
    return ERR_get_error();
}

extern "C" void ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    ERR_error_string_n(e, buf, static_cast<size_t>(len));
}
