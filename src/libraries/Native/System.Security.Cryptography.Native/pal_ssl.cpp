// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_ssl.h"
#include "pal_crypto_config.h"

extern "C" const SSL_METHOD* SslV2_3Method()
{
    return SSLv23_method();
}

extern "C" const SSL_METHOD* SslV3Method()
{
    return SSLv3_method();
}

extern "C" const SSL_METHOD* TlsV1Method()
{
    return TLSv1_method();
}

extern "C" const SSL_METHOD* TlsV1_1Method()
{
#if HAVE_TLS_V1_1
    return TLSv1_1_method();
#else
    return nullptr;
#endif
}

extern "C" const SSL_METHOD* TlsV1_2Method()
{
#if HAVE_TLS_V1_2
    return TLSv1_2_method();
#else
    return nullptr;
#endif
}

extern "C" SSL_CTX* SslCtxCreate(SSL_METHOD* method)
{
    return SSL_CTX_new(method);
}

extern "C" int32_t SslWrite(SSL* ssl, const void* buf, int32_t num)
{
    return SSL_write(ssl, buf, num);
}

extern "C" int32_t SslRead(SSL* ssl, void* buf, int32_t num)
{
    return SSL_read(ssl, buf, num);
}
