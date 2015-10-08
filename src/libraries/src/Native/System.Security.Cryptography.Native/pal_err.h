// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdlib.h>
#include <stdint.h>

/*
Direct shim to ERR_get_error.
*/
extern "C" uint64_t ErrGetError();

/*
Direct shim to ERR_error_string_n.
*/
extern "C" void ErrErrorStringN(uint64_t e, char* buf, int32_t len);
