// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICS_PROTOCOL_H__
#define __DIAGNOSTICS_PROTOCOL_H__

#ifdef FEATURE_PERFTRACING

#include "clr_std/type_traits"
#include "new.hpp"
#include "diagnosticsipc.h"
#include "corerror.h"

#define DOTNET_IPC_V1_MAGIC "DOTNET_IPC_V1"

template <typename T>
bool TryParse(uint8_t *&bufferCursor, uint32_t &bufferLen, T &result)
{
    static_assert(
        std::is_integral<T>::value || std::is_same<T, float>::value ||
        std::is_same<T, double>::value || std::is_same<T, CLSID>::value,
        "Can only be instantiated with integral and floating point types.");

    if (bufferLen < sizeof(T))
        return false;
    result = *(reinterpret_cast<T *>(bufferCursor));
    bufferCursor += sizeof(T);
    bufferLen -= sizeof(T);
    return true;
}

template <typename T>
bool TryParseString(uint8_t *&bufferCursor, uint32_t &bufferLen, const T *&result)
{
    static_assert(
        std::is_same<T, char>::value || std::is_same<T, wchar_t>::value,
        "Can only be instantiated with char and wchar_t types.");

    uint32_t stringLen = 0;
    if (!TryParse(bufferCursor, bufferLen, stringLen))
        return false;
    if (stringLen == 0)
    {
        result = nullptr;
        return true;
    }
    if (stringLen > (bufferLen / sizeof(T)))
        return false;
    if ((reinterpret_cast<const T *>(bufferCursor))[stringLen - 1] != 0)
        return false;
    result = reinterpret_cast<const T *>(bufferCursor);

    const uint32_t TotalStringLength = stringLen * sizeof(T);
    bufferCursor += TotalStringLength;
    bufferLen -= TotalStringLength;
    return true;
}

namespace DiagnosticsIpc
{
    enum class IpcMagicVersion : uint8_t
    {
        DOTNET_IPC_V1 = 0x01,
        // FUTURE
    };

    enum class DiagnosticServerCommandSet : uint8_t
    {
        // reserved   = 0x00,
        Dump          = 0x01,
        EventPipe     = 0x02,
        Profiler      = 0x03,

        Server        = 0xFF,
    };

    enum class DiagnosticServerCommandId : uint8_t
    {
        OK    = 0x00,
        Error = 0xFF,
    };

    struct MagicVersion
    {
        uint8_t Magic[14];
    };

    // The header to be associated with every command and response
    // to/from the diagnostics server
    struct IpcHeader
    {
        union
        {
            MagicVersion _magic;
            uint8_t  Magic[14];  // Magic Version number; a 0 terminated char array
        };
        uint16_t Size;       // The size of the incoming packet, size = header + payload size
        uint8_t  CommandSet; // The scope of the Command.
        uint8_t  CommandId;  // The command being sent
        uint16_t Reserved;   // reserved for future use
    };

    const MagicVersion DotnetIpcMagic_V1 = { "DOTNET_IPC_V1" };

    const IpcHeader GenericSuccessHeader =
    {
        { DotnetIpcMagic_V1 },
        (uint16_t)sizeof(IpcHeader),
        (uint8_t)DiagnosticServerCommandSet::Server,
        (uint8_t)DiagnosticServerCommandId::OK,
        (uint16_t)0x0000
    };

    const IpcHeader GenericErrorHeader =
    {
        { DotnetIpcMagic_V1 },
        (uint16_t)sizeof(IpcHeader),
        (uint8_t)DiagnosticServerCommandSet::Server,
        (uint8_t)DiagnosticServerCommandId::Error,
        (uint16_t)0x0000
    };

    // The Following structs are template, meta-programming to enable
    // users of the IpcMessage class to get free serialization for fixed-size structures.
    // They check that the template parameter has a member (or static) function that
    // has a specified signature and returns true or false based on that check.
    //
    // std::enable_if (and enable_if_t) act as a compile time flag to enable or
    // disable a template specialization based on a boolean value.
    //
    // The Has* structs can be used as the boolean check in std::enable_if to
    // enable a specific overload of a function based on whether the template parameter
    // has that member function.
    //
    // These "switches" can be used in a variety of ways, but are used in the function
    // template parameters below, e.g.,
    //
    // template <typename T,
    //           typename = enable_if_t<HasTryParse<T>::value, const T*> = nullptr>
    // const T* FnName(...)
    //
    // For more details on this pattern, look up "Substitution Failure Is Not An Error" or SFINAE

    // template meta-programming to check for bool(Flatten)(void*) member function
    template <typename T>
    struct HasFlatten
    {
        template <typename U, U u> struct Has;
        template <typename U> static std::true_type test(Has<bool (U::*)(void*), &U::Flatten>*);
        template <typename U> static std::false_type test(...);
        static constexpr bool value = decltype(test<T>(nullptr))::value;
    };

    // template meta-programming to check for uint16_t(GetSize)() member function
    template <typename T>
    struct HasGetSize
    {
        template <typename U, U u> struct Has;
        template <typename U> static std::true_type test(Has<uint16_t(U::*)(), &U::GetSize>*);
        template <typename U> static std::false_type test(...);
        static constexpr bool value = decltype(test<T>(nullptr))::value;
    };

    // template meta-programming to check for a const T*(TryParse)(BYTE*,uint16_t&) static function
    template <typename T>
    struct HasTryParse
    {
        template <typename U, U u> struct Has;
        template <typename U> static std::true_type test(Has<const U* (*)(BYTE*, uint16_t&), &U::TryParse>*);
        template <typename U> static std::false_type test(...);
        static constexpr bool value = decltype(test<T>(nullptr))::value;
    };

    // Encodes the messages sent and received by the Diagnostics Server.
    //
    // Payloads that are fixed-size structs don't require any custom functionality.
    //
    // Payloads that are NOT fixed-size simply need to implement the following methods:
    //  * uint16_t GetSize()                                     -> should return the flattened size of the payload
    //  * bool Flatten(BYTE *lpBuffer)                           -> Should serialize and write the payload to the provided buffer
    //  * const T *TryParse(BYTE *lpBuffer, uint16_t& bufferLen) -> should decode payload or return nullptr
    class IpcMessage
    {
    public:

        // empty constructor for default values.  Use Initialize.
        IpcMessage()
            : m_pData(nullptr), m_Header(), m_Size(0)
        {
            LIMITED_METHOD_CONTRACT;
        };

        // Initialize an outgoing IpcMessage with a header and payload
        template <typename T>
        bool Initialize(IpcHeader header, T& payload)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            m_Header = header;

            return FlattenImpl<T>(payload);
        };

        // Initialize an outgoing IpcMessage with a header and payload
        template <typename T>
        bool Initialize(IpcHeader header, T&& payload)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            m_Header = header;

            return FlattenImpl<T>(payload);
        };

        // Initialize an outgoing IpcMessage for an error
        bool Initialize(HRESULT error)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            return Initialize(GenericErrorHeader, error);
        }

        // Initialize an incoming IpcMessage from a stream by parsing
        // the header and payload.
        //
        // If either fail, this returns false, true otherwise
        bool Initialize(::IpcStream* pStream)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            return TryParse(pStream);
        }

        ~IpcMessage()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            delete[] m_pData;
        };

        // Given a buffer, attempt to parse out a given payload type
        // If a payload type is fixed-size, this will simply return
        // a pointer to the buffer of data reinterpreted as a const pointer.
        // Otherwise, your payload type should implement the following static method:
        // > const T *TryParse(BYTE *lpBuffer)
        // which this will call if it exists.
        //
        // user is expected to check for a nullptr in the error case for non fixed-size payloads
        // user owns the memory returned and is expected to free it when finished
        template <typename T>
        const T* TryParsePayload()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            ASSERT(IsFlattened());
            return TryParsePayloadImpl<T>();
        };

        const IpcHeader& GetHeader() const
        {
            LIMITED_METHOD_CONTRACT;

            return m_Header;
        };

        bool Send(IpcStream* pStream)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_ANY;
                PRECONDITION(pStream != nullptr);
            }
            CONTRACTL_END;

            ASSERT(IsFlattened());
            uint32_t nBytesWritten;
            bool success = pStream->Write(m_pData, m_Size, nBytesWritten);

            return nBytesWritten == m_Size && success;
        };

        // Send an Error message across the pipe.
        // Will return false on failure of any step (init or send).
        // Regardless of success of this function, the spec
        // dictates that the connection be closed on error,
        // so the user is expected to delete the IpcStream
        // after handling error cases.
        static bool SendErrorMessage(IpcStream* pStream, HRESULT error)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
                PRECONDITION(pStream != nullptr);
            }
            CONTRACTL_END;

            IpcMessage errorMessage;
            bool success = errorMessage.Initialize((int32_t)error);
            if (success)
                success = errorMessage.Send(pStream);
            return success;
        };
    private:
        // Pointer to flattened buffer filled with:
        // incoming message: payload (could be empty which would be nullptr)
        // outgoing message: header + payload
        BYTE* m_pData;
        // header associated with this message
        struct IpcHeader m_Header;
        // The total size of the message (header + payload)
        uint16_t m_Size;

        bool IsFlattened() const
        {
            LIMITED_METHOD_CONTRACT;

            return m_pData != NULL;
        };

        // Attempt to populate header and payload from a buffer.
        // Payload is left opaque as a flattened buffer in m_pData
        bool TryParse(::IpcStream* pStream)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
                PRECONDITION(pStream != nullptr);
            }
            CONTRACTL_END;

            // Read out header first
            uint32_t nBytesRead;
            bool success = pStream->Read(&m_Header, sizeof(IpcHeader), nBytesRead);
            if (!success || nBytesRead < sizeof(IpcHeader))
            {
                return false;
            }

            if (m_Header.Size < sizeof(IpcHeader))
            {
                return false;
            }

            m_Size = m_Header.Size;

            // Then read out payload to buffer
            uint16_t payloadSize = m_Header.Size - sizeof(IpcHeader);
            if (payloadSize != 0)
            {
                BYTE* temp_buffer = new (nothrow) BYTE[payloadSize];
                if (temp_buffer == nullptr)
                {
                    // OOM
                    return false;
                }

                success = pStream->Read(temp_buffer, payloadSize, nBytesRead);
                if (!success || nBytesRead < payloadSize)
                {
                    delete[] temp_buffer;
                    return false;
                }
                m_pData = temp_buffer;
            }

            return true;
        };

        // Create a buffer of the correct size filled with
        // header + payload. Correctly handles flattening of
        // trivial structures, but uses a bool(Flatten)(void*)
        // and uint16_t(GetSize)() when available.

        // Handles the case where the payload structure exposes Flatten
        // and GetSize methods
        template <typename U,
                  typename std::enable_if<HasFlatten<U>::value&& HasGetSize<U>::value, int>::type = 0>
        bool FlattenImpl(U& payload)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            if (IsFlattened())
                return true;

            S_UINT16 temp_size = S_UINT16(0);
            temp_size += sizeof(struct IpcHeader) + payload.GetSize();
            ASSERT(!temp_size.IsOverflow());

            m_Size = temp_size.Value();

            BYTE* temp_buffer = new (nothrow) BYTE[m_Size];
            if (temp_buffer == nullptr)
            {
                // OOM
                return false;
            }

            BYTE* temp_buffer_cursor = temp_buffer;

            m_Header.Size = m_Size;

            memcpy(temp_buffer_cursor, &m_Header, sizeof(struct IpcHeader));
            temp_buffer_cursor += sizeof(struct IpcHeader);

            payload.Flatten(temp_buffer_cursor);

            ASSERT(m_pData == nullptr);
            m_pData = temp_buffer;

            return true;
        };

        // handles the case where we were handed a struct with no Flatten or GetSize method
        template <typename U,
                  typename std::enable_if<!HasFlatten<U>::value && !HasGetSize<U>::value, int>::type = 0>
        bool FlattenImpl(U& payload)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;

            if (IsFlattened())
                return true;

            S_UINT16 temp_size = S_UINT16(0);
            temp_size += sizeof(struct IpcHeader) + sizeof(payload);
            ASSERT(!temp_size.IsOverflow());

            m_Size = temp_size.Value();

            BYTE* temp_buffer = new (nothrow) BYTE[m_Size];
            if (temp_buffer == nullptr)
            {
                // OOM
                return false;
            }

            BYTE* temp_buffer_cursor = temp_buffer;

            m_Header.Size = m_Size;

            memcpy(temp_buffer_cursor, &m_Header, sizeof(struct IpcHeader));
            temp_buffer_cursor += sizeof(struct IpcHeader);

            memcpy(temp_buffer_cursor, &payload, sizeof(payload));

            ASSERT(m_pData == nullptr);
            m_pData = temp_buffer;

            return true;
        };

        template <typename U,
                  typename std::enable_if<HasTryParse<U>::value, int>::type = 0>
        const U* TryParsePayloadImpl()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            uint16_t payloadSize = m_Size - (uint16_t)sizeof(IpcHeader);
            const U* payload = U::TryParse(m_pData, payloadSize);
            m_pData = nullptr; // user is expected to clean up buffer when finished with it
            return payload;
        };

        template <typename U,
                  typename std::enable_if<!HasTryParse<U>::value, int>::type = 0>
        const U* TryParsePayloadImpl()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            const U* payload = reinterpret_cast<const U*>(m_pData);
            m_pData = nullptr; // user is expected to clean up buffer when finished with it
            return payload;
        };
    };
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTICS_PROTOCOL_H__
