#include "signatures.hpp"
#include "importhelpers.hpp"
#include <cassert>
#include <cstring>
#include <tuple>
#include <vector>

namespace
{
    template<typename T, typename = typename std::enable_if<std::is_same<typename std::remove_const<T>::type, uint8_t>::value>::type>
    std::tuple<uint32_t, span<T>> read_compressed_uint(span<T> signature)
    {
        ULONG value = 0;
        signature = slice(signature, CorSigUncompressData(signature, &value));
        return std::make_tuple(value, signature);
    }

    template<typename T, typename = typename std::enable_if<std::is_same<typename std::remove_const<T>::type, uint8_t>::value>::type>
    std::tuple<int32_t, span<T>> read_compressed_int(span<T> signature)
    {
        int value = 0;
        signature = slice(signature, CorSigUncompressSignedInt(signature, &value));
        return std::make_tuple(value, signature);
    }

    template<typename T, typename = typename std::enable_if<std::is_same<typename std::remove_const<T>::type, uint8_t>::value>::type>
    std::tuple<mdToken, span<T>> read_compressed_token(span<T> signature)
    {
        mdToken value = mdTokenNil;
        signature = slice(signature, CorSigUncompressToken(signature, &value));
        return std::make_tuple(value, signature);
    }


    struct signature_element_part_tag
    {
    };
    
    struct raw_byte_tag final : signature_element_part_tag
    {
    };

    struct compressed_uint_tag final : signature_element_part_tag
    {
    };

    struct compressed_int_tag final : signature_element_part_tag
    {
    };

    struct token_tag final : signature_element_part_tag
    {
    };

    template<typename TCallback, typename T, typename = typename std::enable_if<std::is_same<typename std::remove_const<T>::type, uint8_t>::value>::type>
    span<T> WalkSignatureElement(span<T> signature, TCallback callback)
    {
        uint8_t elementType = signature[0];
        signature = slice(signature, 1);

        callback(elementType, raw_byte_tag{});
        switch (elementType)
        {
            case ELEMENT_TYPE_VOID:
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_SENTINEL:
                break;
            case ELEMENT_TYPE_FNPTR:
            {
                uint8_t callingConvention = signature[0];
                signature = slice(signature, 1);
                callback(callingConvention, raw_byte_tag{});

                uint32_t genericParameterCount;
                if ((callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC) == IMAGE_CEE_CS_CALLCONV_GENERIC)
                {
                    std::tie(genericParameterCount, signature) = read_compressed_uint(signature);
                    callback(genericParameterCount, compressed_uint_tag{});
                }

                uint32_t parameterCount;
                std::tie(parameterCount, signature) = read_compressed_uint(signature);
                callback(parameterCount, compressed_uint_tag{});

                // Walk the return type
                signature = WalkSignatureElement(signature, callback);

                // Walk the parameters
                for (uint32_t i = 0; i < parameterCount; i++)
                {
                    if (signature[0] == ELEMENT_TYPE_SENTINEL)
                    {
                        signature = slice(signature, 1);
                        callback((uint8_t)ELEMENT_TYPE_SENTINEL, raw_byte_tag{});
                    }

                    signature = WalkSignatureElement(signature, callback);
                }
                break;
            }
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_PINNED:
                signature = WalkSignatureElement(signature, callback);
                break;
            
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
            {
                uint32_t genericParameterIndex;
                std::tie(genericParameterIndex, signature) = read_compressed_uint(signature);
                callback(genericParameterIndex, compressed_uint_tag{});
                break;
            }

            case ELEMENT_TYPE_VALUETYPE:
            case ELEMENT_TYPE_CLASS:
            {
                mdToken token;
                std::tie(token, signature) = read_compressed_token(signature);
                callback(token, token_tag{});
                break;
            }

            case ELEMENT_TYPE_CMOD_REQD:
            case ELEMENT_TYPE_CMOD_OPT:
            {
                mdToken token;
                std::tie(token, signature) = read_compressed_token(signature);
                callback(token, token_tag{});
                signature = WalkSignatureElement(signature, callback);
                break;
            }

            case ELEMENT_TYPE_ARRAY:
            {
                signature = WalkSignatureElement(signature, callback);

                uint32_t rank;
                std::tie(rank, signature) = read_compressed_uint(signature);
                callback(rank, compressed_uint_tag{});

                uint32_t numSizes;
                std::tie(numSizes, signature) = read_compressed_uint(signature);
                callback(numSizes, compressed_uint_tag{});

                for (uint32_t i = 0; i < numSizes; i++)
                {
                    uint32_t size;
                    std::tie(size, signature) = read_compressed_uint(signature);
                    callback(size, compressed_uint_tag{});
                }

                uint32_t numLoBounds;
                std::tie(numLoBounds, signature) = read_compressed_uint(signature);
                callback(numLoBounds, compressed_uint_tag{});

                for (uint32_t i = 0; i < numLoBounds; i++)
                {
                    int32_t loBound;
                    std::tie(loBound, signature) = read_compressed_int(signature);
                    callback(loBound, compressed_int_tag{});
                }
                break;
            }

            case ELEMENT_TYPE_GENERICINST:
            {
                signature = WalkSignatureElement(signature, callback);

                uint32_t genericArgumentCount;
                std::tie(genericArgumentCount, signature) = read_compressed_uint(signature);
                callback(genericArgumentCount, compressed_uint_tag{});

                for (uint32_t i = 0; i < genericArgumentCount; i++)
                {
                    signature = WalkSignatureElement(signature, callback);
                }
                break;
            }
            default:
                throw std::invalid_argument { "Invalid signature element type" };
        }

        return signature;
    }
}

malloc_span<std::uint8_t> GetMethodDefSigFromMethodRefSig(span<uint8_t> methodRefSig)
{
    assert(methodRefSig.size() > 0);
    // We don't need to do anything with the various elements of the signature,
    // we just need to know how many parameters are before the sentinel.    
    span<uint8_t> signature = methodRefSig;
    uint8_t const callingConvention = signature[0];
    signature = slice(signature, 1);

    // The MethodDefSig is the same as the MethodRefSig if the calling convention is not vararg.
    // Only in the vararg case does the MethodRefSig have additional data to describe the exact vararg
    // parameter list.
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_VARARG)
    {
        malloc_span<uint8_t> methodDefSig{ (uint8_t*)std::malloc(methodRefSig.size()), methodRefSig.size() };
        std::memcpy(methodDefSig, methodRefSig, methodRefSig.size());
        return methodDefSig;
    }

    uint32_t genericParameterCount = 0;
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC) == IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        std::tie(genericParameterCount, signature) = read_compressed_uint(signature);
    }

    uint32_t originalParameterCount;
    std::tie(originalParameterCount, signature) = read_compressed_uint(signature);

    // Save this part of the signature for us to copy later.
    span<uint8_t> returnTypeAndParameters = signature;
    // Walk the return type
    // Use std::intmax_t here as it can handle all the values of the various parts of the signature.
    // If we were using C++14, we could use auto here instead.
    signature = WalkSignatureElement(signature, [](std::intmax_t, signature_element_part_tag) { });

    // Walk the parameters
    uint32_t i = 0;
    for (; i < originalParameterCount; i++)
    {
        if (signature[0] == ELEMENT_TYPE_SENTINEL)
        {
            break;
        }

        signature = WalkSignatureElement(signature, [](std::intmax_t, signature_element_part_tag) { });
    }
    
    // Now that we know the number of parameters, we can copy the MethodDefSig portion of the signature
    // and update the parameter count.
    // We need to account for the fact that the parameter count may be encoded with less bytes
    // as it is emitted using the compressed unsigned integer format.
    // An ECMA-335 compressed integer will take up no more than 4 bytes.
    uint8_t buffer[4];
    ULONG originalParamCountCompressedSize = CorSigCompressData(originalParameterCount, buffer);
    ULONG newParamCountCompressedSize = CorSigCompressData(i, buffer);
    span<uint8_t> compressedNewParamCount = { buffer, newParamCountCompressedSize };
    
    // The MethodDefSig length will be the length of the original signature up to the ELEMENT_TYPE_SENTINEL value,
    // minus the difference in the compressed size of the original parameter count and the new parameter count, if any.
    size_t methodDefSigBufferLength = methodRefSig.size() - signature.size() - originalParamCountCompressedSize + newParamCountCompressedSize;
    malloc_span<uint8_t> methodDefSigBuffer{ (uint8_t*)std::malloc(methodDefSigBufferLength), methodDefSigBufferLength };
    
    // Copy over the signature into the new buffer.
    // In case the parameter count was encoded with less bytes, we need to account for that
    // and copy the signature piece by piece.
    size_t offset = 0;
    methodDefSigBuffer[offset++] = callingConvention;
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC) == IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        offset += CorSigCompressData(genericParameterCount, methodDefSigBuffer + offset);
    }
    std::memcpy(methodDefSigBuffer + offset, compressedNewParamCount, newParamCountCompressedSize);
    offset += newParamCountCompressedSize;

    // Now that we've re-written the parameter count, we can copy the rest of the signature directly from the MethodRefSig
    assert(returnTypeAndParameters.size() >= methodDefSigBufferLength - offset);
    std::memcpy(methodDefSigBuffer + offset, returnTypeAndParameters, methodDefSigBufferLength - offset);

    return methodDefSigBuffer;
}

// Define a function object that enables us to combine multiple lambdas into a single overload set.
namespace
{
    template<typename... T>
    struct Overload;

    template<typename T>
    struct Overload<T>
    {
        Overload(T&& t) : _t{ std::forward<T>(t) }
        { }

        // Define a perfectly-forwarding operator() that will call the function object with the given arguments.
        template <typename... Args>
        auto operator()(Args&&... args) const 
        -> decltype(std::declval<T>()(std::forward<Args>(args)...))
        {
            return _t(std::forward<Args>(args)...);
        }
    private:
        T _t;
    };

    template<typename T, typename... Ts>
    struct Overload<T, Ts...> : Overload<T>, Overload<Ts...>
    {
        using Overload<T>::operator();
        using Overload<Ts...>::operator();
        
        Overload(T&& t, Ts&&... ts)
            :Overload<T>(std::forward<T>(t)),
             Overload<Ts...>(std::forward<Ts>(ts)...)
        {
        }
    };

    template<typename... Ts>
    Overload<Ts...> make_overload(Ts&&... ts)
    {
        return Overload<Ts...>(std::forward<Ts>(ts)...);
    }
}

HRESULT ImportSignatureIntoModule(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t destinationAssembly,
    mdhandle_t destinationModule,
    span<const uint8_t> signature,
    std::function<void(mdcursor_t)> onRowAdded,
    malloc_span<uint8_t>& importedSignature)
{
    HRESULT hr;
    // We are going to copy over the signature and replace the tokens from the source module in the signature
    // with equivalent tokens in the destination module, creating them if needed.
    std::vector<uint8_t> importedSignatureBuffer;
    // Our imported signature will likely be a very similar size to the original signature.
    importedSignatureBuffer.reserve(signature.size());

    auto onSignatureItemCallback = make_overload(
        [&](uint8_t byte, raw_byte_tag)
        {
            importedSignatureBuffer.push_back(byte);
        },
        [&](uint32_t value, compressed_uint_tag)
        {
            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressData(value, buffer);
            importedSignatureBuffer.insert(importedSignatureBuffer.end(), buffer, buffer + compressedSize);
        },
        [&](int32_t value, compressed_int_tag)
        {
            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressSignedInt(value, buffer);
            importedSignatureBuffer.insert(importedSignatureBuffer.end(), buffer, buffer + compressedSize);
        },
        [=, &importedSignatureBuffer, &hr](mdToken token, token_tag)
        {
            HRESULT localHR = ImportReferenceToTypeDefOrRefOrSpec(
                sourceAssembly,
                sourceModule,
                sourceAssemblyHash,
                destinationAssembly,
                destinationModule,
                onRowAdded,
                &token);
            
            // We can safely continue walking the signature even if we failed to import the token.
            // We'll return the failure code when we're done.
            if (FAILED(localHR))
            {
                hr = localHR;
                return;
            }

            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressToken(token, buffer);
            importedSignatureBuffer.insert(importedSignatureBuffer.end(), buffer, buffer + compressedSize);
        }
    );

    const uint8_t callingConvention = signature[0];
    signature = slice(signature, 1);
    onSignatureItemCallback(callingConvention, raw_byte_tag{});


    uint32_t genericParameterCount = 0;
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC) == IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        std::tie(genericParameterCount, signature) = read_compressed_uint(signature);
        onSignatureItemCallback(genericParameterCount, compressed_uint_tag{});
    }

    uint32_t parameterCount = 0;
    // FieldSig doesn't have a parameter count.
    // It also has only one element, so treating FieldSig as having 0 parameters ends up with the correct behavior.
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD)
    {
        std::tie(parameterCount, signature) = read_compressed_uint(signature);
        onSignatureItemCallback(parameterCount, compressed_uint_tag{});
    }

    // Walk the return type
    // LocalVarSig and MethodSpecSig both don't have a return type. They both have only N elements,
    // captured in the parameter count.
    if ((callingConvention & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG
        && (callingConvention & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_GENERICINST)
    {
        signature = WalkSignatureElement(signature, onSignatureItemCallback);
    }

    if (FAILED(hr))
        return hr;

    // Walk the parameters
    uint32_t i = 0;
    for (; i < parameterCount; i++)
    {
        if (signature[0] == ELEMENT_TYPE_SENTINEL)
        {
            break;
        }

        signature = WalkSignatureElement(signature, onSignatureItemCallback);

        if (FAILED(hr))
            return hr;
    }

    uint8_t* buffer = (uint8_t*)std::malloc(importedSignatureBuffer.size());
    if (buffer == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    std::memcpy(buffer, importedSignatureBuffer.data(), importedSignatureBuffer.size());
    importedSignature = { buffer, importedSignatureBuffer.size() };
    return S_OK;
}

HRESULT ImportTypeSpecBlob(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t destinationAssembly,
    mdhandle_t destinationModule,
    span<const uint8_t> typeSpecBlob,
    std::function<void(mdcursor_t)> onRowAdded,
    malloc_span<uint8_t>& importedTypeSpecBlob)
{
    std::vector<uint8_t> importedTypeSpecBlobBuffer;
    // Our imported blob will likely be a very similar size to the original blob.
    importedTypeSpecBlobBuffer.reserve(typeSpecBlob.size());

    HRESULT hr = S_OK;

    // WalkSignatureElement is more permissive of what it will accept than the requirements of the TypeSpecBlob.
    span<const uint8_t> remaining = WalkSignatureElement(typeSpecBlob, make_overload(
        [&](uint8_t byte, raw_byte_tag)
        {
            importedTypeSpecBlobBuffer.push_back(byte);
        },
        [&](uint32_t value, compressed_uint_tag)
        {
            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressData(value, buffer);
            importedTypeSpecBlobBuffer.insert(importedTypeSpecBlobBuffer.end(), buffer, buffer + compressedSize);
        },
        [&](int32_t value, compressed_int_tag)
        {
            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressSignedInt(value, buffer);
            importedTypeSpecBlobBuffer.insert(importedTypeSpecBlobBuffer.end(), buffer, buffer + compressedSize);
        },
        [=, &importedTypeSpecBlobBuffer, &hr](mdToken token, token_tag)
        {
            HRESULT localHR = ImportReferenceToTypeDefOrRefOrSpec(
                sourceAssembly,
                sourceModule,
                sourceAssemblyHash,
                destinationAssembly,
                destinationModule,
                onRowAdded,
                &token);
            
            // We can safely continue walking the signature even if we failed to import the token.
            // We'll return the failure code when we're done.
            if (FAILED(localHR))
            {
                hr = localHR;
                return;
            }

            uint8_t buffer[4];
            ULONG compressedSize = CorSigCompressToken(token, buffer);
            importedTypeSpecBlobBuffer.insert(importedTypeSpecBlobBuffer.end(), buffer, buffer + compressedSize);
        }
    ));

    if (FAILED(hr))
        return hr;

    if (remaining.size() != 0)
    {
        // If we have any bytes remaining, then the TypeSpecBlob was invalid.
        return E_INVALIDARG;
    }

    uint8_t* buffer = (uint8_t*)std::malloc(importedTypeSpecBlobBuffer.size());
    if (buffer == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    std::memcpy(buffer, importedTypeSpecBlobBuffer.data(), importedTypeSpecBlobBuffer.size());
    importedTypeSpecBlob = { buffer, importedTypeSpecBlobBuffer.size() };
    return S_OK;
}