#include "signatures.hpp"
#include <cassert>
#include <cstring>
#include <tuple>

namespace
{
    std::tuple<uint32_t, span<uint8_t>> read_compressed_uint(span<uint8_t> signature)
    {
        ULONG value;
        signature = slice(signature, CorSigUncompressData(signature, &value));
        return std::make_tuple(value, signature);
    }

    std::tuple<int32_t, span<uint8_t>> read_compressed_int(span<uint8_t> signature)
    {
        int value;
        signature = slice(signature, CorSigUncompressSignedInt(signature, &value));
        return std::make_tuple(value, signature);
    }

    std::tuple<mdToken, span<uint8_t>> read_compressed_token(span<uint8_t> signature)
    {
        mdToken value;
        signature = slice(signature, CorSigUncompressToken(signature, &value));
        return std::make_tuple(value, signature);
    }


    struct signature_element_part_tag
    {
    };
    
    struct raw_byte_tag : signature_element_part_tag
    {
    };

    struct compressed_uint_tag : signature_element_part_tag
    {
    };

    struct compressed_int_tag : signature_element_part_tag
    {
    };

    struct token_tag : signature_element_part_tag
    {
    };

    template<typename TCallback>
    span<uint8_t> WalkSignatureElement(span<uint8_t> signature, TCallback callback)
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
                        callback(ELEMENT_TYPE_SENTINEL, raw_byte_tag{});
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
