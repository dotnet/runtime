// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"
#include <sys/socket.h>
#include <netinet/in.h>

#define _DARWIN_C_SOURCE 1
#include <pthread.h>
//#include <pthread_np.h>

#include <Foundation/Foundation.h>

static SSLWriteFunc _writeFunc;
static SSLReadFunc _readFunc;
static SslStatusUpdateFunc _statusFunc;
static nw_protocol_definition_t _framerDefinition;
static nw_protocol_definition_t _tlsDefinition;
static dispatch_queue_t _tlsQueue;
static dispatch_queue_t _inputQueue;

static uint64_t GetThreadId(void)
{
    uint64_t tid;
    pthread_threadid_np(NULL, &tid);
    return tid;
}

nw_connection_t AppleCryptoNative_NwCreateContext(int32_t isServer, size_t gcHandle)
{
    struct sockaddr_in6  socketAddress = { .sin6_family = AF_INET6, .sin6_port = 42 };

    if (isServer != 0)  // only client supported at this point.
        return NULL;

    nw_parameters_t nw_parameters = nw_parameters_create_secure_udp(NW_PARAMETERS_DISABLE_PROTOCOL, NW_PARAMETERS_DEFAULT_CONFIGURATION);

    // store GCHandle to fields in sockaddr_in6 and attach it as EndPoint. 
    // This can be revisited if we find better way how to attach arbitrary data to connection.
    socketAddress.sin6_flowinfo - (uint32_t)(gcHandle);
    socketAddress.sin6_scope_id =  (uint32_t)(gcHandle >> 32);
    socketAddress.sin6_addr.__u6_addr.__u6_addr8[15] =  1;
    socketAddress.sin6_port = htons(42);

    //upper = (uint32_t)(gcHandle >> 32);
    //ower = (uint32_t)(gcHandle);
    // printf("%s:%d: nw_connection_create lower = 0x%x and upper = 0x%x\n", __func__, __LINE__, upper, lower);
     //socketAddress.sin6_flowinfo = (uint32_t)(gcHandle >> 32);
     //socketAddress.sin6_scope_id = (uint32_t)(gcHandle);


    socketAddress.sin6_len = (uint8_t)sizeof(struct sockaddr_in6);

    //nw_endpoint_t nw_endpoint = nw_endpoint_create_host("127.0.0.1", "42");
    nw_endpoint_t nw_endpoint = nw_endpoint_create_address((struct sockaddr*)&socketAddress);

    printf("%s:%d: nw_connection_create endpoint is %p %zu with 0x%0x 0x%0x\n", __func__, __LINE__, (void*)nw_endpoint, gcHandle, socketAddress.sin6_flowinfo,  socketAddress.sin6_scope_id);
    if (nw_endpoint == NULL)
    {
//        nw_endpoint = nw_endpoint_create_host("::1", "42");
    }


    nw_connection_t connection = nw_connection_create(nw_endpoint, nw_parameters);
    printf("%s:%d: nw_connection_create is %p\n", __func__, __LINE__, (void*)connection);

    return connection;
}

static nw_framer_output_handler_t framer_output_handler = ^(nw_framer_t framer, nw_framer_message_t message, size_t message_length, bool is_complete)
{

    nw_protocol_options_t framer_options;
//    NSValue* val = NULL;


    if (__builtin_available(macOS 12.3, iOS 9.0, tvOS 9.0, watchOS 2.0, *))
    {
        framer_options = nw_framer_copy_options(framer);
//         NSValue* val = nw_framer_options_copy_object_value(framer_options, "BOO");
        NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
        if (num == NULL)
        {
            printf("WTF NULKL\n");
        }
        else
        {
            void * ptr;
           [num getValue:&ptr]; 
           printf("%s:%d:Got number %p WRITING %zu to connection framer %p \n", __func__, __LINE__, ptr, message_length, (void*)framer);

           size_t size = message_length;

           nw_framer_parse_output(framer, 1, message_length, NULL, ^size_t(uint8_t *buffer, size_t buffer_length, bool is_complete2) {
                printf("PARSE OUTOPUT CALLED WITH %ld %ld %p complete %d\n", message_length, buffer_length, (void*)buffer, is_complete2);
//                       dump_hex(buffer, buffer_length);

               size_t length = buffer_length;
               (_writeFunc)(ptr, buffer, &length);
               printf("%s:%d: _writeFunc e≈ecuted with %zu and returned %zu\n", __func__, __LINE__, buffer_length, length); 
               return buffer_length;
           });

        }
    }
    else
    {
        printf("WTF !!!!! old mac????\n");
    }

//    NSValue* val = nw_framer_options_copy_object_value(framer_options, "BOO");

    void* val = (void*)2;
    (void*)message;
    printf("framer_output_handler got message with length %zu framer %p %d val = %p\n", message_length, (void*)framer, is_complete, val);
//        printf("framer_output_handler got message with length %zu framer %p\n", message_length, framer);
//        uint8_t *buffer = alloca(message_length);
//        nw_framer_parse_output(framer, 1, message_length, buffer, ^size_t(uint8_t *buffer, size_t buffer_length, bool is_complete) {
//            dump_hex(buffer, buffer_length);
//            return buffer_length;
//        });


fflush(stdout);
};

static nw_framer_input_handler_t framer_input_handler = ^size_t(nw_framer_t framer) {
    printf("__framer_input_handler framer %p\n", (void*)framer);
    printf("%s:%d: __framer_input_handler framer %p\n", __func__, __LINE__, (void*)framer);
     nw_framer_parse_input(framer, 0, 1024, nil, ^size_t(uint8_t * _Nullable buffer, size_t bytesRead, bool is_complete) {
         printf("%s:%d: __framer_input_handler framer %p %p %ld, %d\n", __func__, __LINE__, (void*)framer, (void*)buffer, bytesRead, is_complete);
        return bytesRead;
     });

     return 0;

};

static nw_framer_stop_handler_t framer_stop_handler = ^bool(nw_framer_t framer) {
    printf("%s:%d: nw_framer_set_stop_handler!!!!!!!!!!!!!!!!! on %p\n", __func__, __LINE__, (void*)framer);

    if (__builtin_available(macOS 12.3, iOS 9.0, tvOS 9.0, watchOS 2.0, *))
    {
        size_t gcHandle = 0;
        nw_protocol_options_t framer_options = nw_framer_copy_options(framer);
        NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
        assert(num != NULL);

        nw_retain(framer);
        [num getValue:&gcHandle];
        printf("Got number %zu _statusFunc %p\n", gcHandle, (void*)_statusFunc);
        (_statusFunc)(gcHandle, PAL_NwStatusUpdates_FramerStart, 0, 0);
    }

    return TRUE;
};

static nw_framer_cleanup_handler_t framer_cleanup_handler = ^(nw_framer_t framer) {
    printf("%s:%d: nw_framer_set_cleanup_handler!!!!!!!!!!!!!!!!! on %p\n", __func__, __LINE__, (void*)framer);
};

static nw_framer_start_handler_t framer_start = ^nw_framer_start_result_t(nw_framer_t framer)
{
        printf("framer start!!!!!! %p\n", (void*)framer);
        assert(_statusFunc != NULL);
        size_t gcHandle = 0;

        if (__builtin_available(macOS 12.3, iOS 9.0, tvOS 9.0, watchOS 2.0, *))
        {
            nw_protocol_options_t framer_options = nw_framer_copy_options(framer);
            NSNumber* num = nw_framer_options_copy_object_value(framer_options, "GCHANDLE");
            assert(num != NULL);

            [num getValue:&gcHandle];
            printf("Got number %zu _statusFunc %p\n", gcHandle, (void*)_statusFunc);
//            (_statusFunc)(gcHandle, PAL_NwStatusUpdates_FramerStart, (size_t)framer);
        }
        else
        {
            // We should not be here
            assert(0);
        }

        // Notify SafeHandle with framer instance so we can submit to it directly.
        (_statusFunc)(gcHandle, PAL_NwStatusUpdates_FramerStart, (size_t)framer, 0);

        nw_framer_set_output_handler(framer, framer_output_handler);
        nw_framer_set_input_handler(framer, framer_input_handler);

        nw_framer_set_stop_handler(framer, framer_stop_handler);
        nw_framer_set_cleanup_handler(framer, framer_cleanup_handler);
        return nw_framer_start_result_ready;
};


int32_t AppleCryptoNative_NwProcessInputData(nw_connection_t connection, nw_framer_t framer, const uint8_t * buffer, int dataLength)
{
    static int count;

    //nw_protocol_options_t framer_options = nw_framer_copy_options(framer);

   int c = ++count;
    printf("%s:%d called for %p with frame %p %d bytes of data %llu, %d options = \n", __func__, __LINE__, (void*) connection, (void*)framer, dataLength, GetThreadId(), c);
    nw_framer_message_t message = nw_framer_message_create(framer);
    printf("%s:%d called for %p with frame %p %d bytes of data %llu, %d\n", __func__, __LINE__, (void*) connection, (void*)framer, dataLength, GetThreadId(), c);


   dispatch_semaphore_t sem = dispatch_semaphore_create(42);

    // There is race condition when connection can fail or be canceled and if it does we fail to create the message here.
    if (message == NULL)
    {
        return -1;
    }

    dispatch_data_t data;
     uint8_t * b2 = NULL;
    if (dataLength > 0)
    {
         b2 = malloc((size_t)dataLength);
         memcpy(b2, buffer, dataLength);

    //dispatch_data_t data = dispatch_data_create(b2, (size_t)dataLength, _inputQueue, nil);
        data = dispatch_data_create(b2, (size_t)dataLength, _tlsQueue, nil);
    nw_framer_message_set_value(message, "DATA", b2, NULL);
    }
    else
    {
        data = NULL;
    }



printf("%s:%d: caleld with mesage %p\n", __func__, __LINE__,  (void*)b2);
    if (data == NULL || message == NULL)
    {
        printf("%s:%d: WTF !!!!!!!!!!!!!!!!!!!!! %p %p on %p %llu c= %d\n", __func__, __LINE__, (void*)data, (void*)message, (void*) connection,  GetThreadId(), c);
    }
    nw_framer_async(framer, ^(void) 
//    dispatch_sync(_tlsQueue, ^(void) 
    {
        nw_framer_deliver_input(framer, b2, (size_t)dataLength, message, dataLength > 0 ? FALSE : TRUE);
        //nw_framer_deliver_input_no_copy(framer, (size_t)dataLength, message, TRUE);
        printf("%s:%d:  nw_framer_deliver_input is DONE!!! %d %pc=%d\n", __func__, __LINE__, dataLength, (void*) connection, c);
        //nw_framer_schedule_wakeup(framer, 1);
        dispatch_semaphore_signal(sem);
    });

    dispatch_semaphore_wait(sem,  DISPATCH_TIME_FOREVER);
    printf("%s:%d done %p %llu c= %d!!!!\n", __func__, __LINE__, (void*) connection,  GetThreadId(), c);
    fflush(stdout);

    return 0;
}

int AppleCryptoNative_NwStartTlsHandshake(nw_connection_t connection, size_t gcHandle)
{
    if (connection == NULL)
        return -1;


   printf("%s:%d: starting handshek with %p %zu\n",  __func__, __LINE__, (void*)connection, gcHandle);

    nw_connection_set_state_changed_handler(connection, ^(nw_connection_state_t state, nw_error_t error) {
        int errorCode  = error ? nw_error_get_error_code(error) : 0;
        printf("%s:%d: nw_connection_set_state_changed_handler !!!!!! %d error %d con %p handle %zu\n", __func__, __LINE__, state, errorCode, (void*)connection, gcHandle);
        fflush(stdout);


       // nw_endpoint_t endpoint = nw_connection_copy_endpoint(connection);
        //const struct sockaddr_in6* socketAddress = (const struct sockaddr_in6*)nw_endpoint_get_address(endpoint);

        //size_t gcHandle = ((size_t)socketAddress->sin6_flowinfo) << 32 | socketAddress->sin6_scope_id;

        //printf("%s:%d: onw_connection_set_state_changed_handler endpoint is %p 0x%0lx with 0x%0x 0x%0x gcHandle = 0x%0zx\n", __func__, __LINE__, (void*)endpoint, (long)0 , socketAddress->sin6_flowinfo,  socketAddress->sin6_scope_id, gcHandle);


        if (state != nw_connection_state_cancelled)
        {
//            nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _framerDefinition);
//            printf("%s:%d: meta = %p\n", __func__, __LINE__, (void*)meta);

            nw_parameters_t params = nw_connection_copy_parameters(connection);
            if (params == NULL)
            {
                printf("%s:%d:  NULL PARAMESS!!!!!\n",  __func__, __LINE__);
            }
            else
            {
//                nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _framerDefinition);
//                printf("%s:%d: nw_protocol_metadata_t %p\n", __func__, __LINE__,(void*) meta);

            }
         }
//            nw_protocol_options_t framer_options = nw_framer_create_options(_framerDefinition);


            


        nw_endpoint_t remote = nw_connection_copy_endpoint(connection);
        if (state == nw_connection_state_waiting) {
            printf("connect to %s port %u , is waiting error = %d\n",
                 nw_endpoint_get_hostname(remote),
                 nw_endpoint_get_port(remote), errorCode);
            if (error != NULL)
            {
                (_statusFunc)(gcHandle, PAL_NwStatusUpdates_HandshakeFailed, (size_t)errorCode, 0);
            }
        } else if (state == nw_connection_state_failed) {
            printf("connect to %s port %u failed\n",
                 nw_endpoint_get_hostname(remote),
                 nw_endpoint_get_port(remote));
                (_statusFunc)(gcHandle, PAL_NwStatusUpdates_HandshakeFailed, (size_t)errorCode, 0);
        } else if (state == nw_connection_state_ready) {
            //if (g_verbose) {
                fprintf(stderr, "Connection to %s port %u succeeded!\n",
                        nw_endpoint_get_hostname(remote),
                        nw_endpoint_get_port(remote));

/*
                nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);
                printf("%s:%d: META = %p %d %d\n", __func__, __LINE__, (void*)meta, nw_protocol_metadata_is_tls(meta), nw_protocol_metadata_is_framer_message(meta));
                if (meta != NULL)
                {
                    sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);
                    printf("%s:%d: SEC META= %p\n", __func__, __LINE__, (void*)secMeta);
                }
*/

                (_statusFunc)(gcHandle, PAL_NwStatusUpdates_HandshakeFinished, 0, 0);
            //}
        }
        else if (state == nw_connection_state_preparing)
        {
            printf("%s:%d: connect to prepating connection %p\n", __func__, __LINE__, (void*)connection);
        }
        else if (state == nw_connection_state_cancelled) {
             fprintf(stderr, "%s:%d: Connection nw_connection_state_cancelled\n", __func__, __LINE__);
            // Release the primary reference on the connection
            // that was taken at creation time
        //    nw_release(connection);
            (_statusFunc)(gcHandle, PAL_NwStatusUpdates_ConnectionCancelled, 0, 0);
             fprintf(stderr, "Connection nw_connection_state_cancelled !!!!\n");
        }
        else
        {
             fprintf(stderr, "WTF !!!!\n");
        }
       // nw_release(remote);
        fflush(stdout);
    });

    //nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, nw_protocol_definition_t definition);

    //printf("%s:%d: meta = %p for %p\n", __func__, __LINE__, meta, connection);


    nw_connection_set_queue(connection, _tlsQueue);
    nw_connection_start(connection);

    fflush(stdout);

    return PAL_TlsHandshakeState_WouldBlock;
}

int32_t AppleCryptoNative_NwCancelConnection(nw_connection_t connection)
{
    printf("%s:%d: called for %p\n", __func__, __LINE__, (void*)connection);
    nw_connection_cancel(connection);

    return 0;
}

int32_t AppleCryptoNative_NwSendToConnection(nw_connection_t connection,  size_t gcHandle,  uint8_t* buffer, int length)
{
    
    dispatch_data_t data = dispatch_data_create(buffer, (size_t)length, _inputQueue, ^{ printf("%s:%d: dispatch destructor called!!!\n", __func__, __LINE__);});

    nw_connection_send(connection, data, NW_CONNECTION_DEFAULT_MESSAGE_CONTEXT, FALSE, ^(nw_error_t  error) {
        printf("%s:%d: nw_connection_send completion send called!!! %p\n", __func__, __LINE__, (void*)connection);

        if (error != NULL)
        {
            int errorCode  = nw_error_get_error_code(error);
            printf("%s:%d: error %d\n", __func__, __LINE__, errorCode);

            (_statusFunc)(gcHandle, PAL_NwStatusUpdates_ConnectionWriteFailed, (size_t)errorCode, 0);
        }
        else
        {
            (_statusFunc)(gcHandle, PAL_NwStatusUpdates_ConnectionWriteFinished, 0, 0);
        }
        printf("%s:%d: nw_connection_send completion notify callback invoked done %p!!!\n", __func__, __LINE__, (void*)connection);
        fflush(stdout);
     });

    return 0;
}

int32_t AppleCryptoNative_NwReadFromConnection(nw_connection_t connection, size_t gcHandle, uint8_t* buffer, unsigned int length)
{
     printf("%s:%d: called with %p %u for %p\n", __func__, __LINE__, (void*)buffer, length, (void*)connection);
     if (length <= 0)
     {
         printf("%s:%d: WTF!!!!!!!!!!!!!!!!!!! %p, %d\n", __func__, __LINE__,  (void*)buffer, length);
        return -1;
     }
    nw_connection_receive(connection, 1, 65536, ^(dispatch_data_t content, nw_content_context_t context, bool is_complete, nw_error_t error) {
        int errorCode  = error ? nw_error_get_error_code(error) : 0;
        printf("%s:%d: received data!!!!!??? error %p context %p content %p %d buffer %p\n", __func__, __LINE__, (void*)error, (void*)context, (void*)content, is_complete, (void*)buffer);

        if (error != NULL)
        {
             errorCode  = nw_error_get_error_code(error);
             printf("%s:%d: failed with code %d\n", __func__, __LINE__, errorCode);

            return;
        }

        if (content != NULL)
        {
            printf("%s:%d: got %zu byes of data\n", __func__, __LINE__, dispatch_data_get_size(content));
            fflush(stdout);
        // TBD can we get buffer from .NET????

            const void *contig_buf;
            size_t contig_size;
            dispatch_data_t tmp = dispatch_data_create_map(content, &contig_buf, &contig_size);
            //void* ptr = malloc(dispatch_data_get_size(content));
            printf("%s:%d got %zu butes at %p  \n", __func__, __LINE__, contig_size, contig_buf);


       // printf(">>>%.*s<<<\n",  contig_size, contig_buf);


//        (_readFunc)((void*)gcHandle, (void*)((size_t)contig_buf), &contig_size);

        //dispatch_release(tmp);


             printf("%s:%d: going to call status callback %p\n", __func__, __LINE__, (void*)connection);
             fflush(stdout);
            (_statusFunc)(gcHandle, PAL_NwStatusUpdates_ConnectionReadFinished, contig_size, (size_t)contig_buf);
             dispatch_release(tmp);
         }

         if (is_complete || content == NULL)
         {
             printf("%s:%d: NO CONTENT or EOF ???? %p %d\n", __func__, __LINE__, (void*)connection, is_complete);
             (_statusFunc)(gcHandle, PAL_NwStatusUpdates_ConnectionReadFinished, 0, 0);
         }
         printf("%s:%d: call to status callback  dobae %p\n", __func__, __LINE__, (void*)connection);
         fflush(stdout);
    });

    return  0;
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
        default:
            return (tls_protocol_version_t)0;
#pragma clang diagnostic pop
    }
}

int32_t AppleCryptoNative_NwSetTlsOptions(nw_connection_t connection, size_t gcHandle, char* targetName, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol)
{
    printf("%s:%d: called with %p %zu\n", __func__, __LINE__, (void*)connection, gcHandle);
    nw_protocol_options_t tlsOptions = nw_tls_create_options();
    sec_protocol_options_t sec_options = nw_tls_copy_sec_protocol_options(tlsOptions);
    if (targetName != NULL)
    {
        printf("%s:%d: setting TARGETNAME >%s< *%zd)\n", __func__, __LINE__, targetName, strlen(targetName));
        sec_protocol_options_set_tls_server_name(sec_options, targetName);
    }

    tls_protocol_version_t version = PalSslProtocolToTlsProtocolVersion(minTlsProtocol);
    if ((int)version!= 0)
    {
        sec_protocol_options_set_min_tls_protocol_version(sec_options, version);
    }
    version = PalSslProtocolToTlsProtocolVersion(maxTlsProtocol);
    if ((int)version!= 0)
    {
        sec_protocol_options_set_max_tls_protocol_version(sec_options, version);
    }


    //sec_protocol_options_set_max_tls_protocol_version(sec_options, tls_protocol_version_TLSv12);

    sec_protocol_options_set_verify_block(sec_options, ^(sec_protocol_metadata_t metadata, sec_trust_t trust_ref, sec_protocol_verify_complete_t complete) {
        printf("%s:%d: called with %p %p\n", __func__, __LINE__, (void*) metadata, (void *)trust_ref);
        // We will verify trust later in  SslStream
        complete(true);

    }, _tlsQueue);

    nw_release(sec_options);

    nw_protocol_options_t framer_options = nw_framer_create_options(_framerDefinition);
    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        NSNumber *ref  =  [NSNumber numberWithLong:(long)gcHandle];
        nw_framer_options_set_object_value(framer_options, "GCHANDLE", ref);
    }

    nw_parameters_t parameters = nw_connection_copy_parameters(connection);
    nw_protocol_stack_t protocol_stack = nw_parameters_copy_default_protocol_stack(parameters);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, framer_options);
    nw_protocol_stack_prepend_application_protocol(protocol_stack, tlsOptions);

    return 0;
}

int32_t AppleCryptoNative_NwGetConnectionInfo(nw_connection_t connection, PAL_SslProtocol* protocol, uint16_t* pCipherSuiteOut, const char** negotiatedAlpn, uint32_t* alpnLength)
{

    nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);
    printf("%s:%d: META = %p %d %d\n", __func__, __LINE__, (void*)meta, nw_protocol_metadata_is_tls(meta), nw_protocol_metadata_is_framer_message(meta));
    if (meta != NULL)
    {
        sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);
        printf("%s:%d: SEC META= %p\n", __func__, __LINE__, (void*)secMeta);

        const char* alpn = sec_protocol_metadata_get_negotiated_protocol(secMeta);
        if (alpn != NULL)
        {
            *negotiatedAlpn = alpn;
            *alpnLength= (uint32_t)strlen(alpn);
        }
        else
        {
            *negotiatedAlpn= NULL;
            *alpnLength = 0;
        }

        tls_protocol_version_t version = sec_protocol_metadata_get_negotiated_tls_protocol_version(secMeta);
        switch (version)
        {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            case tls_protocol_version_TLSv10:
                *protocol = PAL_SslProtocol_Tls10;
                break;
            case tls_protocol_version_TLSv11:
                *protocol = PAL_SslProtocol_Tls11;
                break;
#pragma clang diagnostic pop
           case tls_protocol_version_TLSv12:
                *protocol = PAL_SslProtocol_Tls12;
                break;
           case tls_protocol_version_TLSv13:
                *protocol = PAL_SslProtocol_Tls13;
                break;
           case tls_protocol_version_DTLSv12:
           default:
                *protocol = PAL_SslProtocol_None;
                break;
        }

        *pCipherSuiteOut = sec_protocol_metadata_get_negotiated_tls_ciphersuite(secMeta);
        return 0;
    }

    return -1;
}

PALEXPORT int32_t AppleCryptoNative_NwCopyCertChain(nw_connection_t connection, CFArrayRef* certificates, int* certificateCount)
{
    CFMutableArrayRef certs = NULL;
    __block int count = 0;

    nw_protocol_metadata_t meta = nw_connection_copy_protocol_metadata(connection, _tlsDefinition);
    printf("%s:%d: META = %p %d %d\n", __func__, __LINE__, (void*)meta, nw_protocol_metadata_is_tls(meta), nw_protocol_metadata_is_framer_message(meta));
    if (meta != NULL)
    {
        sec_protocol_metadata_t secMeta = nw_tls_copy_sec_protocol_metadata(meta);
        printf("%s:%d: SEC META= %p\n", __func__, __LINE__, (void*)secMeta);
        if (secMeta != NULL)
        {

      //  CFMutableArrayRef certs = CFArrayCreateMutable(NULL, 0 , NULL);

            sec_protocol_metadata_access_peer_certificate_chain(secMeta, ^(sec_certificate_t certificate) {
                count++;
                (void*)certificate;
            });

//        *certificateCount = count;

            if (count > 0)
            {
                certs = CFArrayCreateMutable(NULL, count, NULL);
                count--;

                printf("%s:%d: certs is %p count %d\n", __func__, __LINE__, (void*)certs, count);
    
                sec_protocol_metadata_access_peer_certificate_chain(secMeta, ^(sec_certificate_t certificate) {
                    SecCertificateRef c = sec_certificate_copy_ref(certificate);

                    printf("%s:%d: Got certificate %p %p of %lu index %d\n", __func__, __LINE__, (void*)certificate, (void*)c, CFGetTypeID(c), count);
//            CFRetain(certificate);
            CFArrayAppendValue(certs, sec_certificate_copy_ref(certificate));
            //        CFArrayInsertValueAtIndex(certs, 0, c);
                    count--;
                });
            }
       //     sec_release(secMeta);
        }

    }


//        *pOSStatus = SecTrustCreateWithCertificates(certs, NULL, pChainOut);

    *certificateCount= (int)CFArrayGetCount(certs);
    *certificates = (CFArrayRef)certs;

    return 0;
}


int32_t AppleCryptoNative_NwInit(SslStatusUpdateFunc statusFunc, SSLReadFunc readFunc, SSLWriteFunc writeFunc)
{
    assert(statusFunc != NULL);
    assert(writeFunc != NULL);
    assert(readFunc != NULL);

    if (__builtin_available(macOS 12.3, iOS 15.4, tvOS 15.4.0, watchOS 8.4, *))
    {
        _writeFunc = writeFunc;
        _readFunc = readFunc;
        _statusFunc = statusFunc;
        _framerDefinition = nw_framer_create_definition("tlsFramer", NW_FRAMER_CREATE_FLAGS_DEFAULT, framer_start);
        _tlsDefinition = nw_protocol_copy_tls_definition();
        _tlsQueue = dispatch_queue_create("TLS", NULL);
        _inputQueue = dispatch_queue_create("TLS_INPUT", NULL);

        return 0;
   }

   return -1;
}
