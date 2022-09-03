// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// sigparser.h
//

//

#ifndef _H_SIGPARSER
#define _H_SIGPARSER

#include "utilcode.h"
#include "corhdr.h"
#include "corinfo.h"
#include "corpriv.h"
#include <minipal/utils.h>

//---------------------------------------------------------------------------------------
// These macros define how arguments are mapped to the stack in the managed calling convention.
// We assume to be walking a method's signature left-to-right, in the virtual calling convention.
// See MethodDesc::Call for details on this virtual calling convention.
// These macros tell us whether the arguments we see as we proceed with the signature walk are mapped
//   to increasing or decreasing stack addresses. This is valid only for arguments that go on the stack.
//---------------------------------------------------------------------------------------
#if defined(TARGET_X86)
#define STACK_GROWS_DOWN_ON_ARGS_WALK
#else
#define STACK_GROWS_UP_ON_ARGS_WALK
#endif

//------------------------------------------------------------------------
// Encapsulates how compressed integers and typeref tokens are encoded into
// a bytestream.
//
// As you use this class please understand the implicit normalizations
// on the CorElementType's returned by the various methods, especially
// for variable types (e.g. !0 in generic signatures), string types
// (i.e. E_T_STRING), object types (E_T_OBJECT), constructed types
// (e.g. List<int>) and enums.
//------------------------------------------------------------------------
class SigParser
{
    protected:
        // This type is performance critical - do not add fields to it.
        // (If you must, check for managed types that may use a SigParser or SigPointer inline, like ArgIterator.)
        PCCOR_SIGNATURE m_ptr;
        uint32_t           m_dwLen;

        //------------------------------------------------------------------------
        // Skips specified number of bytes WITHOUT VALIDATION. Only to be used
        // when it is known that it won't overflow the signature buffer.
        //------------------------------------------------------------------------
        FORCEINLINE void SkipBytes(uint32_t cb)
        {
            SUPPORTS_DAC;
            _ASSERT(cb <= m_dwLen);
            m_ptr += cb;
            m_dwLen -= cb;
        }

    public:
        //------------------------------------------------------------------------
        // Constructor.
        //------------------------------------------------------------------------
        SigParser() {
            LIMITED_METHOD_DAC_CONTRACT;
            m_ptr = NULL;
            m_dwLen = 0;
        }

        SigParser(const SigParser &sig);

        //------------------------------------------------------------------------
        // Initialize
        //------------------------------------------------------------------------
        FORCEINLINE SigParser(PCCOR_SIGNATURE ptr)
        {
            LIMITED_METHOD_CONTRACT;

            m_ptr = ptr;
            // We don't know the size of the signature, so we'll say it's "big enough"
            m_dwLen = 0xffffffff;
        }

        FORCEINLINE SigParser(PCCOR_SIGNATURE ptr, uint32_t len)
        {
            LIMITED_METHOD_CONTRACT;

            m_ptr = ptr;
            m_dwLen = len;
        }

        inline void SetSig(PCCOR_SIGNATURE ptr)
        {
            LIMITED_METHOD_CONTRACT;

            m_ptr = ptr;
            // We don't know the size of the signature, so we'll say it's "big enough"
            m_dwLen = 0xffffffff;
        }

        inline void SetSig(PCCOR_SIGNATURE ptr, uint32_t len)
        {
            LIMITED_METHOD_CONTRACT;

            m_ptr = ptr;
            m_dwLen = len;
        }


        inline BOOL IsNull() const
        {
            LIMITED_METHOD_CONTRACT;

            return (m_ptr == NULL);
        }

        // Returns represented signature as pointer and size.
        void
        GetSignature(
            PCCOR_SIGNATURE * pSig,
            uint32_t           * pcbSigSize)
        {
            *pSig = m_ptr;
            *pcbSigSize = m_dwLen;
        }


    //=========================================================================
    // The RAW interface for reading signatures.  You see exactly the signature,
    // apart from custom modifiers which for historical reasons tend to get eaten.
    //
    // DO NOT USE THESE METHODS UNLESS YOU'RE TOTALLY SURE YOU WANT
    // THE RAW signature.  You nearly always want GetElemTypeClosed() or
    // PeekElemTypeClosed() or one of the MetaSig functions.  See the notes above.
    // These functions will return E_T_INTERNAL, E_T_VAR, E_T_MVAR and such
    // so the caller must be able to deal with those
    //=========================================================================

        //------------------------------------------------------------------------
        // Remove one compressed integer (using CorSigUncompressData) from
        // the head of the stream and return it.
        //------------------------------------------------------------------------
        __checkReturn
        FORCEINLINE HRESULT GetData(uint32_t* data)
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            uint32_t sizeOfData = 0;
            uint32_t tempData;

            if (data == NULL)
                data = &tempData;

            HRESULT hr = CorSigUncompressData(m_ptr, m_dwLen, data, &sizeOfData);

            if (SUCCEEDED(hr))
            {
                SkipBytes(sizeOfData);
            }

            return hr;
        }


        //-------------------------------------------------------------------------
        // Remove one byte and return it.
        //-------------------------------------------------------------------------
        __checkReturn
        FORCEINLINE HRESULT GetByte(BYTE *data)
        {
            LIMITED_METHOD_CONTRACT;

            if (m_dwLen > 0)
            {
                if (data != NULL)
                    *data = *m_ptr;

                SkipBytes(1);

                return S_OK;
            }

            if (data != NULL)
                *data = 0;
            return META_E_BAD_SIGNATURE;
        }

        //-------------------------------------------------------------------------
        // Peek at value of one byte and return it.
        //-------------------------------------------------------------------------
        __checkReturn
        FORCEINLINE HRESULT PeekByte(BYTE *data)
        {
            LIMITED_METHOD_CONTRACT;

            if (m_dwLen > 0)
            {
                if (data != NULL)
                    *data = *m_ptr;

                return S_OK;
            }

            if (data != NULL)
                *data = 0;
            return META_E_BAD_SIGNATURE;
        }

        //-------------------------------------------------------------------------
        // The element type as defined in CorElementType. No normalization for
        // generics (E_T_VAR, E_T_MVAR,..) or dynamic methods (E_T_INTERNAL occurs)
        //-------------------------------------------------------------------------
        __checkReturn
        HRESULT GetElemTypeSlow(CorElementType * etype)
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            CorElementType tmpEType;

            if (etype == NULL)
                etype = &tmpEType;

            SigParser sigTemp(*this);

            HRESULT hr = sigTemp.SkipCustomModifiers();

            if (SUCCEEDED(hr))
            {
                BYTE bElemType = 0;
                hr = sigTemp.GetByte(&bElemType);
                *etype = (CorElementType)bElemType;

                if (SUCCEEDED(hr))
                {
                    *this = sigTemp;
                    return S_OK;
                }
            }

            *etype = ELEMENT_TYPE_END;

            return META_E_BAD_SIGNATURE;
        }

        // Inlined version
        __checkReturn
        FORCEINLINE HRESULT GetElemType(CorElementType * etype)
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            if (m_dwLen > 0)
            {
                CorElementType typ = (CorElementType) * m_ptr;

                if (typ < ELEMENT_TYPE_CMOD_REQD) // fast path with no modifiers: single byte
                {
                    if (etype != NULL)
                    {
                        * etype = typ;
                    }

                    SkipBytes(1);

                    return S_OK;
                }
            }

            // Slower/normal path
            return GetElemTypeSlow(etype);
        }

        // Note: Calling convention is always one byte, not four bytes
        __checkReturn
        HRESULT GetCallingConvInfo(uint32_t * data)
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            uint32_t tmpData;

            if (data == NULL)
                data = &tmpData;

            HRESULT hr = CorSigUncompressCallingConv(m_ptr, m_dwLen, data);
            if (SUCCEEDED(hr))
            {
                SkipBytes(1);
            }

            return hr;
        }

        __checkReturn
        HRESULT GetCallingConv(uint32_t* data)  // @REVISIT_TODO: Calling convention is one byte, not four.
        {
            WRAPPER_NO_CONTRACT;
            uint32_t info;
            HRESULT hr = GetCallingConvInfo(&info);

            if (SUCCEEDED(hr) && data != NULL)
            {
                *data = IMAGE_CEE_CS_CALLCONV_MASK & info;
            }

            return hr;
        }

        //------------------------------------------------------------------------
        // Non-destructive read of compressed integer.
        //------------------------------------------------------------------------
        __checkReturn
        HRESULT PeekData(uint32_t *data) const
        {
            WRAPPER_NO_CONTRACT;
            _ASSERTE(data != NULL);

            uint32_t sizeOfData = 0;
            return CorSigUncompressData(m_ptr, m_dwLen, data, &sizeOfData);
        }


        //------------------------------------------------------------------------
        // Non-destructive read of element type.
        //
        // This routine makes it look as if the String type is encoded
        // via ELEMENT_TYPE_CLASS followed by a token for the String class,
        // rather than the ELEMENT_TYPE_STRING. This is partially to avoid
        // rewriting client code which depended on this behavior previously.
        // But it also seems like the right thing to do generally.
        // No normalization for generics (E_T_VAR, E_T_MVAR,..) or
        // dynamic methods (E_T_INTERNAL occurs)
        //------------------------------------------------------------------------
        __checkReturn
        HRESULT PeekElemTypeSlow(CorElementType *etype) const
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(etype != NULL);

            SigParser sigTemp(*this);
            HRESULT hr = sigTemp.GetElemType(etype);
            if (SUCCEEDED(hr) && (*etype == ELEMENT_TYPE_STRING || *etype == ELEMENT_TYPE_OBJECT))
                *etype = ELEMENT_TYPE_CLASS;

            return hr;
        }

        // inline version
        __checkReturn
        FORCEINLINE HRESULT PeekElemType(CorElementType *etype) const
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(etype != NULL);

            if (m_dwLen > 0)
            {
                CorElementType typ = (CorElementType) * m_ptr;

                if (typ < ELEMENT_TYPE_CMOD_REQD) // fast path with no modifiers: single byte
                {
                    if ((typ == ELEMENT_TYPE_STRING) || (typ == ELEMENT_TYPE_OBJECT))
                    {
                        *etype = ELEMENT_TYPE_CLASS;
                    }
                    else
                    {
                        *etype = typ;
                    }

                    return S_OK;
                }
            }

            return PeekElemTypeSlow(etype);
        }

        //-------------------------------------------------------------------------
        // Returns the raw size of the type next in the signature, or returns
        // E_INVALIDARG for base types that have variables sizes.
        //-------------------------------------------------------------------------
        __checkReturn
        HRESULT PeekElemTypeSize(uint32_t *pSize)
        {
            WRAPPER_NO_CONTRACT;
            HRESULT hr = S_OK;

            uint32_t dwSize = 0;

            if (pSize == NULL)
            {
                pSize = &dwSize;
            }

            SigParser sigTemp(*this);

            hr = sigTemp.SkipAnyVASentinel();

            if (FAILED(hr))
            {
                return hr;
            }

            *pSize = 0;

            BYTE bElementType = 0;
            hr = sigTemp.GetByte(&bElementType);

            if (FAILED(hr))
            {
                return hr;
            }

            switch (bElementType)
            {
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
        #ifdef HOST_64BIT
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
        #endif // WIN64

                *pSize = 8;
                break;

            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_R4:
        #ifndef HOST_64BIT
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
        #endif // HOST_64BIT

                *pSize = 4;
                break;

            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_CHAR:
                *pSize = 2;
                break;

            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_BOOLEAN:
                *pSize = 1;
                break;

            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_FNPTR:
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_ARRAY:
            case ELEMENT_TYPE_SZARRAY:
                *pSize = sizeof(void *);
                break;

            case ELEMENT_TYPE_VOID:
                break;

            case ELEMENT_TYPE_END:
            case ELEMENT_TYPE_CMOD_REQD:
            case ELEMENT_TYPE_CMOD_OPT:
                _ASSERTE(!"Asked for the size of an element that doesn't have a size!");
                return E_INVALIDARG;

            case ELEMENT_TYPE_VALUETYPE:
                _ASSERTE(!"Asked for the size of an element that doesn't have a size!");
                return E_INVALIDARG;

            default:

                _ASSERTE( !"CorSigGetElementTypeSize given invalid signature to size!" );
                return META_E_BAD_SIGNATURE;
            }

            return hr;
        }

        //------------------------------------------------------------------------
        // Is this at the Sentinel (the ... in a varargs signature) that marks
        // the beginning of varguments that are not decared at the target

        bool AtSentinel() const
        {
            if (m_dwLen > 0)
                return *m_ptr == ELEMENT_TYPE_SENTINEL;
            else
                return false;
        }

        //------------------------------------------------------------------------
        // Removes a compressed metadata token and returns it.
        // WARNING: dynamic methods do not have tokens so this api is completely
        //          broken in that case. Make sure you call this function if
        //          you are absolutely sure E_T_INTERNAL was not in the sig
        //------------------------------------------------------------------------
        __checkReturn
        FORCEINLINE
        HRESULT GetToken(mdToken * token)
        {
            WRAPPER_NO_CONTRACT;
            uint32_t dwLen;
            mdToken tempToken;

            if (token == NULL)
                token = &tempToken;

            HRESULT hr = CorSigUncompressToken(m_ptr, m_dwLen, token, &dwLen);

            if (SUCCEEDED(hr))
            {
                SkipBytes(dwLen);
            }

            return hr;
        }

        //------------------------------------------------------------------------
        // Removes a pointer value and returns it. Used for ELEMENT_TYPE_INTERNAL.
        __checkReturn
        FORCEINLINE
        HRESULT GetPointer(void ** pPtr)
        {
            WRAPPER_NO_CONTRACT;

            if (m_dwLen < sizeof(void *))
            {   // Not enough data to read a pointer
                if (pPtr != NULL)
                {
                    *pPtr = NULL;
                }
                return META_E_BAD_SIGNATURE;
            }
            if (pPtr != NULL)
            {
                *pPtr = *(void * UNALIGNED *)m_ptr;
            }
            SkipBytes(sizeof(void *));

            return S_OK;
        }

        //------------------------------------------------------------------------
        // Tests if two SigParsers point to the same location in the stream.
        //------------------------------------------------------------------------
        FORCEINLINE BOOL Equals(SigParser sp) const
        {
            LIMITED_METHOD_CONTRACT;

            return m_ptr == sp.m_ptr;
        }

        __checkReturn
        HRESULT SkipCustomModifiers()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            HRESULT hr = S_OK;

            SigParser sigTemp(*this);

            hr = sigTemp.SkipAnyVASentinel();

            if (FAILED(hr))
            {
                return hr;
            }

            BYTE bElementType = 0;

            hr = sigTemp.PeekByte(&bElementType);
            if (FAILED(hr))
                return hr;

            while ((ELEMENT_TYPE_CMOD_REQD == bElementType) ||
                   (ELEMENT_TYPE_CMOD_OPT == bElementType))
            {
                sigTemp.SkipBytes(1);

                mdToken token;

                hr = sigTemp.GetToken(&token);

                if (FAILED(hr))
                    return hr;

                hr = sigTemp.PeekByte(&bElementType);
                if (FAILED(hr))
                    return hr;
            }

            // Following custom modifiers must be an element type value which is less than ELEMENT_TYPE_MAX, or one of the other element types
            // that we support while parsing various signatures
            if (bElementType >= ELEMENT_TYPE_MAX)
            {
                switch (bElementType)
                {
                case ELEMENT_TYPE_VAR_ZAPSIG:
                case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
                case ELEMENT_TYPE_CANON_ZAPSIG:
                case ELEMENT_TYPE_MODULE_ZAPSIG:
                case ELEMENT_TYPE_PINNED:
                    break;
                default:
                    return META_E_BAD_SIGNATURE;
                }
            }

            *this = sigTemp;
            return hr;
        }// SkipCustomModifiers

        __checkReturn
        HRESULT SkipFunkyAndCustomModifiers()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            SigParser sigTemp(*this);
            HRESULT hr = S_OK;
            hr = sigTemp.SkipAnyVASentinel();

            if (FAILED(hr))
            {
                return hr;
            }

            BYTE bElementType = 0;

            hr = sigTemp.PeekByte(&bElementType);
            if (FAILED(hr))
                return hr;

            while (ELEMENT_TYPE_CMOD_REQD == bElementType ||
                   ELEMENT_TYPE_CMOD_OPT == bElementType ||
                   ELEMENT_TYPE_MODIFIER == bElementType ||
                   ELEMENT_TYPE_PINNED == bElementType)
            {
                sigTemp.SkipBytes(1);

                mdToken token;

                hr = sigTemp.GetToken(&token);

                if (FAILED(hr))
                    return hr;

                hr = sigTemp.PeekByte(&bElementType);
                if (FAILED(hr))
                    return hr;
            }

            // Following custom modifiers must be an element type value which is less than ELEMENT_TYPE_MAX, or one of the other element types
            // that we support while parsing various signatures
            if (bElementType >= ELEMENT_TYPE_MAX)
            {
                switch (bElementType)
                {
                case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
                case ELEMENT_TYPE_CANON_ZAPSIG:
                case ELEMENT_TYPE_MODULE_ZAPSIG:
                case ELEMENT_TYPE_PINNED:
                    break;
                default:
                    return META_E_BAD_SIGNATURE;
                }
            }

            *this = sigTemp;
            return hr;
        }// SkipFunkyAndCustomModifiers


        __checkReturn
        HRESULT SkipAnyVASentinel()
        {
            WRAPPER_NO_CONTRACT;

            HRESULT hr = S_OK;
            BYTE bElementType = 0;

            hr = PeekByte(&bElementType);
            if (FAILED(hr))
                return hr;

            if (bElementType == ELEMENT_TYPE_SENTINEL)
            {
                SkipBytes(1);
            }

            return hr;
        }// SkipAnyVASentinel

        //------------------------------------------------------------------------
        // Assumes that the SigParser points to the start of an element type
        // (i.e. function parameter, function return type or field type.)
        // Advances the pointer to the first data after the element type.
        //------------------------------------------------------------------------
        __checkReturn
        HRESULT SkipExactlyOne();

        //------------------------------------------------------------------------
        // Skip only the method header of the signature, not the signature of
        // the arguments.
        //------------------------------------------------------------------------
        __checkReturn
        HRESULT SkipMethodHeaderSignature(uint32_t *pcArgs);

        //------------------------------------------------------------------------
        // Skip a sub signature (as immediately follows an ELEMENT_TYPE_FNPTR).
        //------------------------------------------------------------------------
        __checkReturn
        HRESULT SkipSignature();

public:

        //------------------------------------------------------------------------
        // Return pointer
        // PLEASE DON'T USE THIS.
        //
        // Return the internal pointer.  It's hard to resist, but please try
        // not to use this.  Certainly don't use it if there's any chance of the
        // signature containing generic type variables.
        //
        // It's currently only used for working on the
        // signatures stored in TypeSpec tokens (we should add a new abstraction,
        // i.e. on MetaSig for this) and a couple of places to do with COM
        // and native interop signature parsing.
        // <REVISIT_TODO>We should try to get rid of these uses as well. </REVISIT_TODO>
        //------------------------------------------------------------------------
        PCCOR_SIGNATURE GetPtr() const
        {
            LIMITED_METHOD_DAC_CONTRACT;
            return m_ptr;
        }

};  // class SigParser

//------------------------------------------------------------------------
FORCEINLINE
SigParser::SigParser(
    const SigParser &sig)
    : m_ptr(sig.m_ptr), m_dwLen(sig.m_dwLen)
{
    LIMITED_METHOD_DAC_CONTRACT;
}

/*****************************************************************/
/* CorTypeInfo is a single global table that you can hang information
   about ELEMENT_TYPE_* */

class CorTypeInfo
{
protected:
    struct CorTypeInfoEntry
    {
        LPCUTF8        nameSpace;
        LPCUTF8        className;
        CorElementType type         : 8;
        unsigned       size         : 8;
        CorInfoGCType  gcType       : 3;
        unsigned       isArray      : 1;
        unsigned       isPrim       : 1;
        unsigned       isFloat      : 1;
        unsigned       isModifier   : 1;
        unsigned       isGenVar     : 1;
        // 1 more byte here to use for 32-bit
    };

protected:
    FORCEINLINE static const CorTypeInfoEntry &GetTypeInfo(CorElementType type)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
#ifdef MODE_ANY
            MODE_ANY;
#endif
        }
        CONTRACTL_END;

        if (type >= (CorElementType)ARRAY_SIZE(info))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        return info[type];
    }
    FORCEINLINE static const CorTypeInfoEntry &GetTypeInfo_NoThrow(CorElementType type)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (type >= (CorElementType)ARRAY_SIZE(info))
        {
            return info[ELEMENT_TYPE_END];
        }
        return info[type];
    }

public:

    FORCEINLINE static LPCUTF8 GetName(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).className;
    }

    FORCEINLINE static LPCUTF8 GetNamespace(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).nameSpace;
    }

    static void CheckConsistency()
    {
        LIMITED_METHOD_CONTRACT;

        for (int i = 0; i < (int)ARRAY_SIZE(info); i++)
        {
            _ASSERTE(info[i].type == i);
        }
    }

    FORCEINLINE static CorInfoGCType GetGCType(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).gcType;
    }
    FORCEINLINE static CorInfoGCType GetGCType_NoThrow(CorElementType type)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetTypeInfo_NoThrow(type).gcType;
    }

    static BOOL IsObjRef(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return (GetGCType(type) == TYPE_GC_REF);
    }
    static BOOL IsObjRef_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return (GetGCType_NoThrow(type) == TYPE_GC_REF);
    }

    static BOOL IsByRef(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return (GetGCType(type) == TYPE_GC_BYREF);
    }
    static BOOL IsByRef_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return (GetGCType_NoThrow(type) == TYPE_GC_BYREF);
    }

    FORCEINLINE static BOOL IsGenericVariable(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).isGenVar;
    }
    FORCEINLINE static BOOL IsGenericVariable_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).isGenVar;
    }

    FORCEINLINE static BOOL IsArray(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).isArray;
    }
    FORCEINLINE static BOOL IsArray_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).isArray;
    }

    FORCEINLINE static BOOL IsFloat(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).isFloat;
    }
    FORCEINLINE static BOOL IsFloat_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).isFloat;
    }

    FORCEINLINE static BOOL IsModifier(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).isModifier;
    }
    FORCEINLINE static BOOL IsModifier_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).isModifier;
    }

    FORCEINLINE static BOOL IsPrimitiveType(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).isPrim;
    }
    FORCEINLINE static BOOL IsPrimitiveType_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).isPrim;
    }

    FORCEINLINE static unsigned Size(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo(type).size;
    }
    FORCEINLINE static unsigned Size_NoThrow(CorElementType type)
    {
        WRAPPER_NO_CONTRACT;

        return GetTypeInfo_NoThrow(type).size;
    }

    static CorElementType FindPrimitiveType(LPCUTF8 name);

protected:
    static const CorTypeInfoEntry info[ELEMENT_TYPE_MAX];

};  // class CorTypeInfo


// Returns the address of the payload inside the stackelem
inline void* StackElemEndiannessFixup(void* pStackElem, UINT cbSize) {
    LIMITED_METHOD_CONTRACT;

    BYTE *pRetVal = (BYTE*)pStackElem;

#if BIGENDIAN
    switch (cbSize)
    {
    case 1:
        pRetVal += sizeof(void*)-1;
        break;
    case 2:
        pRetVal += sizeof(void*)-2;
        break;
#ifdef HOST_64BIT
    case 4:
        pRetVal += sizeof(void*)-4;
        break;
#endif
    default:
        // nothing to do
        break;
    }
#endif

    return pRetVal;
}

#endif /* _H_SIGINFOBASE */
