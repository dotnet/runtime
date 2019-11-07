// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: FieldMarshaler.h
//


#ifndef __FieldMarshaler_h__
#define __FieldMarshaler_h__

#include "util.hpp"
#include "mlinfo.h"
#include "eeconfig.h"
#include "olevariant.h"
#include "nativefieldflags.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif // FEATURE_PREJIT

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
    IsAnsi  = 0x01,
#ifdef FEATURE_COMINTEROP
    IsWinRT = 0x02,
#endif // FEATURE_COMINTEROP
};

VOID ParseNativeType(Module*    pModule,
    SigPointer                  sig,
    mdFieldDef                  fd,
    ParseNativeTypeFlags        flags,
    NativeFieldDescriptor*      pNFD,
    const SigTypeContext *      pTypeContext
#ifdef _DEBUG
    ,
    LPCUTF8                     szNamespace,
    LPCUTF8                     szClassName,
    LPCUTF8                     szFieldName
#endif
);

//=======================================================================
// This function returns TRUE if the type passed in is either a value class or a class and if it has layout information
// and is marshalable. In all other cases it will return FALSE.
//=======================================================================
BOOL IsStructMarshalable(TypeHandle th);

class NativeFieldDescriptor
{
public:
    NativeFieldDescriptor();

    NativeFieldDescriptor(NativeFieldFlags flags, ULONG nativeSize, ULONG alignment);

    NativeFieldDescriptor(PTR_MethodTable pMT, int numElements = 1, bool isBlittable = false);

    NativeFieldDescriptor(const NativeFieldDescriptor& other);

    NativeFieldDescriptor& operator=(const NativeFieldDescriptor& other);

    ~NativeFieldDescriptor() = default;

#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)
    void Save(DataImage * image)
    {
        STANDARD_VM_CONTRACT;
    }

    void Fixup(DataImage * image)
    {
        STANDARD_VM_CONTRACT;

        image->FixupFieldDescPointer(this, &m_pFD);

        if (m_isNestedType)
        {
            image->FixupMethodTablePointer(this, &nestedTypeAndCount.m_pNestedType);
        }
    }
#endif // defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)

#ifdef _DEBUG
    BOOL IsRestored() const;
#endif

    void Restore();

    NativeFieldFlags GetNativeFieldFlags() const
    {
        return m_flags;
    }

    PTR_MethodTable GetNestedNativeMethodTable() const;

    ULONG GetNumElements() const
    {
        CONTRACTL
        {
            PRECONDITION(m_isNestedType);
        }
        CONTRACTL_END;

        return nestedTypeAndCount.m_numElements;
    }

    UINT32 NativeSize() const
    {
        if (m_isNestedType)
        {
            return GetNestedNativeMethodTable()->GetLayoutInfo()->GetNativeSize() * GetNumElements();
        }
        else
        {
            return nativeSizeAndAlignment.m_nativeSize;
        }
    }

    UINT32 AlignmentRequirement() const
    {
        if (m_isNestedType)
        {
            return GetNestedNativeMethodTable()->GetLayoutInfo()->GetLargestAlignmentRequirementOfAllMembers();
        }
        else
        {
            return nativeSizeAndAlignment.m_alignmentRequirement;
        }
    }

    PTR_FieldDesc GetFieldDesc() const;

    void SetFieldDesc(PTR_FieldDesc pFD);

    UINT32 GetExternalOffset() const
    {
        return m_offset;
    }

    void SetExternalOffset(UINT32 offset)
    {
        m_offset = offset;
    }

    BOOL IsUnmarshalable() const
    {
        return m_flags == NATIVE_FIELD_CATEGORY_ILLEGAL ? TRUE : FALSE;
    }

    BOOL IsBlittable() const
    {
        return m_flags & NATIVE_FIELD_SUBCATEGORY_BLITTABLE ? TRUE : FALSE;
    }

private:
    RelativeFixupPointer<PTR_FieldDesc> m_pFD;
    union
    {
        struct
        {
            RelativeFixupPointer<PTR_MethodTable> m_pNestedType;
            ULONG m_numElements;
        } nestedTypeAndCount;
        struct
        {
            UINT32 m_nativeSize;
            UINT32 m_alignmentRequirement;
        } nativeSizeAndAlignment;
    };
    UINT32 m_offset;
    NativeFieldFlags m_flags;
    bool m_isNestedType;
};

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
    RawFieldPlacementInfo m_nativePlacement; // Description of the native field placement
    ULONG       m_sequence;       // sequence # from metadata


    // The LayoutKind.Sequential attribute now affects managed layout as well.
    // So we need to keep a parallel set of layout data for the managed side. The Size and AlignmentReq
    // is redundant since we can figure it out from the sig but since we're already accessing the sig
    // in ParseNativeType, we might as well capture it at that time.
    RawFieldPlacementInfo m_managedPlacement;

    NativeFieldDescriptor m_nfd;
};

#endif // __FieldMarshaler_h__
