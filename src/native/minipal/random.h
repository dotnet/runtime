// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

void minipal_get_non_cryptographically_secure_random_bytes(uint8_t* buffer, int32_t bufferLength);
int32_t minipal_get_cryptographically_secure_random_bytes(uint8_t* buffer, int32_t bufferLength);


#ifdef __cplusplus
}
#endif // __cplusplus
