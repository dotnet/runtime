// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Network.framework requires an underlying network connection for TLS operations,
// but we need to perform TLS without an actual connection so that we can expose
// it as the SslStream abstraction. This implementation uses a workaround: we
// create a dummy UDP connection that will never be used.
// 
// The trick works by layering a custom framer on top of this dummy connection,
// then adding TLS on top of the framer. The framer intercepts the raw TLS data
// and exposes it to SslStream, preventing it from ever reaching the underlying
// connection.
//

#include "pal_networkframework.h"
#include <Foundation/Foundation.h>
#include <Network/Network.h>
#include <Security/Security.h>

static WriteCallback _writeFunc;
static StatusUpdateCallback _statusFunc;
static ChallengeCallback _challengeFunc;
static nw_protocol_definition_t _framerDefinition;
static nw_protocol_definition_t _tlsDefinition;
static dispatch_queue_t _tlsQueue;
static dispatch_queue_t _inputQueue;
static nw_endpoint_t _endpoint;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunguarded-availability-new"

#define LOG_IMPL_(context, isError, ...) \
    do { \
        char buff[256]; \
        snprintf(buff, sizeof(buff), __VA_ARGS__); \
        _statusFunc(context, PAL_NwStatusUpdates_DebugLog, (size_t)(buff), (size_t)(isError), NULL); \
    } while (0)

#ifdef DEBUG
#define LOG_INFO(context, ...) LOG_IMPL_(context, 0, __VA_ARGS__)
#else
#define LOG_INFO(context, ...) do { (void)context; } while (0)
#endif

#define LOG_ERROR(context, ...) LOG_IMPL_(context, 1, __VA_ARGS__)

#define MANAGED_CONTEXT_KEY "GCHANDLE"

static void* FramerGetManagedContext(nw_framer_t framer)
{
    void* ptr = NULL;

    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4, watchOS 8.4, *))
    {
        nw_protocol_options_t framer_options = nw_framer_copy_options(framer);
        assert(framer_options != NULL);

        NSNumber* num = nw_framer_options_copy_object_value(framer_options, MANAGED_CONTEXT_KEY);
        assert(num != NULL);
        [num getValue:&ptr];
        [num release];

        nw_release(framer_options);
    }

    return ptr;
}

static void FramerOptionsSetManagedContext(nw_protocol_options_t framer_options, void* context)
{
    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        NSNumber *ref = [NSNumber numberWithLong:(long)context];
        nw_framer_options_set_object_value(framer_options, MANAGED_CONTEXT_KEY, ref);
        [ref release];
    }
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


// Helper function to extract error information from nw_error_t
// Returns CFStringRef that needs to be released after use, or NULL if no CFString was created
static CFStringRef ExtractNetworkFrameworkError(nw_error_t error, PAL_NetworkFrameworkError* outError)
{
    if (error == NULL || outError == NULL)
    {
        if (outError != NULL)
        {
            outError->errorCode = 0;
            outError->errorDomain = 0;
            outError->errorMessage = NULL;
        }
        return NULL;
    }
    
    outError->errorCode = nw_error_get_error_code(error);
    nw_error_domain_t domain = nw_error_get_error_domain(error);
    outError->errorDomain = (int32_t)domain;

    if (domain == nw_error_domain_posix)
    {
        outError->errorMessage = strerror(outError->errorCode);
        return NULL;
    }
    
    // Get error message from CoreFoundation error if available
    CFStringRef descriptionToRelease = NULL;
    CFErrorRef cfError = nw_error_copy_cf_error(error);
    if (cfError != NULL)
    {
        CFStringRef description = CFErrorCopyDescription(cfError);
        if (description != NULL)
        {
            outError->errorMessage = CFStringGetCStringPtr(description, kCFStringEncodingUTF8);
            if (outError->errorMessage == NULL)
            {
                // If direct pointer access fails, we'll leave it as NULL
                CFRelease(description);
            }
            else
            {
                // We got a direct pointer, so we need to keep the CFString alive
                descriptionToRelease = description;
            }
        }
        CFRelease(cfError);
    }
    else
    {
        outError->errorMessage = NULL;
    }
    
    return descriptionToRelease;
}

PALEXPORT nw_connection_t AppleCryptoNative_NwConnectionCreate(int32_t isServer, void* context, char* targetName, const uint8_t * alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol, uint32_t* cipherSuites, int cipherSuitesLength)
{
    if (isServer != 0)  // the current implementation only supports client
        return NULL;

    nw_parameters_t parameters = nw_parameters_create_secure_udp(NW_PARAMETERS_DISABLE_PROTOCOL, NW_PARAMETERS_DEFAULT_CONFIGURATION);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunreachable-code"
    //return connection;

    nw_protocol_options_t tls_options = nw_tls_create_options();
    sec_protocol_options_t sec_options = nw_tls_copy_sec_protocol_options(tls_options);
    if (targetName != NULL)
    {
        sec_protocol_options_set_tls_server_name(sec_options, targetName);
    }

    tls_protocol_version_t version = PalSslProtocolToTlsProtocolVersion(minTlsProtocol);
    if ((int)version != 0)
    {
        LOG_INFO(context, "Min TLS version: %d", version);
        sec_protocol_options_set_min_tls_protocol_version(sec_options, version);
    }

    version = PalSslProtocolToTlsProtocolVersion(maxTlsProtocol);
    if ((int)version != 0)
    {
        LOG_INFO(context, "Max TLS version: %d", version);
        sec_protocol_options_set_max_tls_protocol_version(sec_options, version);
    }

    if (alpnBuffer != NULL)
    {
        int offset = 0;
        while (offset < alpnLength)
        {
            uint8_t length = alpnBuffer[offset];
            const char* alpn = (const char*) &alpnBuffer[offset + 1];
            LOG_INFO(context, "Appending ALPN: %s", alpn);
            sec_protocol_options_add_tls_application_protocol(sec_options, alpn);
            offset += length + 2;
        }
    }

    if (cipherSuites != NULL && cipherSuitesLength > 0)
    {
        for (int i = 0; i < cipherSuitesLength; i++)
        {
            uint16_t cipherSuite = (uint16_t)cipherSuites[i];
            LOG_INFO(context, "Appending cipher suite: 0x%04x", cipherSuite);
            sec_protocol_options_append_tls_ciphersuite(sec_options, cipherSuite);
        }
    }

    // Set up challenge block to detect when server requests client certificate
    sec_protocol_options_set_challenge_block(sec_options, ^(sec_protocol_metadata_t metadata, sec_protocol_challenge_complete_t complete)
    {
        // Extract acceptable issuers from metadata
        CFMutableArrayRef acceptableIssuers = NULL;
        
        if (metadata != NULL)
        {
            // Create array to hold distinguished names
            acceptableIssuers = CFArrayCreateMutable(NULL, 0, &kCFTypeArrayCallBacks);
            
            // Access distinguished names from the metadata
            sec_protocol_metadata_access_distinguished_names(metadata, ^(dispatch_data_t dn)
            {
                // Convert dispatch_data to CFData
                const void* dnBytes = NULL;
                size_t dnLength = 0;
                dispatch_data_t contiguousDN = dispatch_data_create_map(dn, &dnBytes, &dnLength);
                
                if (dnBytes != NULL && dnLength > 0)
                {   
                    CFDataRef dnData = CFDataCreate(NULL, (const UInt8*)dnBytes, (CFIndex)dnLength);
                    if (dnData != NULL)
                    {
                        CFArrayAppendValue(acceptableIssuers, dnData);
                        CFRelease(dnData);
                    }
                }
                
                if (contiguousDN != NULL)
                {
                    dispatch_release(contiguousDN);
                }
            });
        }
        
        // Call the managed callback to get the client identity
        void* identity = _challengeFunc(context, acceptableIssuers);
        
        // Clean up
        CFRelease(acceptableIssuers);
        
        if (identity != NULL)
        {
            // Convert to sec_identity_t and set it
            SecIdentityRef secIdentityRef = (SecIdentityRef)identity;
            sec_identity_t sec_identity = sec_identity_create(secIdentityRef);
            if (sec_identity != NULL)
            {
                complete(sec_identity);
                sec_release(sec_identity);
            }

            return;
        }
        
        complete(NULL);
    }, _tlsQueue);

    // we accept all certificates here and we will do validation later
    sec_protocol_options_set_verify_block(sec_options, ^(sec_protocol_metadata_t metadata, sec_trust_t trust_ref, sec_protocol_verify_complete_t complete)
    {
        LOG_INFO(context, "Cert validation callback called");

        SecTrustRef chain = sec_trust_copy_ref(trust_ref);

        _statusFunc(context, PAL_NwStatusUpdates_CertificateAvailable, (size_t)chain, 0, NULL);

        (void)metadata;
        (void)trust_ref;
        complete(true);
    }, _tlsQueue);

    nw_release(sec_options);

    nw_protocol_options_t framer_options = nw_framer_create_options(_framerDefinition);
    FramerOptionsSetManagedContext(framer_options, context);

    nw_protocol_stack_t protocol_stack = nw_parameters_copy_default_protocol_stack(parameters);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, framer_options);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, tls_options);

    nw_release(framer_options);
    nw_release(protocol_stack);
    nw_release(tls_options);

    nw_connection_t connection = nw_connection_create(_endpoint, parameters);

    nw_release(parameters);

    if (connection == NULL)
    {
        LOG_ERROR(context, "Failed to create Network Framework connection");
        return NULL;
    }

    return connection;
}

// This writes encrypted TLS frames to the safe handle. It is executed on NW Thread pool
static nw_framer_output_handler_t framer_output_handler = ^(nw_framer_t framer, nw_framer_message_t message, size_t message_length, bool is_complete)
{
    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4, watchOS 2.0, *))
    {
        void* context = FramerGetManagedContext(framer);
        size_t size = message_length;

        nw_framer_parse_output(framer, 1, message_length, NULL, ^size_t(uint8_t *buffer, size_t buffer_length, bool is_complete2)
        {
            (_writeFunc)(context, buffer, buffer_length);
            (void)is_complete2;
            (void)message;
            return buffer_length;
        });
    }
    else
    {
        assert(0);
    }
    (void)is_complete;
};

static nw_framer_stop_handler_t framer_stop_handler = ^bool(nw_framer_t framer)
{
    (void)framer;
    return TRUE;
};

static nw_framer_cleanup_handler_t framer_cleanup_handler = ^(nw_framer_t framer)
{
    (void)framer;
};

// This is called when connection start to set up framer
static nw_framer_start_handler_t framer_start = ^nw_framer_start_result_t(nw_framer_t framer)
{
    assert(_statusFunc != NULL);

    void* context = FramerGetManagedContext(framer);

    // Notify managed code with framer reference so we can submit to it directly.
    (_statusFunc)(context, PAL_NwStatusUpdates_FramerStart, (size_t)framer, 0, NULL);

    nw_framer_set_output_handler(framer, framer_output_handler);

    nw_framer_set_stop_handler(framer, framer_stop_handler);
    nw_framer_set_cleanup_handler(framer, framer_cleanup_handler);

    return nw_framer_start_result_ready;
};


// this takes encrypted input from underlying stream and feeds it to nw_connection.
PALEXPORT int32_t AppleCryptoNative_NwFramerDeliverInput(nw_framer_t framer, void* context, const uint8_t* buffer, int bufferLength, CompletionCallback completionCallback)
{
    assert(framer != NULL);
    if (framer == NULL)
    {
        LOG_ERROR(context, "NwFramerDeliverInput called with NULL framer");
        return -1;
    }

    nw_framer_message_t message = nw_framer_message_create(framer);

    // There is a race condition when connection can fail or be canceled and if it does we fail to create the message here.
    if (message == NULL)
    {
        LOG_ERROR(context, "NwFramerDeliverInput failed to create message");
        return -1;
    }

    nw_framer_async(framer, ^(void) 
    {
        nw_framer_deliver_input(framer, buffer, (size_t)bufferLength, message, bufferLength > 0 ? FALSE : TRUE);
        completionCallback(context, NULL);
        nw_release(message);
    });

    return 0;
}

// This starts TLS handshake. For client, it will produce ClientHello and call output handler (on thread pool)
// important part here is the context handler that will get asynchronous notifications about progress.
PALEXPORT int AppleCryptoNative_NwConnectionStart(nw_connection_t connection, void* context)
{
    if (connection == NULL)
    {
        LOG_ERROR(context, "NwConnectionStart called with NULL connection");
        return -1;
    }

    nw_connection_set_state_changed_handler(connection, ^(nw_connection_state_t status, nw_error_t error)
    {
        PAL_NetworkFrameworkError errorInfo;
        CFStringRef cfStringToRelease = ExtractNetworkFrameworkError(error, &errorInfo);
        LOG_INFO(context, "Connection context changed: %d, errorCode: %d", (int)status, errorInfo.errorCode);
        switch (status)
        {
            case nw_connection_state_preparing:
            case nw_connection_state_waiting:
            case nw_connection_state_failed:
            {
                if (errorInfo.errorCode != 0 || status == nw_connection_state_failed)
                {
                    (_statusFunc)(context, PAL_NwStatusUpdates_ConnectionFailed, 0, 0, &errorInfo);
                }
            }
            break;
            case nw_connection_state_ready:
            {
                (_statusFunc)(context, PAL_NwStatusUpdates_HandshakeFinished, 0, 0, NULL);
            }
            break;
            case nw_connection_state_cancelled:
            {
                (_statusFunc)(context, PAL_NwStatusUpdates_ConnectionCancelled, 0, 0, NULL);
            }
            break;
            case nw_connection_state_invalid:
            {
                (_statusFunc)(context, PAL_NwStatusUpdates_UnknownError, 0, 0, NULL);
            }
            break;
        }
        
        // Release CFString if we created one
        if (cfStringToRelease != NULL)
        {
            CFRelease(cfStringToRelease);
        }
    });

    nw_connection_set_queue(connection, _tlsQueue);
    nw_connection_start(connection);

    return 0;
}

// This will start connection cleanup
PALEXPORT void AppleCryptoNative_NwConnectionCancel(nw_connection_t connection)
{
    nw_connection_cancel(connection);
}

// this is used by encrypt. We write plain text to the connection and it will be handound out encrypted via output handler
PALEXPORT void AppleCryptoNative_NwConnectionSend(nw_connection_t connection, void* context, uint8_t* buffer, int length, CompletionCallback completionCallback)
{
    dispatch_data_t data = dispatch_data_create(buffer, (size_t)length, _inputQueue, ^()
    {
        // we specify empty destructor instead of DISPATCH_DATA_DESTRUCTOR_DEFAULT to avoid creating
        // an internal copy of the data. The caller ensures the buffer is valid until we call completionCallback.
    });

    nw_connection_send(connection, data, NW_CONNECTION_DEFAULT_MESSAGE_CONTEXT, FALSE, ^(nw_error_t error)
    {
        PAL_NetworkFrameworkError errorInfo;
        CFStringRef cfStringToRelease = ExtractNetworkFrameworkError(error, &errorInfo);
        completionCallback(context, error != NULL ? &errorInfo : NULL);
        
        // Release CFString if we created one
        if (cfStringToRelease != NULL)
        {
            CFRelease(cfStringToRelease);
        }
    });
    
    // Release our reference to dispatch_data - nw_connection_send retains it
    dispatch_release(data);
}

// This is used by decrypt. We feed data in via AppleCryptoNative_NwProcessInputData and we try to read from the connection.
PALEXPORT void AppleCryptoNative_NwConnectionReceive(nw_connection_t connection, void* context, uint32_t length, ReadCompletionCallback readCompletionCallback)
{
    nw_connection_receive(connection, 0, length, ^(dispatch_data_t content, nw_content_context_t ctx, bool is_complete, nw_error_t error)
    {
        PAL_NetworkFrameworkError errorInfo;

        if (error != NULL)
        {
            CFStringRef cfStringToRelease = ExtractNetworkFrameworkError(error, &errorInfo);
            readCompletionCallback(context, &errorInfo, NULL, 0);

            // Release CFString if we created one
            if (cfStringToRelease != NULL)
            {
                CFRelease(cfStringToRelease);
            }
            return;
        }

        if (content != NULL)
        {
            const void *buffer;
            size_t bufferLength;
            dispatch_data_t tmp = dispatch_data_create_map(content, &buffer, &bufferLength);
            readCompletionCallback(context, NULL, (const uint8_t*)buffer, bufferLength);
            dispatch_release(tmp);
            return;
         }

         if (is_complete || content == NULL)
         {
            readCompletionCallback(context, NULL, NULL, 0);
            return;
         }

        (void)ctx;
    });
}

// This wil get TLS details after handshake is finished
PALEXPORT int32_t AppleCryptoNative_GetConnectionInfo(nw_connection_t connection, void* context, PAL_SslProtocol* protocol, uint16_t* pCipherSuiteOut, char* negotiatedAlpn, int32_t* negotiatedAlpnLength)
{
    nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);

    if (meta == NULL)
    {
        LOG_ERROR(context, "nw_connection_copy_protocol_metadata returned null");
        return -1;
    }

    sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);

    const char* alpn = sec_protocol_metadata_get_negotiated_protocol(secMeta);
    if (alpn != NULL)
    {
        strcpy(negotiatedAlpn, alpn);
        *negotiatedAlpnLength = (int32_t)strlen(alpn);
    }
    else
    {
        negotiatedAlpn[0] = '\0';
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
    sec_release(secMeta);
    return 0;
}

// this is called once to set everything up
PALEXPORT int32_t AppleCryptoNative_Init(StatusUpdateCallback statusFunc, WriteCallback writeFunc, ChallengeCallback challengeFunc)
{
    assert(statusFunc != NULL);
    assert(writeFunc != NULL);

    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        _writeFunc = writeFunc;
        _statusFunc = statusFunc;
        _challengeFunc = challengeFunc;
        _framerDefinition = nw_framer_create_definition("com.dotnet.networkframework.tlsframer",
            NW_FRAMER_CREATE_FLAGS_DEFAULT, framer_start);
        _tlsDefinition = nw_protocol_copy_tls_definition();
        _tlsQueue = dispatch_queue_create("com.dotnet.networkframework.tlsqueue", NULL);
        _inputQueue = _tlsQueue;

        // The endpoint values (127.0.0.1:42) are arbitrary - they just need to be
        // syntactically and semantically valid since the connection is never established.
        _endpoint = nw_endpoint_create_host("127.0.0.1", "42");

        return 0;
   }

   return 1;
}
