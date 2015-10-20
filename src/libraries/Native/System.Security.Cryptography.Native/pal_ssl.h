// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/ssl.h>

/*
Shims the SSLv23_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* SslV2_3Method();

/*
Shims the SSLv3_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* SslV3Method();

/*
Shims the TLSv1_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1Method();

/*
Shims the TLSv1_1_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1_1Method();

/*
Shims the TLSv1_2_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1_2Method();

/*
Shims the SSL_CTX_new method.

Returns the new SSL_CTX instance.
*/
extern "C" SSL_CTX* SslCtxCreate(SSL_METHOD* method);

/*
Shims the SSL_CTX_ctrl method.

The return value of the SSL_CTX_ctrl() function depends on the command supplied via the cmd parameter.
*/
extern "C" int64_t SslCtxCtrl(SSL_CTX* ctx, int32_t cmd, int64_t larg, void* parg);

/*
Shims the SSL_new method.

Returns the new SSL instance.
*/
extern "C" SSL* SslCreate(SSL_CTX* ctx);

/*
Shims the SSL_get_error method.

Returns the error code for the specified result.
*/
extern "C" int32_t SslGetError(SSL* ssl, int32_t ret);

/*
Cleans up and deletes an SSL instance.

Implemented by calling SSL_free.

No-op if ssl is null.
The given X509 SSL is invalid after this call.
Always succeeds.
*/
extern "C" void SslDestroy(SSL* ssl);

/*
Cleans up and deletes an SSL_CTX instance.

Implemented by calling SSL_CTX_free.

No-op if ctx is null.
The given X509 SSL_CTX is invalid after this call.
Always succeeds.
*/
extern "C" void SslCtxDestroy(SSL_CTX* ctx);

/*
Shims the SSL_write method.

Returns the positive number of bytes written when successful, 0 or a negative number
when an error is encountered.
*/
extern "C" int32_t SslWrite(SSL* ssl, const void* buf, int32_t num);

/*
Shims the SSL_read method.

Returns the positive number of bytes read when successful, 0 or a negative number
when an error is encountered.
*/
extern "C" int32_t SslRead(SSL* ssl, void* buf, int32_t num);
