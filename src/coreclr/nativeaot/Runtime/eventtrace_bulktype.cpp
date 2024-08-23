// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "eventtrace.h"
#include "eventtrace_etw.h"
#include "rhbinder.h"
#include "slist.h"
#include "runtimeinstance.h"
#include "shash.h"
#include "eventtracebase.h"
#include "eventtracepriv.h"
#include "shash.inl"

#if defined(FEATURE_EVENT_TRACE)

//---------------------------------------------------------------------------------------
// BulkTypeValue / BulkTypeEventLogger: These take care of batching up types so they can
// be logged via ETW in bulk
//---------------------------------------------------------------------------------------

BulkTypeValue::BulkTypeValue()
    : cTypeParameters(0)
    , ullSingleTypeParameter(0)
{
    LIMITED_METHOD_CONTRACT;
    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
}

//---------------------------------------------------------------------------------------
//
// Clears a BulkTypeValue so it can be reused after the buffer is flushed to ETW
//

void BulkTypeValue::Clear()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
    cTypeParameters = 0;
    ullSingleTypeParameter = 0;
}

//---------------------------------------------------------------------------------------
// BulkTypeEventLogger is a helper class to batch up type information and then flush to
// ETW once the event reaches its max # descriptors

//---------------------------------------------------------------------------------------
//
// Fire an ETW event for all the types we batched so far, and then reset our state
// so we can start batching new types at the beginning of the array.
//
void BulkTypeEventLogger::FireBulkTypeEvent()
{
    LIMITED_METHOD_CONTRACT;

    if (m_nBulkTypeValueCount == 0)
    {
        // No types were batched up, so nothing to send
        return;
    }
    UINT16 nClrInstanceID = GetClrInstanceId();

    if(m_pBulkTypeEventBuffer == NULL)
    {
        // The buffer could not be allocated when this object was created, so bail.
        return;
    }

    UINT iSize = 0;

    for (int iTypeData = 0; iTypeData < m_nBulkTypeValueCount; iTypeData++)
    {
        BulkTypeValue& target = m_rgBulkTypeValues[iTypeData];

        // Do fixed-size data as one bulk copy
        memcpy(
                m_pBulkTypeEventBuffer + iSize,
                &(target.fixedSizedData),
                sizeof(target.fixedSizedData));
        iSize += sizeof(target.fixedSizedData);

        // Do var-sized data individually per field
        // No name in event, so just the null terminator
        m_pBulkTypeEventBuffer[iSize++] = 0;
        m_pBulkTypeEventBuffer[iSize++] = 0;

        // Type parameter count
        ULONG cTypeParams = target.cTypeParameters;
        ULONG *ptrInt = (ULONG*)(m_pBulkTypeEventBuffer + iSize);
        *ptrInt = cTypeParams;
        iSize += sizeof(ULONG);

        // Type parameter array
        if (cTypeParams == 1)
        {
            memcpy(m_pBulkTypeEventBuffer + iSize, &target.ullSingleTypeParameter, sizeof(ULONGLONG) * cTypeParams);
            iSize += sizeof(ULONGLONG) * cTypeParams;
        }
        else if (cTypeParams > 1)
        {
            ASSERT_UNCONDITIONALLY("unexpected value of cTypeParams greater than 1");
        }
    }

    FireEtwBulkType(m_nBulkTypeValueCount, GetClrInstanceId(), iSize, m_pBulkTypeEventBuffer);

    // Reset state
    m_nBulkTypeValueCount = 0;
    m_nBulkTypeValueByteCount = 0;
}

// We keep a hash of these to keep track of:
//     * Which types have been logged through ETW (so we can avoid logging dupe Type
//         events), and
//     * GCSampledObjectAllocation stats to help with "smart sampling" which
//         dynamically adjusts sampling rate of objects by type.
// See code:LoggedTypesFromModuleTraits

class LoggedTypesTraits : public  DefaultSHashTraits<MethodTable*>
{
public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef MethodTable* key_t;

    static key_t GetKey(const element_t &e)
    {
        LIMITED_METHOD_CONTRACT;
        return e;
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return (k1 == k2);
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t) (uintptr_t) k;
    }

    static bool IsNull(const element_t &e)
    {
        LIMITED_METHOD_CONTRACT;
        return (e == NULL);
    }

    static const element_t Null()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }
};

enum class CorElementType : uint8_t
{
    ELEMENT_TYPE_END = 0x0,

    ELEMENT_TYPE_BOOLEAN = 0x2,
    ELEMENT_TYPE_CHAR = 0x3,
    ELEMENT_TYPE_I1 = 0x4,
    ELEMENT_TYPE_U1 = 0x5,
    ELEMENT_TYPE_I2 = 0x6,
    ELEMENT_TYPE_U2 = 0x7,
    ELEMENT_TYPE_I4 = 0x8,
    ELEMENT_TYPE_U4 = 0x9,
    ELEMENT_TYPE_I8 = 0xa,
    ELEMENT_TYPE_U8 = 0xb,
    ELEMENT_TYPE_R4 = 0xc,
    ELEMENT_TYPE_R8 = 0xd,

    ELEMENT_TYPE_I = 0x18,
    ELEMENT_TYPE_U = 0x19,
};

static CorElementType ElementTypeToCorElementType(EETypeElementType elementType)
{
    switch (elementType)
    {
    case EETypeElementType::ElementType_Boolean:
        return CorElementType::ELEMENT_TYPE_BOOLEAN;
    case EETypeElementType::ElementType_Char:
        return CorElementType::ELEMENT_TYPE_CHAR;
    case EETypeElementType::ElementType_SByte:
        return CorElementType::ELEMENT_TYPE_I1;
    case EETypeElementType::ElementType_Byte:
        return CorElementType::ELEMENT_TYPE_U1;
    case EETypeElementType::ElementType_Int16:
        return CorElementType::ELEMENT_TYPE_I2;
    case EETypeElementType::ElementType_UInt16:
        return CorElementType::ELEMENT_TYPE_U2;
    case EETypeElementType::ElementType_Int32:
        return CorElementType::ELEMENT_TYPE_I4;
    case EETypeElementType::ElementType_UInt32:
        return CorElementType::ELEMENT_TYPE_U4;
    case EETypeElementType::ElementType_Int64:
        return CorElementType::ELEMENT_TYPE_I8;
    case EETypeElementType::ElementType_UInt64:
        return CorElementType::ELEMENT_TYPE_U8;
    case EETypeElementType::ElementType_Single:
        return CorElementType::ELEMENT_TYPE_R4;
    case EETypeElementType::ElementType_Double:
        return CorElementType::ELEMENT_TYPE_R8;
    case EETypeElementType::ElementType_IntPtr:
        return CorElementType::ELEMENT_TYPE_I;
    case EETypeElementType::ElementType_UIntPtr:
        return CorElementType::ELEMENT_TYPE_U;
    }
    return CorElementType::ELEMENT_TYPE_END;
}

// Avoid reporting the same type twice by keeping a hash of logged types.
SHash<LoggedTypesTraits>* s_loggedTypesHash = NULL;

//---------------------------------------------------------------------------------------
//
// Interrogates MethodTable for the info that's interesting to include in the BulkType ETW
// event.  Does not recursively call self for type parameters.
//
// Arguments:
//      * pEEType - MethodTable to log info about
//
// Return Value:
//      Index into internal array where the info got batched.  Or -1 if there was a
//      failure.
//
int BulkTypeEventLogger::LogSingleType(MethodTable * pEEType)
{
#ifdef MULTIPLE_HEAPS
    // We need to add a lock to protect the types hash for Server GC.
    ASSERT_UNCONDITIONALLY("Add a lock to protect s_loggedTypesHash access!");
#endif
    //Avoid logging the same type twice, but using the hash of loggged types.
    if (s_loggedTypesHash == NULL)
        s_loggedTypesHash = new SHash<LoggedTypesTraits>();
    MethodTable* preexistingType = s_loggedTypesHash->Lookup(pEEType);
    if (preexistingType != NULL)
    {
        return -1;
    }
    else
    {
        s_loggedTypesHash->Add(pEEType);
    }

    // If there's no room for another type, flush what we've got
    if (m_nBulkTypeValueCount == _countof(m_rgBulkTypeValues))
    {
        FireBulkTypeEvent();
    }

    _ASSERTE(m_nBulkTypeValueCount < _countof(m_rgBulkTypeValues));

    BulkTypeValue * pVal = &m_rgBulkTypeValues[m_nBulkTypeValueCount];

    // Clear out pVal before filling it out (array elements can get reused if there
    // are enough types that we need to flush to multiple events).
    pVal->Clear();

    pVal->fixedSizedData.TypeID = (ULONGLONG) pEEType;
    pVal->fixedSizedData.Flags = kEtwTypeFlagsModuleBaseAddress;
    pVal->fixedSizedData.CorElementType = (BYTE)ElementTypeToCorElementType(pEEType->GetElementType());

    ULONGLONG * rgTypeParamsForEvent = NULL;
    ULONGLONG typeParamForNonGenericType = 0;

    // Determine this MethodTable's module.
    RuntimeInstance * pRuntimeInstance = GetRuntimeInstance();

    // EEType for GC statics are not fully populated and they do not have a valid TypeManager. We will identify them by checking for `ElementType_Unknown`.
    // We will not be able to get the osModuleHandle for these
    ULONGLONG osModuleHandle = 0;
    if (pEEType->GetElementType() != ElementType_Unknown)
    {
        osModuleHandle = (ULONGLONG) pEEType->GetTypeManagerPtr()->AsTypeManager()->GetOsModuleHandle();
    }
    pVal->fixedSizedData.ModuleID = osModuleHandle;

    if (pEEType->IsParameterizedType())
    {
        ASSERT(pEEType->IsArray());
        // Array
        pVal->fixedSizedData.Flags |= kEtwTypeFlagsArray;
        pVal->cTypeParameters = 1;
        pVal->ullSingleTypeParameter = (ULONGLONG) pEEType->GetRelatedParameterType();
    }
    else
    {
        // Note: For generic types, we do not necessarily know the generic parameters.
        // So we leave it to the profiler at post-processing time to determine that via
        // the PDBs.  We'll leave pVal->cTypeParameters as 0, even though there could be
        // type parameters.

        // Flags
        if (pEEType->HasFinalizer())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsFinalizable;
        }

        // Note: Pn runtime knows nothing about delegates, and there are no CCWs/RCWs.
        // So no other type flags are applicable to set
    }

    ULONGLONG rvaType = osModuleHandle == 0 ? 0 : (ULONGLONG(pEEType) - osModuleHandle);
    pVal->fixedSizedData.TypeNameID = (DWORD) rvaType;

    // Now that we know the full size of this type's data, see if it fits in our
    // batch or whether we need to flush

    int cbVal = pVal->GetByteCountInEvent();
    if (cbVal > kMaxBytesTypeValues)
    {
        // This type is apparently so huge, it's too big to squeeze into an event, even
        // if it were the only type batched in the whole event.  Bail
        ASSERT(!"Type too big to log via ETW");
        return -1;
    }

    if (m_nBulkTypeValueByteCount + cbVal > kMaxBytesTypeValues)
    {
        // Although this type fits into the array, its size is so big that the entire
        // array can't be logged via ETW. So flush the array, and start over by
        // calling ourselves--this refetches the type info and puts it at the
        // beginning of the array.  Since we know this type is small enough to be
        // batched into an event on its own, this recursive call will not try to
        // call itself again.
        FireBulkTypeEvent();
        return LogSingleType(pEEType);
    }

    // The type fits into the batch, so update our state
    m_nBulkTypeValueCount++;
    m_nBulkTypeValueByteCount += cbVal;
    return m_nBulkTypeValueCount - 1;       // Index of type we just added
}

//---------------------------------------------------------------------------------------
//
// Batches up ETW information for a type and pops out to recursively call
// ETW::TypeSystemLog::LogTypeAndParametersIfNecessary for any
// "type parameters".  Generics info is not reliably available, so "type parameter"
// really just refers to the type of array elements if thAsAddr is an array.
//
// Arguments:
//      * thAsAddr - MethodTable to log
//      * typeLogBehavior - Ignored in Redhawk builds
//

void BulkTypeEventLogger::LogTypeAndParameters(uint64_t thAsAddr)
{
    MethodTable * pEEType = (MethodTable *) thAsAddr;

    // Batch up this type.  This grabs useful info about the type, including any
    // type parameters it may have, and sticks it in m_rgBulkTypeValues
    int iBulkTypeEventData = LogSingleType(pEEType);
    if (iBulkTypeEventData == -1)
    {
        // There was a failure trying to log the type, so don't bother with its type
        // parameters
        return;
    }

    // Look at the type info we just batched, so we can get the type parameters
    BulkTypeValue * pVal = &m_rgBulkTypeValues[iBulkTypeEventData];

    // We're about to recursively call ourselves for the type parameters, so make a
    // local copy of their type handles first (else, as we log them we could flush
    // and clear out m_rgBulkTypeValues, thus trashing pVal)
    DWORD cTypeParams = pVal->cTypeParameters;
    if (cTypeParams == 1)
    {
        LogTypeAndParameters(pVal->ullSingleTypeParameter);
    }
    else if (cTypeParams > 1)
    {
        
        ASSERT_UNCONDITIONALLY("unexpected value of cTypeParams greater than 1");
    }
}

void BulkTypeEventLogger::Cleanup()
{
    if (s_loggedTypesHash != NULL)
    {
        delete s_loggedTypesHash;
        s_loggedTypesHash = NULL;
    }
}

#endif // defined(FEATURE_EVENT_TRACE)
