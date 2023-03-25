// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;

namespace System.Net.Quic;

internal sealed unsafe partial class MsQuicApi
{
    public void SetContext(MsQuicSafeHandle handle, void* context)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            ApiTable->SetContext(handle.QuicHandle, context);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void* GetContext(MsQuicSafeHandle handle)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            return ApiTable->GetContext(handle.QuicHandle);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void SetCallbackHandler(MsQuicSafeHandle handle, void* callback, void* context)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            ApiTable->SetCallbackHandler(handle.QuicHandle, callback, context);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int SetParam(MsQuicSafeHandle handle, uint param, uint bufferLength, void* buffer)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            return ApiTable->SetParam(handle.QuicHandle, param, bufferLength, buffer);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public int GetParam(MsQuicSafeHandle handle, uint param, uint* bufferLength, void* buffer)
    {
        bool success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            return ApiTable->GetParam(handle.QuicHandle, param, bufferLength, buffer);
        }
        finally
        {
            if (success)
            {
                handle.DangerousRelease();
            }
        }
    }

    public void RegistrationShutdown(MsQuicSafeHandle registration, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            registration.DangerousAddRef(ref success);
            ApiTable->RegistrationShutdown(registration.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                registration.DangerousRelease();
            }
        }
    }

    public int ConfigurationOpen(MsQuicSafeHandle registration, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QUIC_SETTINGS* settings, uint settingsSize, void* context, QUIC_HANDLE** configuration)
    {
        bool success = false;
        try
        {
            registration.DangerousAddRef(ref success);
            return ApiTable->ConfigurationOpen(registration.QuicHandle, alpnBuffers, alpnBuffersCount, settings, settingsSize, context, configuration);
        }
        finally
        {
            if (success)
            {
                registration.DangerousRelease();
            }
        }
    }

    public int ConfigurationLoadCredential(MsQuicSafeHandle configuration, QUIC_CREDENTIAL_CONFIG* config)
    {
        bool success = false;
        try
        {
            configuration.DangerousAddRef(ref success);
            return ApiTable->ConfigurationLoadCredential(configuration.QuicHandle, config);
        }
        finally
        {
            if (success)
            {
                configuration.DangerousRelease();
            }
        }
    }

    public int ListenerOpen(MsQuicSafeHandle registration, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int> callback, void* context, QUIC_HANDLE** listener)
    {
        bool success = false;
        try
        {
            registration.DangerousAddRef(ref success);
            return ApiTable->ListenerOpen(registration.QuicHandle, callback, context, listener);
        }
        finally
        {
            if (success)
            {
                registration.DangerousRelease();
            }
        }
    }

    public int ListenerStart(MsQuicSafeHandle listener, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QuicAddr* localAddress)
    {
        bool success = false;
        try
        {
            listener.DangerousAddRef(ref success);
            return ApiTable->ListenerStart(listener.QuicHandle, alpnBuffers, alpnBuffersCount, localAddress);
        }
        finally
        {
            if (success)
            {
                listener.DangerousRelease();
            }
        }
    }

    public void ListenerStop(MsQuicSafeHandle listener)
    {
        bool success = false;
        try
        {
            listener.DangerousAddRef(ref success);
            ApiTable->ListenerStop(listener.QuicHandle);
        }
        finally
        {
            if (success)
            {
                listener.DangerousRelease();
            }
        }
    }

    public int ConnectionOpen(MsQuicSafeHandle registration, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> callback, void* context, QUIC_HANDLE** connection)
    {
        bool success = false;
        try
        {
            registration.DangerousAddRef(ref success);
            return ApiTable->ConnectionOpen(registration.QuicHandle, callback, context, connection);
        }
        finally
        {
            if (success)
            {
                registration.DangerousRelease();
            }
        }
    }

    public void ConnectionShutdown(MsQuicSafeHandle connection, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            connection.DangerousAddRef(ref success);
            ApiTable->ConnectionShutdown(connection.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                connection.DangerousRelease();
            }
        }
    }

    public int ConnectionStart(MsQuicSafeHandle connection, MsQuicSafeHandle configuration, ushort family, sbyte* serverName, ushort serverPort)
    {
        bool connectionSuccess = false;
        bool configurationSuccess = false;
        try
        {
            connection.DangerousAddRef(ref connectionSuccess);
            configuration.DangerousAddRef(ref configurationSuccess);
            return ApiTable->ConnectionStart(connection.QuicHandle, configuration.QuicHandle, family, serverName, serverPort);
        }
        finally
        {
            if (connectionSuccess)
            {
                connection.DangerousRelease();
            }
            if (configurationSuccess)
            {
                configuration.DangerousRelease();
            }
        }
    }

    public int ConnectionSetConfiguration(MsQuicSafeHandle connection, MsQuicSafeHandle configuration)
    {
        bool connectionSuccess = false;
        bool configurationSuccess = false;
        try
        {
            connection.DangerousAddRef(ref connectionSuccess);
            configuration.DangerousAddRef(ref configurationSuccess);
            return ApiTable->ConnectionSetConfiguration(connection.QuicHandle, configuration.QuicHandle);
        }
        finally
        {
            if (connectionSuccess)
            {
                connection.DangerousRelease();
            }
            if (configurationSuccess)
            {
                configuration.DangerousRelease();
            }
        }
    }

    public int StreamOpen(MsQuicSafeHandle connection, QUIC_STREAM_OPEN_FLAGS flags, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> callback, void* context, QUIC_HANDLE** stream)
    {
        bool success = false;
        try
        {
            connection.DangerousAddRef(ref success);
            return ApiTable->StreamOpen(connection.QuicHandle, flags, callback, context, stream);
        }
        finally
        {
            if (success)
            {
                connection.DangerousRelease();
            }
        }
    }

    public int StreamStart(MsQuicSafeHandle stream, QUIC_STREAM_START_FLAGS flags)
    {
        bool success = false;
        try
        {
            stream.DangerousAddRef(ref success);
            return ApiTable->StreamStart(stream.QuicHandle, flags);
        }
        finally
        {
            if (success)
            {
                stream.DangerousRelease();
            }
        }
    }

    public int StreamShutdown(MsQuicSafeHandle stream, QUIC_STREAM_SHUTDOWN_FLAGS flags, ulong code)
    {
        bool success = false;
        try
        {
            stream.DangerousAddRef(ref success);
            return ApiTable->StreamShutdown(stream.QuicHandle, flags, code);
        }
        finally
        {
            if (success)
            {
                stream.DangerousRelease();
            }
        }
    }

    public int StreamSend(MsQuicSafeHandle stream, QUIC_BUFFER* buffers, uint buffersCount, QUIC_SEND_FLAGS flags, void* context)
    {
        bool success = false;
        try
        {
            stream.DangerousAddRef(ref success);
            return ApiTable->StreamSend(stream.QuicHandle, buffers, buffersCount, flags, context);
        }
        finally
        {
            if (success)
            {
                stream.DangerousRelease();
            }
        }
    }

    public void StreamReceiveComplete(MsQuicSafeHandle stream, ulong length)
    {
        bool success = false;
        try
        {
            stream.DangerousAddRef(ref success);
            ApiTable->StreamReceiveComplete(stream.QuicHandle, length);
        }
        finally
        {
            if (success)
            {
                stream.DangerousRelease();
            }
        }
    }

    public int StreamReceiveSetEnabled(MsQuicSafeHandle stream, byte enabled)
    {
        bool success = false;
        try
        {
            stream.DangerousAddRef(ref success);
            return ApiTable->StreamReceiveSetEnabled(stream.QuicHandle, enabled);
        }
        finally
        {
            if (success)
            {
                stream.DangerousRelease();
            }
        }
    }
}
