// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_networkframework.h"
#include <Foundation/Foundation.h>
#include <Network/Network.h>

static WriteCallback _writeFunc;
static ReadCallback _readFunc;
static StatusUpdateCallback _statusFunc;
static nw_protocol_definition_t _framerDefinition;
static nw_protocol_definition_t _tlsDefinition;
static dispatch_queue_t _tlsQueue;
static dispatch_queue_t _inputQueue;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunguarded-availability-new"

PALEXPORT nw_connection_t AppleCryptoNative_NwCreateContext(int32_t isServer)
{
    if (isServer != 0)  // the current implementation only supports client
        return NULL;

    // Network.framework requires an underlying network connection for TLS operations,
    // but we need to perform TLS without an actual connection. This implementation
    // uses a workaround: we create a dummy UDP connection that will never be used.
    // 
    // The trick works by layering a custom framer on top of this dummy connection,
    // then adding TLS on top of the framer. The framer intercepts the raw TLS data
    // and exposes it to SslStream, preventing it from ever reaching the underlying
    // connection.
    //
    // The endpoint values (127.0.0.1:42) are arbitrary - they just need to be
    // syntactically and semantically valid since the connection is never established.
    //
    // TODO: reuse parameters and endpoint across connections
    nw_parameters_t parameters = nw_parameters_create_secure_udp(NW_PARAMETERS_DISABLE_PROTOCOL, NW_PARAMETERS_DEFAULT_CONFIGURATION);
    nw_endpoint_t endpoint = nw_endpoint_create_host("127.0.0.1", "42");

    nw_connection_t connection = nw_connection_create(endpoint, parameters);

    // connection retains its own reference to the endpoint and parameters
    nw_release(endpoint);
    nw_release(parameters);

    return connection;
}

// This writes encrypted TLS frames to the safe handle. It is executed on NW Thread pool
static nw_framer_output_handler_t framer_output_handler = ^(nw_framer_t framer, nw_framer_message_t message, size_t message_length, bool is_complete)
{

    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4, watchOS 2.0, *))
    {
        nw_protocol_options_t framer_options = nw_framer_copy_options(framer);

        NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
        assert(num != NULL);

        void * ptr;
        [num getValue:&ptr];
        size_t size = message_length;

        nw_framer_parse_output(framer, 1, message_length, NULL, ^size_t(uint8_t *buffer, size_t buffer_length, bool is_complete2) {
            size_t length = buffer_length;
            (_writeFunc)(ptr, buffer, length);
            (void)is_complete2;
            (void)message;
            return buffer_length;
        });

        nw_release(framer_options);
    }
    else
    {
        assert(0);
    }
    (void)is_complete;
};

static nw_framer_stop_handler_t framer_stop_handler = ^bool(nw_framer_t framer) {
    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4, watchOS 8.4, *))
    {
        size_t state = 0;
        nw_protocol_options_t framer_options = nw_framer_copy_options(framer);
        NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
        if (num != NULL)
        {
            [num getValue:&state];
            (_statusFunc)(state, PAL_NwStatusUpdates_FramerStop, 0, 0);
        }

        nw_release(framer_options);
    }

    return TRUE;
};

static nw_framer_cleanup_handler_t framer_cleanup_handler = ^(nw_framer_t framer) {
    (void)framer;
};


// This is called when connection start to set up framer
static nw_framer_start_handler_t framer_start = ^nw_framer_start_result_t(nw_framer_t framer)
{
    assert(_statusFunc != NULL);
    size_t state = 0;

    nw_protocol_options_t framer_options = nw_framer_copy_options(framer);
    NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
    assert(num != NULL);

    [num getValue:&state];

    // Notify SafeHandle with framer instance so we can submit to it directly.
    (_statusFunc)(state, PAL_NwStatusUpdates_FramerStart, (size_t)framer, 0);

    nw_framer_set_output_handler(framer, framer_output_handler);

    nw_framer_set_stop_handler(framer, framer_stop_handler);
    nw_framer_set_cleanup_handler(framer, framer_cleanup_handler);

    nw_release(framer_options);

    return nw_framer_start_result_ready;
};


// this takes encrypted input from underlying stream and feeds it to nw_connection.
PALEXPORT int32_t AppleCryptoNative_NwProcessInputData(nw_connection_t connection, nw_framer_t framer, const uint8_t * buffer, int bufferLength)
{
    if (connection == NULL || framer == NULL)
    {
        return -1;
    }

    nw_framer_message_t message = nw_framer_message_create(framer);

    // There is race condition when connection can fail or be canceled and if it does we fail to create the message here.
    if (message == NULL)
    {
        return -1;
    }

    uint8_t * copy = NULL;
    if (bufferLength > 0)
    {
        copy = malloc((size_t)bufferLength);
        if (copy == NULL)
        {
            return -1;
        }
        memcpy(copy, buffer, bufferLength);
        nw_framer_message_set_value(message, "DATA", copy,  ^(void* ptr) {
            free(ptr);
        });
    }

    nw_framer_async(framer, ^(void) 
    {
        nw_framer_deliver_input(framer, copy, (size_t)bufferLength, message, bufferLength > 0 ? FALSE : TRUE);
        nw_release(message);
    });

    (void)connection;

    return 0;
}

// This starts TLS handshake. For client, it will produce ClientHello and call output handler (on thread pool)
// important part here is the state handler that will get asynchronous n=notifications about progress.
PALEXPORT int AppleCryptoNative_NwStartTlsHandshake(nw_connection_t connection, size_t state)
{
    if (connection == NULL)
        return -1;

    nw_retain(connection); // hold a reference until canceled
    nw_connection_set_state_changed_handler(connection, ^(nw_connection_state_t status, nw_error_t error) {
        int errorCode  = error ? nw_error_get_error_code(error) : 0;
        switch (status)
        {
            case nw_connection_state_preparing:
            case nw_connection_state_waiting:
            case nw_connection_state_failed:
            {
                if (errorCode != 0 || status == nw_connection_state_failed)
                {
                    (_statusFunc)(state, PAL_NwStatusUpdates_HandshakeFailed, (size_t)errorCode, 0);
                }
            }
            break;
            case nw_connection_state_ready:
            {
                (_statusFunc)(state, PAL_NwStatusUpdates_HandshakeFinished, 0, 0);
            }
            break;
            case nw_connection_state_cancelled:
            {
                (_statusFunc)(state, PAL_NwStatusUpdates_ConnectionCancelled, 0, 0);
                nw_release(connection); // release the reference we held
            }
            break;
            case nw_connection_state_invalid:
            {
                (_statusFunc)(state, PAL_NwStatusUpdates_UnknownError, 0, 0);
            }
            break;
        }
    });

    nw_connection_set_queue(connection, _tlsQueue);
    nw_connection_start(connection);

    return PAL_TlsHandshakeState_WouldBlock;
}

// This will start connection cleanup
PALEXPORT void AppleCryptoNative_NwCancelConnection(nw_connection_t connection)
{
    nw_connection_cancel(connection);
}

// this is used by encrypt. We write plain text to the connection and it will be handound out encrypted via output handler
PALEXPORT void AppleCryptoNative_NwSendToConnection(nw_connection_t connection,  size_t state,  uint8_t* buffer, int length)
{
    dispatch_data_t data = dispatch_data_create(buffer, (size_t)length, _inputQueue, ^{ printf("%s:%d: dispatch destructor called!!!\n", __func__, __LINE__);});

    nw_connection_send(connection, data, NW_CONNECTION_DEFAULT_MESSAGE_CONTEXT, FALSE, ^(nw_error_t  error) {
        if (error != NULL)
        {
            int errorCode  = nw_error_get_error_code(error);
            (_statusFunc)(state, PAL_NwStatusUpdates_ConnectionWriteFailed, (size_t)errorCode, 0);
        }
        else
        {
            (_statusFunc)(state, PAL_NwStatusUpdates_ConnectionWriteFinished, 0, 0);
        }
     });
}

// This is used by decrypt. We feed data in via AppleCryptoNative_NwProcessInputData and we try to read from the connection.
PALEXPORT void AppleCryptoNative_NwReadFromConnection(nw_connection_t connection, size_t state)
{
    nw_connection_receive(connection, 1, 65536, ^(dispatch_data_t content, nw_content_context_t context, bool is_complete, nw_error_t error) {
        int errorCode = error ? nw_error_get_error_code(error) : 0;

        if (error != NULL)
        {
            errorCode  = nw_error_get_error_code(error);
            return;
        }

        if (content != NULL)
        {
            const void *buffer;
            size_t bufferLength;
            dispatch_data_t tmp = dispatch_data_create_map(content, &buffer, &bufferLength);
            (_statusFunc)(state, PAL_NwStatusUpdates_ConnectionReadFinished, bufferLength, (size_t)buffer);
            dispatch_release(tmp);
         }

         if (is_complete || content == NULL)
         {
             (_statusFunc)(state, PAL_NwStatusUpdates_ConnectionReadFinished, 0, 0);
         }
        (void)context;
    });
}

static tls_protocol_version_t PalSslProtocolToTlsProtocolVersion(PAL_SslProtocol palProtocolId)
{
    switch (palProtocolId)
    {
        case PAL_SslProtocol_Tls13:
            return tls_protocol_version_TLSv13;
        case PAL_SslProtocol_Tls12:
            return tls_protocol_version_TLSv12;
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        case PAL_SslProtocol_Tls11:
            return tls_protocol_version_TLSv11;
        case PAL_SslProtocol_Tls10:
            return tls_protocol_version_TLSv10;
        case tls_protocol_version_DTLSv10:
        default:
            break;
#pragma clang diagnostic pop
    }

    return (tls_protocol_version_t)0;
}

// This configures TLS properties
PALEXPORT void AppleCryptoNative_NwSetTlsOptions(nw_connection_t connection, size_t state, char* targetName, const uint8_t * alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol)
{
    nw_protocol_options_t tls_options = nw_tls_create_options();
    sec_protocol_options_t sec_options = nw_tls_copy_sec_protocol_options(tls_options);
    if (targetName != NULL)
    {
        sec_protocol_options_set_tls_server_name(sec_options, targetName);
    }

    tls_protocol_version_t version = PalSslProtocolToTlsProtocolVersion(minTlsProtocol);
    if ((int)version != 0)
    {
        sec_protocol_options_set_min_tls_protocol_version(sec_options, version);
    }
    version = PalSslProtocolToTlsProtocolVersion(maxTlsProtocol);
    if ((int)version != 0)
    {
        sec_protocol_options_set_max_tls_protocol_version(sec_options, version);
    }

    if (alpnBuffer != NULL)
    {
        int offset = 0;
        while (offset < alpnLength)
        {
            uint8_t length = alpnBuffer[offset];
            sec_protocol_options_add_tls_application_protocol(sec_options, (const char*) &alpnBuffer[offset + 1]);
            offset += length + 2;
        }
    }

    // we accept all certificates here and we will do validation later
    sec_protocol_options_set_verify_block(sec_options, ^(sec_protocol_metadata_t metadata, sec_trust_t trust_ref, sec_protocol_verify_complete_t complete) {
        (void)metadata;
        (void)trust_ref;
        complete(true);
    }, _tlsQueue);

    nw_release(sec_options);

    nw_protocol_options_t framer_options = nw_framer_create_options(_framerDefinition);
    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        NSNumber *ref = [NSNumber numberWithLong:(long)state];
        nw_framer_options_set_object_value(framer_options, "GCHANDLE", ref);
    }

    nw_parameters_t parameters = nw_connection_copy_parameters(connection);
    nw_protocol_stack_t protocol_stack = nw_parameters_copy_default_protocol_stack(parameters);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, framer_options);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, tls_options);

    nw_release(framer_options);
    nw_release(protocol_stack);
    nw_release(tls_options);
}

// This wil get TLS details after handshake is finished
PALEXPORT int32_t AppleCryptoNative_NwGetConnectionInfo(nw_connection_t connection, PAL_SslProtocol* protocol, uint16_t* pCipherSuiteOut, const char** negotiatedAlpn, int32_t* negotiatedAlpnLength)
{
    nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);
    if (meta != NULL)
    {
        sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);
        const char* alpn = sec_protocol_metadata_get_negotiated_protocol(secMeta);
        if (alpn != NULL)
        {
            *negotiatedAlpn = alpn;
            *negotiatedAlpnLength = (int32_t)strlen(alpn);
        }
        else
        {
            *negotiatedAlpn = NULL;
            *negotiatedAlpnLength = 0;
        }

        tls_protocol_version_t version = sec_protocol_metadata_get_negotiated_tls_protocol_version(secMeta);
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        switch (version)
        {
            case tls_protocol_version_TLSv10:
                *protocol = PAL_SslProtocol_Tls10;
                break;
            case tls_protocol_version_TLSv11:
                *protocol = PAL_SslProtocol_Tls11;
                break;
           case tls_protocol_version_TLSv12:
                *protocol = PAL_SslProtocol_Tls12;
                break;
           case tls_protocol_version_TLSv13:
                *protocol = PAL_SslProtocol_Tls13;
                break;
           case tls_protocol_version_DTLSv10:
           case tls_protocol_version_DTLSv12:
           default:
                *protocol = PAL_SslProtocol_None;
                break;
        }
#pragma clang diagnostic pop

        *pCipherSuiteOut = sec_protocol_metadata_get_negotiated_tls_ciphersuite(secMeta);

        nw_release(meta);
        nw_release(secMeta);

        return 0;
    }

    return -1;
}

PALEXPORT void AppleCryptoNative_NwCopyCertChain(nw_connection_t connection, CFArrayRef* certificates, int* certificateCount)
{
    CFMutableArrayRef certs = NULL;
    __block int count = 0;

    nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);
    if (meta != NULL)
    {
        sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);
        if (secMeta != NULL)
        {
            sec_protocol_metadata_access_peer_certificate_chain(secMeta, ^(sec_certificate_t certificate) {
                count++;
                (void*)certificate;
            });

            if (count > 0)
            {
                certs = CFArrayCreateMutable(NULL, count, &kCFTypeArrayCallBacks);
                
                sec_protocol_metadata_access_peer_certificate_chain(secMeta, ^(sec_certificate_t certificate) {
                    CFArrayAppendValue(certs, sec_certificate_copy_ref(certificate));
                });
            }

            sec_release(secMeta);
        }

        nw_release(meta);
    }

    if (certs != NULL)
    {
        *certificateCount = (int)CFArrayGetCount(certs);
        *certificates = (CFArrayRef)certs;
    }
    else
    {
        *certificateCount = 0;
        *certificates = NULL;
    }
}
#pragma clang diagnostic pop

// this is called once to set everything up
PALEXPORT int32_t AppleCryptoNative_NwInit(StatusUpdateCallback statusFunc, ReadCallback readFunc, WriteCallback writeFunc)
{
    assert(statusFunc != NULL);
    assert(writeFunc != NULL);
    assert(readFunc != NULL);

    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        _writeFunc = writeFunc;
        _readFunc = readFunc;
        _statusFunc = statusFunc;
        _framerDefinition = nw_framer_create_definition("com.dotnet.networkframework.tlsframer",
            NW_FRAMER_CREATE_FLAGS_DEFAULT, framer_start);
        _tlsDefinition = nw_protocol_copy_tls_definition();
        _tlsQueue = dispatch_queue_create("com.dotnet.networkframework.tlsqueue", NULL);
        _inputQueue = dispatch_queue_create("com.dotnet.networkframework.inputqueue", NULL);

        return 0;
   }

   return 1;
}
