// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: FieldMarshaler.h
//

//
// FieldMarshalers are used to allow CLR programs to allocate and access
// native structures for interop purposes. FieldMarshalers are actually normal GC
// objects with a class, but instead of keeping fields in the GC object,
// it keeps a hidden pointer to a fixed memory block (which may have been
// allocated by a third party.) Field accesses to FieldMarshalers are redirected
// to this fixed block.
//


#ifndef __FieldMarshaler_h__
#define __FieldMarshaler_h__

#include "util.hpp"
#include "mlinfo.h"
#include "eeconfig.h"
#include "olevariant.h"

#ifdef FEATURE_COMINTEROP
#endif  // FEATURE_COMINTEROP

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif // FEATURE_PREJIT

// Forward refernces
class EEClassLayoutInfo;
class FieldDesc;
class MethodTable;

class FieldMarshaler_NestedLayoutClass;
class FieldMarshaler_NestedValueClass;
class FieldMarshaler_StringUni;
class FieldMarshaler_StringAnsi;
class FieldMarshaler_FixedStringUni;
class FieldMarshaler_FixedStringAnsi;
class FieldMarshaler_FixedArray;
class FieldMarshaler_FixedCharArrayAnsi;
class FieldMarshaler_Delegate;
class FieldMarshaler_Illegal;
class FieldMarshaler_Copy1;
class FieldMarshaler_Copy2;
class FieldMarshaler_Copy4;
class FieldMarshaler_Copy8;
class FieldMarshaler_Ansi;
class FieldMarshaler_WinBool;
class FieldMarshaler_CBool;
class FieldMarshaler_Decimal;
class FieldMarshaler_Date;
#ifdef FEATURE_COMINTEROP
class FieldMarshaler_SafeArray;
class FieldMarshaler_BSTR;
class FieldMarshaler_HSTRING;
class FieldMarshaler_Interface;
class FieldMarshaler_Variant;
class FieldMarshaler_VariantBool;
class FieldMarshaler_DateTimeOffset;
class FieldMarshaler_SystemType;
class FieldMarshaler_Exception;
class FieldMarshaler_Nullable;
#endif // FEATURE_COMINTEROP

VOID NStructFieldTypeToString(FieldMarshaler* pFM, SString& strNStructFieldType);

//=======================================================================
// Each possible COM+/Native pairing of data type has a
// NLF_* id. This is used to select the marshaling code.
//=======================================================================
#undef DEFINE_NFT
#define DEFINE_NFT(name, nativesize, fWinRTSupported) name,
enum NStructFieldType
{
#include "nsenums.h"
    NFT_COUNT
};


//=======================================================================
// Magic number for default struct packing size.
//=======================================================================
#if defined(_TARGET_X86_) && defined(UNIX_X86_ABI)
// A double is 4-byte aligned on GCC (without -malign-dobule)
#define DEFAULT_PACKING_SIZE 4
#else // _TARGET_X86_ && UNIX_X86_ABI
#define DEFAULT_PACKING_SIZE 8
#endif // !_TARGET_X86_ || !UNIX_X86_ABI


//=======================================================================
// This is invoked from the class loader while building the data structures for a type.
// This function checks if explicit layout metadata exists.
//
// Returns:
//  TRUE    - yes, there's layout metadata
//  FALSE   - no, there's no layout.
//  fail    - throws a typeload exception
//
// If S_OK,
//   *pNLType            gets set to nltAnsi or nltUnicode
//   *pPackingSize       declared packing size
//   *pfExplicitoffsets  offsets explicit in metadata or computed?
//=======================================================================
BOOL HasLayoutMetadata(Assembly* pAssembly, IMDInternalImport *pInternalImport, mdTypeDef cl, 
                            MethodTable *pParentMT, BYTE *pPackingSize, BYTE *pNLTType,
                            BOOL *pfExplicitOffsets);


//=======================================================================
// This function returns TRUE if the type passed in is either a value class or a class and if it has layout information 
// and is marshalable. In all other cases it will return FALSE. 
//=======================================================================
BOOL IsStructMarshalable(TypeHandle th);

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
// WARNING!! Before you change this struct see the comment above the m_FieldMarshaler field
//=======================================================================
struct LayoutRawFieldInfo
{
    mdFieldDef  m_MD;             // mdMemberDefNil for end of array
    UINT8       m_nft;            // NFT_* value
    UINT32      m_offset;         // native offset of field
    UINT32      m_cbNativeSize;   // native size of field in bytes
    ULONG       m_sequence;       // sequence # from metadata
    BOOL        m_fIsOverlapped;


    //----- Post v1.0 addition: The LayoutKind.Sequential attribute now affects managed layout as well.
    //----- So we need to keep a parallel set of layout data for the managed side. The Size and AlignmentReq
    //----- is redundant since we can figure it out from the sig but since we're already accessing the sig
    //----- in ParseNativeType, we might as well capture it at that time.
    UINT32      m_managedSize;    // managed size of field
    UINT32      m_managedAlignmentReq; // natural alignment of field
    UINT32      m_managedOffset;  // managed offset of field
    UINT32      m_pad;            // needed to keep m_FieldMarshaler 8-byte aligned

    // WARNING!
    // We in-place create a field marshaler in the following
    // memory, so keep it 8-byte aligned or 
    // the vtable pointer initialization will cause a 
    // misaligned memory write on IA64.
    // The entire struct's size must also be multiple of 8 bytes
    struct
    {
        private:
            char m_space[MAXFIELDMARSHALERSIZE];
    } m_FieldMarshaler;
};


//=======================================================================
// 
//=======================================================================

VOID LayoutUpdateNative(LPVOID *ppProtectedManagedData, SIZE_T offsetbias, MethodTable *pMT, BYTE* pNativeData, OBJECTREF *ppCleanupWorkListOnStack);
VOID LayoutUpdateCLR(LPVOID *ppProtectedManagedData, SIZE_T offsetbias, MethodTable *pMT, BYTE *pNativeData);
VOID LayoutDestroyNative(LPVOID pNative, MethodTable *pMT);

VOID FmtClassUpdateNative(OBJECTREF *ppProtectedManagedData, BYTE *pNativeData, OBJECTREF *ppCleanupWorkListOnStack);
VOID FmtClassUpdateCLR(OBJECTREF *ppProtectedManagedData, BYTE *pNativeData);
VOID FmtClassDestroyNative(LPVOID pNative, MethodTable *pMT);

VOID FmtValueTypeUpdateNative(LPVOID pProtectedManagedData, MethodTable *pMT, BYTE *pNativeData, OBJECTREF *ppCleanupWorkListOnStack);
VOID FmtValueTypeUpdateCLR(LPVOID pProtectedManagedData, MethodTable *pMT, BYTE *pNativeData);


//=======================================================================
// Abstract base class. Each type of NStruct reference field extends
// this class and implements the necessary methods.
//
//   UpdateNativeImpl
//       - this method receives a COM+ field value and a pointer to
//         native field inside the fixed portion. it should marshal
//         the COM+ value to a new native instance and store it
//         inside *pNativeValue. Do not destroy the value you overwrite
//         in *pNativeValue.
//
//         may throw COM+ exceptions
//
//   UpdateCLRImpl
//       - this method receives a read-only pointer to the native field inside
//         the fixed portion. it should marshal the native value to
//         a new CLR instance and store it in *ppCLRValue.
//         (the caller keeps *ppCLRValue gc-protected.)
//
//         may throw CLR exceptions
//
//   DestroyNativeImpl
//       - should do the type-specific deallocation of a native instance.
//         if the type has a "NULL" value, this method should
//         overwrite the field with this "NULL" value (whether or not
//         it does, however, it's considered a bug to depend on the
//         value left over after a DestroyNativeImpl.)
//
//         must NOT throw a CLR exception
//
//   NativeSizeImpl
//       - returns the size, in bytes, of the native version of the field.
//
//   AlignmentRequirementImpl
//       - returns one of 1,2,4 or 8; indicating the "natural" alignment
//         of the native field. In general,
//
//            for scalars, the AR is equal to the size
//            for arrays,  the AR is that of a single element
//            for structs, the AR is that of the member with the largest AR
//
//
//=======================================================================


#ifndef DACCESS_COMPILE

#define UNUSED_METHOD_IMPL(PROTOTYPE)                   \
    PROTOTYPE                                           \
    {                                                   \
        LIMITED_METHOD_CONTRACT;                                  \
        _ASSERTE(!"Not supposed to get here.");         \
    }

#define ELEMENT_SIZE_IMPL(NativeSize, AlignmentReq)     \
    UINT32 NativeSizeImpl() const                       \
    {                                                   \
        LIMITED_METHOD_CONTRACT;                                  \
        return NativeSize;                              \
    }                                                   \
    UINT32 AlignmentRequirementImpl() const             \
    {                                                   \
        LIMITED_METHOD_CONTRACT;                                  \
        return AlignmentReq;                            \
    }

#define SCALAR_MARSHALER_IMPL(NativeSize, AlignmentReq) \
    BOOL IsScalarMarshalerImpl() const                  \
    {                                                   \
        LIMITED_METHOD_CONTRACT;                                  \
        return TRUE;                                    \
    }                                                   \
    ELEMENT_SIZE_IMPL(NativeSize, AlignmentReq)


//=======================================================================
//
// FieldMarshaler's are constructed in place and replicated via bit-wise
// copy, so you can't have a destructor. Make sure you don't define a 
// destructor in derived classes!!
// We used to enforce this by defining a private destructor, by the C++
// compiler doesn't allow that anymore.
//
//=======================================================================

class FieldMarshaler
{
public:
    VOID UpdateNative(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLR(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNative(LPVOID pNativeValue) const;
    UINT32 NativeSize() const;
    UINT32 AlignmentRequirement() const;
    BOOL IsScalarMarshaler() const;
    BOOL IsNestedValueClassMarshaler() const;
    VOID ScalarUpdateNative(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLR(const VOID *pNative, LPVOID pCLR) const;
    VOID NestedValueClassUpdateNative(const VOID **ppProtectedCLR, SIZE_T startoffset, LPVOID pNative, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID NestedValueClassUpdateCLR(const VOID *pNative, LPVOID *ppProtectedCLR, SIZE_T startoffset) const;
#ifdef FEATURE_PREJIT
    void Save(DataImage *image);
    void Fixup(DataImage *image);
#endif // FEATURE_PREJIT
    void Restore();

    VOID DestroyNativeImpl(LPVOID pNativeValue) const
    {
        LIMITED_METHOD_CONTRACT;
    }

    BOOL IsScalarMarshalerImpl() const
    {
        LIMITED_METHOD_CONTRACT; 
        return FALSE;
    }

    BOOL IsNestedValueClassMarshalerImpl() const
    {
        LIMITED_METHOD_CONTRACT; 
        return FALSE;
    }

    UNUSED_METHOD_IMPL(VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const)
    UNUSED_METHOD_IMPL(VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const)
    UNUSED_METHOD_IMPL(VOID NestedValueClassUpdateNativeImpl(const VOID **ppProtectedCLR, SIZE_T startoffset, LPVOID pNative, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID NestedValueClassUpdateCLRImpl(const VOID *pNative, LPVOID *ppProtectedCLR, SIZE_T startoffset) const)

    // 
    // Methods for saving & restoring in prejitted images:
    //

    NStructFieldType GetNStructFieldType() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_nft;
    }

    void SetNStructFieldType(NStructFieldType nft)
    {
        LIMITED_METHOD_CONTRACT;
        m_nft = nft;
    }

#ifdef FEATURE_PREJIT
    void SaveImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
    }

    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;

        image->FixupFieldDescPointer(this, &m_pFD);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

#ifdef FEATURE_PREJIT
        Module::RestoreFieldDescPointer(&m_pFD);
#endif // FEATURE_PREJIT
    }

    void SetFieldDesc(FieldDesc* pFD)
    {
        LIMITED_METHOD_CONTRACT;
        m_pFD.SetValue(pFD);
    }

    FieldDesc* GetFieldDesc() const
    {
        CONTRACT (FieldDesc*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SO_TOLERANT;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pFD.GetValue();
    }

    void SetExternalOffset(UINT32 dwExternalOffset)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwExternalOffset = dwExternalOffset;
    }

    UINT32 GetExternalOffset() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwExternalOffset;
    }
    
protected:
    FieldMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
        
#ifdef _DEBUG
        m_dwExternalOffset = 0xcccccccc;
#endif
    }

    static inline void RestoreHelper(FixupPointer<PTR_MethodTable> *ppMT)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(ppMT));
        }
        CONTRACTL_END;

#ifdef FEATURE_PREJIT
        Module::RestoreMethodTablePointer(ppMT);
#else // FEATURE_PREJIT
        // without NGEN we only have to make sure that the type is fully loaded
        ClassLoader::EnsureLoaded(ppMT->GetValue());
#endif // FEATURE_PREJIT
    }

#ifdef _DEBUG
    static inline BOOL IsRestoredHelper(FixupPointer<PTR_MethodTable> pMT)
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
        return pMT.IsNull() || (!pMT.IsTagged() && pMT.GetValue()->IsRestored());
#else // FEATURE_PREJIT
        // putting the IsFullyLoaded check here is tempting but incorrect
        return TRUE;
#endif // FEATURE_PREJIT
    }
#endif // _DEBUG


    FixupPointer<PTR_FieldDesc> m_pFD;      // FieldDesc
    UINT32           m_dwExternalOffset;    // offset of field in the fixed portion
    NStructFieldType m_nft;
};



#ifdef FEATURE_COMINTEROP

//=======================================================================
// BSTR <--> System.String
//=======================================================================
class FieldMarshaler_BSTR : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(BSTR), sizeof(BSTR))
};

//=======================================================================
// HSTRING <--> System.String
//=======================================================================
class FieldMarshaler_HSTRING : public FieldMarshaler
{
public:
    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(HSTRING), sizeof(HSTRING))
};

//=======================================================================
// Windows.Foundation.IReference`1 <--> System.Nullable`1
//=======================================================================
class FieldMarshaler_Nullable : public FieldMarshaler
{
public:

    FieldMarshaler_Nullable(MethodTable* pMT)
    {
        m_pNullableTypeMT.SetValue(pMT);
    }

    BOOL IsNullableMarshalerImpl() const
    {
        LIMITED_METHOD_CONTRACT;
        return TRUE;
    }

    //UnImplementedMethods.
    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    ELEMENT_SIZE_IMPL(sizeof(IUnknown*), sizeof(IUnknown*))

    //ImplementedMethods
    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const;
    VOID DestroyNativeImpl(const VOID* pNativeValue) const;
    MethodDesc* GetMethodDescForGenericInstantiation(MethodDesc* pMD) const;

    BOOL IsScalarMarshalerImpl() const
    {
        LIMITED_METHOD_CONTRACT; 
        return TRUE;
    }

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pNullableTypeMT);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pNullableTypeMT);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return IsRestoredHelper(m_pNullableTypeMT);
    }
#endif

    MethodTable *GetMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pNullableTypeMT.GetValue();
    }

private:
    FixupPointer<PTR_MethodTable> m_pNullableTypeMT;
};


//=======================================================================
// Windows.UI.Xaml.Interop.TypeName <--> System.Type
//=======================================================================
class FieldMarshaler_SystemType : public FieldMarshaler
{
public:
    VOID UpdateNativeImpl(OBJECTREF * pCLRValue, LPVOID pNativeValue, OBJECTREF * ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID * pNativeValue, OBJECTREF * ppProtectedCLRValue, OBJECTREF * ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;
    
    ELEMENT_SIZE_IMPL(sizeof(HSTRING), sizeof(HSTRING))
};

//=======================================================================
// Windows.Foundation.HResult <--> System.Exception
// Note: The WinRT struct has exactly 1 field, Value (an HRESULT)
//=======================================================================
class FieldMarshaler_Exception : public FieldMarshaler
{
public:
    VOID UpdateNativeImpl(OBJECTREF * pCLRValue, LPVOID pNativeValue, OBJECTREF * ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID * pNativeValue, OBJECTREF * ppProtectedCLRValue, OBJECTREF * ppProtectedOldCLRValue) const;
    
    ELEMENT_SIZE_IMPL(sizeof(HRESULT), sizeof(HRESULT))
};

#endif // FEATURE_COMINTEROP



//=======================================================================
// Embedded struct <--> LayoutClass
//=======================================================================
class FieldMarshaler_NestedLayoutClass : public FieldMarshaler
{
public:
    FieldMarshaler_NestedLayoutClass(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        m_pNestedMethodTable.SetValue(pMT);
    }

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    UINT32 NativeSizeImpl() const;
    UINT32 AlignmentRequirementImpl() const;
    
#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pNestedMethodTable);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pNestedMethodTable);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return IsRestoredHelper(m_pNestedMethodTable);
    }
#endif

    MethodTable *GetMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pNestedMethodTable.GetValue();
    }

private:
    // MethodTable of nested FieldMarshaler.
    FixupPointer<PTR_MethodTable> m_pNestedMethodTable;
};


//=======================================================================
// Embedded struct <--> ValueClass
//=======================================================================
class FieldMarshaler_NestedValueClass : public FieldMarshaler
{
public:
    FieldMarshaler_NestedValueClass(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        m_pNestedMethodTable.SetValue(pMT);
    }

    BOOL IsNestedValueClassMarshalerImpl() const
    {
        LIMITED_METHOD_CONTRACT;
        return TRUE;
    }

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    UINT32 NativeSizeImpl() const;
    UINT32 AlignmentRequirementImpl() const;
    VOID NestedValueClassUpdateNativeImpl(const VOID **ppProtectedCLR, SIZE_T startoffset, LPVOID pNative, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID NestedValueClassUpdateCLRImpl(const VOID *pNative, LPVOID *ppProtectedCLR, SIZE_T startoffset) const;

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    { 
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pNestedMethodTable);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pNestedMethodTable);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return IsRestoredHelper(m_pNestedMethodTable);
    }
#endif

    BOOL IsBlittable()
    {
        WRAPPER_NO_CONTRACT;
        return GetMethodTable()->IsBlittable();
    }

    MethodTable *GetMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pNestedMethodTable.GetValue();
    }


private:
    // MethodTable of nested NStruct.
    FixupPointer<PTR_MethodTable> m_pNestedMethodTable;
};


//=======================================================================
// LPWSTR <--> System.String
//=======================================================================
class FieldMarshaler_StringUni : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPWSTR), sizeof(LPWSTR))
};

//=======================================================================
// LPUTF8STR <--> System.String
//=======================================================================
class FieldMarshaler_StringUtf8 : public FieldMarshaler
{
public:

	VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
	VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
	VOID DestroyNativeImpl(LPVOID pNativeValue) const;

	ELEMENT_SIZE_IMPL(sizeof(LPSTR), sizeof(LPSTR))
};

//=======================================================================
// LPSTR <--> System.String
//=======================================================================
class FieldMarshaler_StringAnsi : public FieldMarshaler
{
public:
    FieldMarshaler_StringAnsi(BOOL BestFit, BOOL ThrowOnUnmappableChar) : 
        m_BestFitMap(!!BestFit), m_ThrowOnUnmappableChar(!!ThrowOnUnmappableChar)
    {
        WRAPPER_NO_CONTRACT;
    }

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPSTR), sizeof(LPSTR))
    
    BOOL GetBestFit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_BestFitMap;
    }
    
    BOOL GetThrowOnUnmappableChar()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThrowOnUnmappableChar;
    }
    
private:
    bool m_BestFitMap:1;
    bool m_ThrowOnUnmappableChar:1;
};


//=======================================================================
// Embedded LPWSTR <--> System.String
//=======================================================================
class FieldMarshaler_FixedStringUni : public FieldMarshaler
{
public:
    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(m_numchar * sizeof(WCHAR), sizeof(WCHAR))

    FieldMarshaler_FixedStringUni(UINT32 numChar)
    {
        WRAPPER_NO_CONTRACT;
        m_numchar = numChar;
    }
    
private:
    // # of characters for fixed strings
    UINT32           m_numchar;
};


//=======================================================================
// Embedded LPSTR <--> System.String
//=======================================================================
class FieldMarshaler_FixedStringAnsi : public FieldMarshaler
{
public:
    FieldMarshaler_FixedStringAnsi(UINT32 numChar, BOOL BestFitMap, BOOL ThrowOnUnmappableChar) :
        m_numchar(numChar), m_BestFitMap(!!BestFitMap), m_ThrowOnUnmappableChar(!!ThrowOnUnmappableChar)
    {
        WRAPPER_NO_CONTRACT;
    }

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(m_numchar * sizeof(CHAR), sizeof(CHAR))
    
    BOOL GetBestFit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_BestFitMap;
    }
    
    BOOL GetThrowOnUnmappableChar()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThrowOnUnmappableChar;
    }
    
private:
    // # of characters for fixed strings
    UINT32           m_numchar;
    bool             m_BestFitMap:1;
    bool             m_ThrowOnUnmappableChar:1;
};


//=======================================================================
// Embedded AnsiChar array <--> char[]
//=======================================================================
class FieldMarshaler_FixedCharArrayAnsi : public FieldMarshaler
{
public:
    FieldMarshaler_FixedCharArrayAnsi(UINT32 numElems, BOOL BestFit, BOOL ThrowOnUnmappableChar) :
        m_numElems(numElems), m_BestFitMap(!!BestFit), m_ThrowOnUnmappableChar(!!ThrowOnUnmappableChar)
    {
        WRAPPER_NO_CONTRACT;
    }

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(m_numElems * sizeof(CHAR), sizeof(CHAR))

    BOOL GetBestFit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_BestFitMap;
    }
    
    BOOL GetThrowOnUnmappableChar()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThrowOnUnmappableChar;
    }
    
private:
    // # of elements for fixedchararray
    UINT32           m_numElems;
    bool             m_BestFitMap:1;
    bool             m_ThrowOnUnmappableChar:1;
};


//=======================================================================
// Embedded arrays
//=======================================================================
class FieldMarshaler_FixedArray : public FieldMarshaler
{
public:
    FieldMarshaler_FixedArray(IMDInternalImport *pMDImport, mdTypeDef cl, UINT32 numElems, VARTYPE vt, MethodTable* pElementMT);

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;
    UINT32 AlignmentRequirementImpl() const;

    UINT32 NativeSizeImpl() const
    {
        LIMITED_METHOD_CONTRACT;

        MethodTable *pElementMT = m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle().GetMethodTable();
        return OleVariant::GetElementSizeForVarType(m_vt, pElementMT) * m_numElems;
    }

    TypeHandle GetElementTypeHandle() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;
        
        return m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle();
    }
    
    VARTYPE GetElementVT() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_vt;
    }

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
        
        image->FixupTypeHandlePointer(this, &m_arrayType);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

#ifdef FEATURE_PREJIT
        Module::RestoreTypeHandlePointer(&m_arrayType);
#else // FEATURE_PREJIT
        // without NGEN we only have to make sure that the type is fully loaded
        ClassLoader::EnsureLoaded(m_arrayType.GetValue());
#endif // FEATURE_PREJIT
        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
        return !m_arrayType.IsTagged() && (m_arrayType.IsNull() || m_arrayType.GetValue().IsRestored());
#else // FEATURE_PREJIT
        return m_arrayType.IsNull() || m_arrayType.GetValue().IsFullyLoaded();
#endif // FEATURE_PREJIT
    }
#endif
   
private:
    FixupPointer<TypeHandle> m_arrayType;
    UINT32           m_numElems;
    VARTYPE          m_vt;
    bool             m_BestFitMap:1; // Note: deliberately use small bools to save on working set - this is the largest FieldMarshaler and dominates the cost of the FieldMarshaler array
    bool             m_ThrowOnUnmappableChar:1; // Note: deliberately use small bools to save on working set - this is the largest FieldMarshaler and dominates the cost of the FieldMarshaler array
};


#ifdef FEATURE_CLASSIC_COMINTEROP
//=======================================================================
// SafeArrays
//=======================================================================
class FieldMarshaler_SafeArray : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPSAFEARRAY), sizeof(LPSAFEARRAY))

    FieldMarshaler_SafeArray(VARTYPE vt, MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;
        m_vt = vt;
        m_pMT.SetValue(pMT);
    }

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    { 
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pMT);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pMT);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return IsRestoredHelper(m_pMT);
    }
#endif

    TypeHandle GetElementTypeHandle() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return TypeHandle(m_pMT.GetValue());
    }

    VARTYPE GetElementVT() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_vt;
    }

private:
    FixupPointer<PTR_MethodTable> m_pMT;
    VARTYPE          m_vt;
};
#endif //FEATURE_CLASSIC_COMINTEROP


//=======================================================================
// Embedded function ptr <--> Delegate
//=======================================================================
class FieldMarshaler_Delegate : public FieldMarshaler
{
public:
    FieldMarshaler_Delegate(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;
        m_pNestedMethodTable.SetValue(pMT);
    }

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPVOID), sizeof(LPVOID))

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pNestedMethodTable);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pNestedMethodTable);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return IsRestoredHelper(m_pNestedMethodTable);
    }
#endif

    MethodTable *GetMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pNestedMethodTable.GetValue();
    }

    FixupPointer<PTR_MethodTable> m_pNestedMethodTable;
};


//=======================================================================
// Embedded SafeHandle <--> Handle. This field really only supports
// going from managed to unmanaged. In the other direction, we only
// check that the handle value has not changed.
//=======================================================================
class FieldMarshaler_SafeHandle : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPVOID), sizeof(LPVOID))
};


//=======================================================================
// Embedded CriticalHandle <--> Handle. This field really only supports
// going from managed to unmanaged. In the other direction, we only
// check that the handle value has not changed.
//=======================================================================
class FieldMarshaler_CriticalHandle : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;

    ELEMENT_SIZE_IMPL(sizeof(LPVOID), sizeof(LPVOID))
};

#ifdef FEATURE_COMINTEROP

//=======================================================================
// COM IP <--> Interface
//=======================================================================
class FieldMarshaler_Interface : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(IUnknown*), sizeof(IUnknown*))

    FieldMarshaler_Interface(MethodTable *pClassMT, MethodTable *pItfMT, DWORD dwFlags)
    {
        WRAPPER_NO_CONTRACT;
        m_pClassMT.SetValue(pClassMT);
        m_pItfMT.SetValue(pItfMT);
        m_dwFlags = dwFlags;
    }

#ifdef FEATURE_PREJIT
    void FixupImpl(DataImage *image)
    {
        STANDARD_VM_CONTRACT;
        
        image->FixupMethodTablePointer(this, &m_pClassMT);
        image->FixupMethodTablePointer(this, &m_pItfMT);

        FieldMarshaler::FixupImpl(image);
    }
#endif // FEATURE_PREJIT

    void RestoreImpl()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RestoreHelper(&m_pClassMT);
        RestoreHelper(&m_pItfMT);

        FieldMarshaler::RestoreImpl();
    }

#ifdef _DEBUG
    BOOL IsRestored() const
    {
        WRAPPER_NO_CONTRACT;

        return (IsRestoredHelper(m_pClassMT) && IsRestoredHelper(m_pItfMT));
    }
#endif

    void GetInterfaceInfo(MethodTable **ppItfMT, DWORD* pdwFlags) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(ppItfMT));
#ifdef FEATURE_PREJIT						
            PRECONDITION(IsRestored());
#endif
        }
        CONTRACTL_END;
        
        *ppItfMT    = m_pItfMT.GetValue();
        *pdwFlags   = m_dwFlags;
    }

    MethodTable *GetMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pClassMT.GetValue();
    }

    MethodTable *GetInterfaceMethodTable() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsRestored());
        }
        CONTRACTL_END;

        return m_pItfMT.GetValue();
    }

private:
    FixupPointer<PTR_MethodTable> m_pClassMT;
    FixupPointer<PTR_MethodTable> m_pItfMT;
    DWORD           m_dwFlags;
};

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
// This compile-time assert checks that the above FieldMarshaler is the biggest 
// (or equal-biggest) FieldMasharler we have,
// i.e. that we haven't set MAXFIELDMARSHALERSIZE to a value that is needlessly big.
// Corresponding asserts in FieldMarshaler.cpp ensure that we haven't set it to a value that is needlessly
// big, which would waste a whole lot of memory given the current storage scheme for FMs.
//
// If this assert first, it probably means you have successully reduced the size of the above FieldMarshaler.
// You should now place this assert on the FieldMarshaler that is the biggest, or modify MAXFIELDMARSHALERSIZE
// to match the new size.  
static_assert_no_msg(sizeof(FieldMarshaler_Interface) == MAXFIELDMARSHALERSIZE); 

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP

//=======================================================================
// VARIANT <--> Object
//=======================================================================
class FieldMarshaler_Variant : public FieldMarshaler
{
public:

    VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const;
    VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const;
    VOID DestroyNativeImpl(LPVOID pNativeValue) const;

    ELEMENT_SIZE_IMPL(sizeof(VARIANT), 8)
};

#endif // FEATURE_COMINTEROP


//=======================================================================
// Dummy marshaler
//=======================================================================
class FieldMarshaler_Illegal : public FieldMarshaler
{
public:
    FieldMarshaler_Illegal(UINT resIDWhy)
    {
        WRAPPER_NO_CONTRACT;
        m_resIDWhy = resIDWhy;
    }

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const;

    SCALAR_MARSHALER_IMPL(1, 1)
    
private:
    UINT m_resIDWhy;
};


#define FIELD_MARSHALER_COPY


class FieldMarshaler_Copy1 : public FieldMarshaler
{
public: 

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(1, 1)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        *((U1*)pNative) = *((U1*)pCLR);
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        *((U1*)pCLR) = *((U1*)pNative);
    }

};



class FieldMarshaler_Copy2 : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(2, 2)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pNative, 16, MAYBE_UNALIGNED_READ(pCLR, 16));
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pCLR, 16, MAYBE_UNALIGNED_READ(pNative, 16));
    }

};


class FieldMarshaler_Copy4 : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(4, 4)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pNative, 32, MAYBE_UNALIGNED_READ(pCLR, 32));
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pCLR, 32, MAYBE_UNALIGNED_READ(pNative, 32));
    }

};


class FieldMarshaler_Copy8 : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(8, 8)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pNative, 64, MAYBE_UNALIGNED_READ(pCLR, 64));
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));            
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        
        MAYBE_UNALIGNED_WRITE(pCLR, 64, MAYBE_UNALIGNED_READ(pNative, 64));
    }

};



class FieldMarshaler_Ansi : public FieldMarshaler
{
public:
    FieldMarshaler_Ansi(BOOL BestFitMap, BOOL ThrowOnUnmappableChar) :
        m_BestFitMap(!!BestFitMap), m_ThrowOnUnmappableChar(!!ThrowOnUnmappableChar)
    {
        WRAPPER_NO_CONTRACT;
    }

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const) 

    SCALAR_MARSHALER_IMPL(sizeof(CHAR), sizeof(CHAR))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR, NULL_OK));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        char c;
        InternalWideToAnsi((LPCWSTR)pCLR,
                           1,
                           &c,
                           1,
                           m_BestFitMap,
                           m_ThrowOnUnmappableChar);
        
        *((char*)pNative) = c;
    }

    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        MultiByteToWideChar(CP_ACP, 0, (char*)pNative, 1, (LPWSTR)pCLR, 1);
    }

    BOOL GetBestFit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_BestFitMap;
    }
    
    BOOL GetThrowOnUnmappableChar()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThrowOnUnmappableChar;
    }
    
private:
    bool             m_BestFitMap:1;
    bool             m_ThrowOnUnmappableChar:1;
};



class FieldMarshaler_WinBool : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(BOOL), sizeof(BOOL))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
        static_assert_no_msg(sizeof(BOOL) == sizeof(UINT32));
        MAYBE_UNALIGNED_WRITE(pNative, 32, ((*((U1 UNALIGNED*)pCLR)) ? 1 : 0));
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        static_assert_no_msg(sizeof(BOOL) == sizeof(UINT32));
        *((U1*)pCLR)  = MAYBE_UNALIGNED_READ(pNative, 32) ? 1 : 0;       
    }

};



#ifdef FEATURE_COMINTEROP

class FieldMarshaler_VariantBool : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(VARIANT_BOOL), sizeof(VARIANT_BOOL))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;

        static_assert_no_msg(sizeof(VARIANT_BOOL) == sizeof(BYTE) * 2);

        MAYBE_UNALIGNED_WRITE(pNative, 16, (*((U1*)pCLR)) ? VARIANT_TRUE : VARIANT_FALSE);
    }


    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;

        static_assert_no_msg(sizeof(VARIANT_BOOL) == sizeof(BYTE) * 2);

        *((U1*)pCLR) = MAYBE_UNALIGNED_READ(pNative, 16) ? 1 : 0;
    }

};

#endif // FEATURE_COMINTEROP



class FieldMarshaler_CBool : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(1, 1)

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;

        *((U1*)pNative) = (*((U1*)pCLR)) ? 1 : 0;
    }

    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        *((U1*)pCLR) = (*((U1*)pNative)) ? 1 : 0;
    }

};


class FieldMarshaler_Decimal : public FieldMarshaler
{
public:
    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(DECIMAL), 8);

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        memcpyNoGCRefs(pNative, pCLR, sizeof(DECIMAL));
    }

    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pCLR));
            PRECONDITION(CheckPointer(pNative));
        }
        CONTRACTL_END;
      
        memcpyNoGCRefs(pCLR, pNative, sizeof(DECIMAL));
    }

};

class FieldMarshaler_Date : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(DATE), sizeof(DATE))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const;

};



#ifdef FEATURE_COMINTEROP

class FieldMarshaler_Currency : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(CURRENCY), sizeof(CURRENCY))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const;

};

class FieldMarshaler_DateTimeOffset : public FieldMarshaler
{
public:

    UNUSED_METHOD_IMPL(VOID UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const)
    UNUSED_METHOD_IMPL(VOID UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const)

    SCALAR_MARSHALER_IMPL(sizeof(INT64), sizeof(INT64))

    VOID ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const;
    VOID ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const;

};

#endif // FEATURE_COMINTEROP


//========================================================================
// Used to ensure that native data is properly deleted in exception cases.
//========================================================================
class NativeLayoutDestroyer
{
public:
    NativeLayoutDestroyer(BYTE* pNativeData, MethodTable* pMT, UINT32 cbSize)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pNativeData));
            PRECONDITION(CheckPointer(pMT));
        }
        CONTRACTL_END;
        
        m_pNativeData = pNativeData;
        m_pMT = pMT;
        m_cbSize = cbSize;
        m_fDestroy = TRUE;
    }

    ~NativeLayoutDestroyer()
    {
        WRAPPER_NO_CONTRACT;

        if (m_fDestroy)
        {
            LayoutDestroyNative(m_pNativeData, m_pMT);
            FillMemory(m_pNativeData, m_cbSize, 0);
        }
    }

    void SuppressRelease()
    {
        m_fDestroy = FALSE;
    }
    
private:
    NativeLayoutDestroyer()
    {
        LIMITED_METHOD_CONTRACT;
    }

    BYTE*       m_pNativeData;
    MethodTable*    m_pMT;
    UINT32      m_cbSize;
    BOOL        m_fDestroy;
};

#endif // DACCESS_COMPILE


#endif // __FieldMarshaler_h__
