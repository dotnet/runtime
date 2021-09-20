// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: FieldMarshaler.h
//


#ifndef __FieldMarshaler_h__
#define __FieldMarshaler_h__

#include "util.hpp"
#include "mlinfo.h"
#include "eeconfig.h"
#include "olevariant.h"

// Forward references
class EEClassLayoutInfo;
class FieldDesc;
class MethodTable;

//=======================================================================
// Magic number for default struct packing size.
//
// Currently we set this to the packing size of the largest supported
// fundamental type and let the field marshaller downsize where needed.
//=======================================================================
#define DEFAULT_PACKING_SIZE 32

//=======================================================================
// This structure contains information about where a field is placed in a structure, as well as it's size and alignment.
// It is used as part of type-loading to determine native layout and (where applicable) managed sequential layout.
//=======================================================================
struct RawFieldPlacementInfo
{
    UINT32 m_offset;
    UINT32 m_size;
    UINT32 m_alignment;
};

enum class ParseNativeTypeFlags : int
{
    None    = 0x00,
    IsAnsi  = 0x01
};

//=======================================================================
// This function returns TRUE if the type passed in is either a value class or a class and if it has layout information
// and is marshalable. In all other cases it will return FALSE.
//=======================================================================
BOOL IsStructMarshalable(TypeHandle th);

bool IsFieldBlittable(
    Module* pModule,
    mdFieldDef fd,
    SigPointer fieldSig,
    const SigTypeContext* pTypeContext,
    ParseNativeTypeFlags flags
);

// Describes specific categories of native fields.
enum class NativeFieldCategory : short
{
    // The native representation of the field is a floating point field.
    FLOAT,
    // The field has a nested MethodTable* (i.e. a field of a struct, class, or array)
    NESTED,
    // The native representation of the field can be treated as an integer.
    INTEGER,
    // The field is illegal to marshal.
    ILLEGAL
};

class NativeFieldDescriptor
{
public:
    NativeFieldDescriptor();

    NativeFieldDescriptor(PTR_FieldDesc pFD);

    NativeFieldDescriptor(PTR_FieldDesc pFD, NativeFieldCategory flags, ULONG nativeSize, ULONG alignment);

    NativeFieldDescriptor(PTR_FieldDesc pFD, PTR_MethodTable pMT, int numElements = 1);

    NativeFieldDescriptor(const NativeFieldDescriptor& other);

    NativeFieldDescriptor& operator=(const NativeFieldDescriptor& other);

    ~NativeFieldDescriptor() = default;

    NativeFieldCategory GetCategory() const
    {
        return m_category;
    }

    PTR_MethodTable GetNestedNativeMethodTable() const;

    ULONG GetNumElements() const
    {
        CONTRACTL
        {
            PRECONDITION(IsNestedType());
        }
        CONTRACTL_END;

        return nestedTypeAndCount.m_numElements;
    }

    UINT32 NativeSize() const
    {
        if (IsNestedType())
        {
            MethodTable* pMT = GetNestedNativeMethodTable();
            return pMT->GetNativeSize() * GetNumElements();
        }
        else
        {
            return nativeSizeAndAlignment.m_nativeSize;
        }
    }

    UINT32 AlignmentRequirement() const;

    PTR_FieldDesc GetFieldDesc() const;

    UINT32 GetExternalOffset() const
    {
        return m_offset;
    }

    void SetExternalOffset(UINT32 offset)
    {
        m_offset = offset;
    }

    bool IsUnmarshalable() const
    {
        return m_category == NativeFieldCategory::ILLEGAL;
    }

private:
    bool IsNestedType() const
    {
        return m_category == NativeFieldCategory::NESTED;
    }

    PTR_FieldDesc m_pFD;
    union
    {
        struct
        {
            PTR_MethodTable m_pNestedType;
            ULONG m_numElements;
        } nestedTypeAndCount;
        struct
        {
            UINT32 m_nativeSize;
            UINT32 m_alignmentRequirement;
        } nativeSizeAndAlignment;
    };
    UINT32 m_offset;
    NativeFieldCategory m_category;
};

VOID ParseNativeType(Module* pModule,
    SigPointer                  sig,
    PTR_FieldDesc               fd,
    ParseNativeTypeFlags        flags,
    NativeFieldDescriptor* pNFD,
    const SigTypeContext* pTypeContext
#ifdef _DEBUG
    ,
    LPCUTF8                     szNamespace,
    LPCUTF8                     szClassName,
    LPCUTF8                     szFieldName
#endif
);

//=======================================================================
// The classloader stores an intermediate representation of the layout
// metadata in an array of these structures. The dual-pass nature
// is a bit extra overhead but building this structure requiring loading
// other classes (for nested structures) and I'd rather keep this
// next to the other places where we load other classes (e.g. the superclass
// and implemented interfaces.)
//
// Each redirected field gets one entry in LayoutRawFieldInfo.
// The array is terminated by one dummy record whose m_MD == mdMemberDefNil.
//=======================================================================
struct LayoutRawFieldInfo
{
    mdFieldDef  m_MD;             // mdMemberDefNil for end of array
    ULONG       m_sequence;       // sequence # from metadata
    RawFieldPlacementInfo m_placement;
    NativeFieldDescriptor m_nfd;
};


class EEClassNativeLayoutInfo
{
private:
    uint8_t m_alignmentRequirement;
#ifdef UNIX_AMD64_ABI
    bool m_passInRegisters;
#endif
#ifdef FEATURE_HFA
    CorInfoHFAElemType m_hfaType;
#endif
    bool m_isMarshalable;
    uint32_t m_size;
    uint32_t m_numFields;

    // An array of NativeFieldDescriptors off the end of this object, used to drive call-time
    // marshaling of NStruct reference parameters. The number of elements
    // equals m_numFields.
    NativeFieldDescriptor m_nativeFieldDescriptors[0];

    static PTR_EEClassNativeLayoutInfo CollectNativeLayoutFieldMetadataThrowing(MethodTable* pMT);
public:
    static void InitializeNativeLayoutFieldMetadataThrowing(MethodTable* pMT);

    uint32_t GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_size;
    }

    uint8_t GetLargestAlignmentRequirement() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_alignmentRequirement;
    }

    uint32_t GetNumFields() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_numFields;
    }

    NativeFieldDescriptor * GetNativeFieldDescriptors()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_nativeFieldDescriptors[0];
    }

    NativeFieldDescriptor const* GetNativeFieldDescriptors() const
    {
        LIMITED_METHOD_CONTRACT;
        return &m_nativeFieldDescriptors[0];
    }

    CorInfoHFAElemType GetNativeHFATypeRaw() const;

#ifdef FEATURE_HFA
    bool IsNativeHFA() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_hfaType != CORINFO_HFA_ELEM_NONE;
    }

    CorInfoHFAElemType GetNativeHFAType() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_hfaType;
    }

    void SetHFAType(CorInfoHFAElemType hfaType)
    {
        LIMITED_METHOD_CONTRACT;
        // We should call this at most once.
        _ASSERTE(m_hfaType == CORINFO_HFA_ELEM_NONE);
        m_hfaType = hfaType;
    }
#else
    bool IsNativeHFA() const
    {
        return GetNativeHFATypeRaw() != CORINFO_HFA_ELEM_NONE;
    }
    CorInfoHFAElemType GetNativeHFAType() const
    {
        return GetNativeHFATypeRaw();
    }
#endif

#ifdef UNIX_AMD64_ABI
    bool IsNativeStructPassedInRegisters() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_passInRegisters;
    }
    void SetNativeStructPassedInRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        m_passInRegisters = true;
    }
#else
    bool IsNativeStructPassedInRegisters() const
    {
        return false;
    }
#endif

    bool IsMarshalable() const
    {
        return m_isMarshalable;
    }

    void SetIsMarshalable(bool isMarshalable)
    {
        m_isMarshalable = isMarshalable;
    }
};
#endif // __FieldMarshaler_h__
