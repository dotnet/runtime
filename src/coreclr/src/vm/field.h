// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// COM+ Data Field Abstraction
// 


#ifndef _FIELD_H_
#define _FIELD_H_

#include "excep.h"

// Temporary values stored in FieldDesc m_dwOffset during loading
// The high 5 bits must be zero (because in field.h we steal them for other uses), so we must choose values > 0
#define FIELD_OFFSET_MAX              ((1<<27)-1)
#define FIELD_OFFSET_UNPLACED         FIELD_OFFSET_MAX
#define FIELD_OFFSET_UNPLACED_GC_PTR  (FIELD_OFFSET_MAX-1)
#define FIELD_OFFSET_VALUE_CLASS      (FIELD_OFFSET_MAX-2)
#define FIELD_OFFSET_NOT_REAL_FIELD   (FIELD_OFFSET_MAX-3)

// Offset to indicate an EnC added field. They don't have offsets as aren't placed in the object.
#define FIELD_OFFSET_NEW_ENC          (FIELD_OFFSET_MAX-4)
#define FIELD_OFFSET_BIG_RVA          (FIELD_OFFSET_MAX-5)
#define FIELD_OFFSET_LAST_REAL_OFFSET (FIELD_OFFSET_MAX-6)    // real fields have to be smaller than this


//
// This describes a field - one of this is allocated for every field, so don't make this structure any larger.
//
// @GENERICS: 
// Field descriptors for fields in instantiated types may be shared between compatible instantiations
// Hence for reflection it's necessary to pair a field desc with the exact owning type handle
class FieldDesc
{
    friend class MethodTableBuilder;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

  protected:
    RelativePointer<PTR_MethodTable> m_pMTOfEnclosingClass;  // This is used to hold the log2 of the field size temporarily during class loading.  Yuck.

    // See also: FieldDesc::InitializeFrom method

#if defined(DACCESS_COMPILE)
    union { //create a union so I can get the correct offset for ClrDump.
        unsigned m_dword1;
        struct {
#endif
        // Note that we may store other information in the high bits if available --
        // see enum_packedMBLayout and m_requiresFullMbValue for details.
        unsigned m_mb               : 24;

        // 8 bits...
        unsigned m_isStatic         : 1;
        unsigned m_isThreadLocal    : 1;
        unsigned m_isRVA            : 1;
        unsigned m_prot             : 3;
        // Does this field's mb require all 24 bits
        unsigned m_requiresFullMbValue : 1;
#if defined(DACCESS_COMPILE)
        };
    };
#endif

#if defined(DACCESS_COMPILE)
    union { //create a union so I can get the correct offset for ClrDump
        unsigned m_dword2;
        struct {
#endif
        // Note: this has been as low as 22 bits in the past & seemed to be OK.
        // we can steal some more bits here if we need them.
        unsigned m_dwOffset         : 27;
        unsigned m_type             : 5;
#if defined(DACCESS_COMPILE)
        };
    };
#endif

#ifdef _DEBUG
    LPUTF8 m_debugName;
#endif

public:
    // Allocated by special heap means, don't construct me
    FieldDesc() =delete;

#ifndef DACCESS_COMPILE
    void InitializeFrom(const FieldDesc& sourceField, MethodTable *pMT)
    {
        m_pMTOfEnclosingClass.SetValue(pMT);

        m_mb = sourceField.m_mb;
        m_isStatic = sourceField.m_isStatic;
        m_isThreadLocal = sourceField.m_isThreadLocal;
        m_isRVA = sourceField.m_isRVA;
        m_prot = sourceField.m_prot;
        m_requiresFullMbValue = sourceField.m_requiresFullMbValue;

        m_dwOffset = sourceField.m_dwOffset;
        m_type = sourceField.m_type;

#ifdef _DEBUG
        m_debugName = sourceField.m_debugName;
#endif // _DEBUG
    }
#endif // !DACCESS_COMPILE

#ifdef _DEBUG
    inline LPUTF8 GetDebugName()
    {
        LIMITED_METHOD_CONTRACT;
        return m_debugName;
    }
#endif // _DEBUG
    
#ifndef DACCESS_COMPILE
    // This should be called.  It was added so that Reflection
    // can create FieldDesc's for the static primitive fields that aren't
    // stored with the EEClass.
    void SetMethodTable(MethodTable* mt)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMTOfEnclosingClass.SetValue(mt);
    }
#endif

    VOID Init(mdFieldDef mb, 
              CorElementType FieldType, 
              DWORD dwMemberAttrs, 
              BOOL fIsStatic, 
              BOOL fIsRVA, 
              BOOL fIsThreadLocal, 
              LPCSTR pszFieldName);

    enum {
        enum_packedMbLayout_MbMask        = 0x01FFFF,
        enum_packedMbLayout_NameHashMask  = 0xFE0000
    };

    void SetMemberDef(mdFieldDef mb)
    {
        WRAPPER_NO_CONTRACT;

        // Check if we have to avoid using the packed mb layout
        if (RidFromToken(mb) > enum_packedMbLayout_MbMask)
        {
            m_requiresFullMbValue = 1;
        }

        // Set only the portion of m_mb we are using
        if (!m_requiresFullMbValue)
        {
            m_mb &= ~enum_packedMbLayout_MbMask;
            m_mb |= RidFromToken(mb);
        }
        else
        {
            m_mb = RidFromToken(mb);
        }
    }

    mdFieldDef GetMemberDef() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Check if this FieldDesc is using the packed mb layout
        if (!m_requiresFullMbValue)
        {
            return TokenFromRid(m_mb & enum_packedMbLayout_MbMask, mdtFieldDef);
        }
        
        return TokenFromRid(m_mb, mdtFieldDef);
    }

    CorElementType GetFieldType()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Set in code:FieldDesc.Init which in turn is called from
        // code:MethodTableBuilder.InitializeFieldDescs#InitCall which in turn calls
        // code:MethodTableBuilder.InitializeFieldDescs#FieldDescTypeMorph
        return (CorElementType) m_type;
    }

    DWORD GetFieldProtection()
    {
        LIMITED_METHOD_CONTRACT;

        // Set in code:FieldDesc.Init which in turn is called from code:MethodTableBuilder::InitializeFieldDescs#InitCall
        return m_prot;
    }

        // Please only use this in a path that you have already guarenteed
        // the assert is true
    DWORD GetOffsetUnsafe()
    {
        LIMITED_METHOD_CONTRACT;

        g_IBCLogger.LogFieldDescsAccess(this);
        _ASSERTE(m_dwOffset <= FIELD_OFFSET_LAST_REAL_OFFSET);
        return m_dwOffset;
    }

    DWORD GetOffset()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        g_IBCLogger.LogFieldDescsAccess(this);
        return GetOffset_NoLogging();
    }

    // During class load m_pMTOfEnclosingClass has the field size in it, so it has to use this version of
    // GetOffset during that time
    DWORD GetOffset_NoLogging()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Note FieldDescs are no longer on "hot" paths so the optimized code here
        // does not look necessary.

        if (m_dwOffset != FIELD_OFFSET_BIG_RVA) {
            // Assert that the big RVA case handling doesn't get out of sync
            // with the normal RVA case.
#ifdef _DEBUG
            // The OutOfLine_BigRVAOffset() can't be correctly evaluated during the time
            // that we repurposed m_pMTOfEnclosingClass for holding the field size
            // I don't see any good way to determine when this is so hurray for
            // heuristics!
            //
            // As of 4/11/2012 I could repro this by turning on the COMPLUS log and
            // the LOG() at line methodtablebuilder.cpp:7845 
            // MethodTableBuilder::PlaceRegularStaticFields() calls GetOffset_NoLogging()
            if((DWORD)(DWORD_PTR&)m_pMTOfEnclosingClass > 16)
            {
                _ASSERTE(!this->IsRVA() || (m_dwOffset == OutOfLine_BigRVAOffset()));
            }
#endif
            return m_dwOffset;
        }

        return OutOfLine_BigRVAOffset();
    }

    DWORD OutOfLine_BigRVAOffset()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DWORD   rva;

        // <NICE>I'm discarding a potential error here.  According to the code in MDInternalRO.cpp,
        // we won't get an error if we initially found the RVA.  So I'm going to just
        // assert it never happens.
        //
        // This is a small sin, but I don't see a good alternative. --cwb.</NICE>
        HRESULT hr;
        hr = GetMDImport()->GetFieldRVA(GetMemberDef(), &rva); 
        _ASSERTE(SUCCEEDED(hr));
        return rva;
    }

    HRESULT SetOffset(DWORD dwOffset)
    {
        LIMITED_METHOD_CONTRACT;

        //
        // value class fields must be aligned to pointer-sized boundaries
        //
        //
        // This is commented out because it isn't valid in all cases.
        // This is still here because it is useful for finding alignment
        // problems on IA64.
        //
        //_ASSERTE((dwOffset > FIELD_OFFSET_LAST_REAL_OFFSET)  ||
        //         (ELEMENT_TYPE_VALUETYPE != GetFieldType()) ||
        //         (IS_ALIGNED(dwOffset, sizeof(void*))));

        m_dwOffset = dwOffset;
        return((dwOffset > FIELD_OFFSET_LAST_REAL_OFFSET) ? COR_E_TYPELOAD : S_OK);
    }

    // Okay, we've stolen too many bits from FieldDescs.  In the RVA case, there's no
    // reason to believe they will be limited to 22 bits.  So use a sentinel for the
    // huge cases, and recover them from metadata on-demand.
    void SetOffsetRVA(DWORD dwOffset)
    {
        LIMITED_METHOD_CONTRACT;

        m_dwOffset = (dwOffset > FIELD_OFFSET_LAST_REAL_OFFSET)
                      ? FIELD_OFFSET_BIG_RVA
                      : dwOffset;
    }

    DWORD   IsStatic() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_isStatic;
    }

    BOOL   IsSpecialStatic()
    {
        LIMITED_METHOD_CONTRACT;

        return m_isStatic && (m_isRVA || m_isThreadLocal
            );
    }

    BOOL   IsRVA() const               // Has an explicit RVA associated with it
    { 
        LIMITED_METHOD_DAC_CONTRACT;

        return m_isRVA;
    }

    BOOL   IsThreadStatic() const      // Static relative to a thread
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_isThreadLocal;
    }

    // Indicate that this field was added by EnC
    // Must only be called on instances of EnCFieldDesc
    void SetEnCNew() 
    {
        WRAPPER_NO_CONTRACT;

        // EnC added fields don't live in the actual object, so don't have a real offset
        SetOffset(FIELD_OFFSET_NEW_ENC);
    }

    // Was this field added by EnC?
    // If this is true, then this object is an instance of EnCFieldDesc
    BOOL IsEnCNew() 
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // EnC added fields don't have a real offset
        return m_dwOffset == FIELD_OFFSET_NEW_ENC;
    }

    BOOL IsByValue()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetFieldType() == ELEMENT_TYPE_VALUETYPE;
    }

    BOOL IsPrimitive()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (CorIsPrimitiveType(GetFieldType()) != FALSE);
    }

    BOOL IsObjRef();

#ifdef FEATURE_PREJIT
    void SaveContents(DataImage *image);
    void Fixup(DataImage *image);
#endif // FEATURE_PREJIT

    UINT LoadSize();

    // Return -1 if the type isn't loaded yet (i.e. if LookupFieldTypeHandle() would return null)
    UINT GetSize();

    // These routines encapsulate the operation of getting and setting
    // fields.
    void    GetInstanceField(OBJECTREF o, VOID * pOutVal);
    void    SetInstanceField(OBJECTREF o, const VOID * pInVal);

    void*   GetInstanceAddress(OBJECTREF o);

        // Get the address of a field within object 'o'
    PTR_VOID   GetAddress(PTR_VOID o);

    PTR_VOID GetAddressNoThrowNoGC(PTR_VOID o);
    void*   GetAddressGuaranteedInHeap(void *o);

    void*   GetValuePtr(OBJECTREF o);
    VOID    SetValuePtr(OBJECTREF o, void* pValue);
    DWORD   GetValue32(OBJECTREF o);
    VOID    SetValue32(OBJECTREF o, DWORD dwValue);
    OBJECTREF GetRefValue(OBJECTREF o);
    VOID    SetRefValue(OBJECTREF o, OBJECTREF orValue);
    USHORT  GetValue16(OBJECTREF o);
    VOID    SetValue16(OBJECTREF o, DWORD dwValue);  
    BYTE    GetValue8(OBJECTREF o);               
    VOID    SetValue8(OBJECTREF o, DWORD dwValue);  
    __int64 GetValue64(OBJECTREF o);               
    VOID    SetValue64(OBJECTREF o, __int64 value);  

    PTR_MethodTable GetApproxEnclosingMethodTable_NoLogging()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pMTOfEnclosingClass.GetValue(PTR_HOST_MEMBER_TADDR(FieldDesc, this, m_pMTOfEnclosingClass));
    }

    PTR_MethodTable GetApproxEnclosingMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        g_IBCLogger.LogFieldDescsAccess(this);
        return GetApproxEnclosingMethodTable_NoLogging();
    }

    PTR_MethodTable GetEnclosingMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(!IsSharedByGenericInstantiations());
        return GetApproxEnclosingMethodTable();
    }

    // FieldDesc can be shared between generic instantiations. So List<String>._items
    // is really the  same as List<__Canon>._items. Hence, the FieldDesc itself
    // cannot know the exact enclosing type. You need to provide the exact owner
    // like List<String> or a subtype like MyInheritedList<String>.
    MethodTable * GetExactDeclaringType(MethodTable * ownerOrSubType);

    BOOL IsSharedByGenericInstantiations()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (!IsStatic()) && GetApproxEnclosingMethodTable()->IsSharedByGenericInstantiations();
    }

    BOOL IsFieldOfValueType()
    {
        WRAPPER_NO_CONTRACT;
        return GetApproxEnclosingMethodTable()->IsValueType();
    }

    DWORD GetNumGenericClassArgs()
    {
        WRAPPER_NO_CONTRACT;
        return GetApproxEnclosingMethodTable()->GetNumGenericArgs();
    }

   PTR_BYTE GetBaseInDomainLocalModule(DomainLocalModule * pLocalModule)
    {
        WRAPPER_NO_CONTRACT;

        if (GetFieldType() == ELEMENT_TYPE_CLASS || GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            return pLocalModule->GetGCStaticsBasePointer(GetEnclosingMethodTable());
        }
        else
        {
            return pLocalModule->GetNonGCStaticsBasePointer(GetEnclosingMethodTable());
        }
    }

    PTR_BYTE GetBase()
    {
        CONTRACTL
        {
          NOTHROW;
          GC_NOTRIGGER;
        }
        CONTRACTL_END

        MethodTable *pMT = GetEnclosingMethodTable();

        return GetBaseInDomainLocalModule(pMT->GetDomainLocalModule());
    }

    // returns the address of the field
    void* GetStaticAddress(void *base);

    // In all cases except Value classes, the AddressHandle is
    // simply the address of the static.  For the case of value
    // types, however, it is the address of OBJECTREF that holds
    // the boxed value used to hold the value type.  This is needed
    // because the OBJECTREF moves, and the JIT needs to embed something
    // in the code that does not move.  Thus the jit has to 
    // dereference and unbox before the access.  
    PTR_VOID GetStaticAddressHandle(PTR_VOID base);

#ifndef DACCESS_COMPILE
    OBJECTREF GetStaticOBJECTREF()
    {
        WRAPPER_NO_CONTRACT;
        return *(OBJECTREF *)GetCurrentStaticAddress();
    }

    VOID SetStaticOBJECTREF(OBJECTREF objRef)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END

        GCPROTECT_BEGIN(objRef);
        OBJECTREF *pObjRef = (OBJECTREF *)GetCurrentStaticAddress();
        SetObjectReference(pObjRef, objRef);
        GCPROTECT_END();
    }

    void*   GetStaticValuePtr()
    {
        WRAPPER_NO_CONTRACT;
        return *(void**)GetCurrentStaticAddress();
    }

    VOID    SetStaticValuePtr(void *value)
    {
        WRAPPER_NO_CONTRACT;
        *(void**)GetCurrentStaticAddress() = value;
    }

    DWORD   GetStaticValue32()
    {
        WRAPPER_NO_CONTRACT;
        return *(DWORD*)GetCurrentStaticAddress(); 
    }

    VOID    SetStaticValue32(DWORD dwValue)  
    { 
        WRAPPER_NO_CONTRACT;
        *(DWORD*)GetCurrentStaticAddress() = dwValue; 
    }

    USHORT  GetStaticValue16()
    {
        WRAPPER_NO_CONTRACT;
        return *(USHORT*)GetCurrentStaticAddress(); 
    }

    VOID    SetStaticValue16(DWORD dwValue)
    { 
        WRAPPER_NO_CONTRACT;
        *(USHORT*)GetCurrentStaticAddress() = (USHORT)dwValue; 
    }

    BYTE    GetStaticValue8()
    {
        WRAPPER_NO_CONTRACT;
        return *(BYTE*)GetCurrentStaticAddress(); 
    }

    VOID    SetStaticValue8(DWORD dwValue)
    {
        WRAPPER_NO_CONTRACT;
        *(BYTE*)GetCurrentStaticAddress() = (BYTE)dwValue; 
    }

    __int64 GetStaticValue64()
    { 
        WRAPPER_NO_CONTRACT;
        return *(__int64*)GetCurrentStaticAddress();
    }

    VOID    SetStaticValue64(__int64 qwValue)
    {
        WRAPPER_NO_CONTRACT;
        *(__int64*)GetCurrentStaticAddress() = qwValue;
    }

    void* GetCurrentStaticAddress()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END

        _ASSERTE(IsStatic());

        if (IsThreadStatic()) 
        {
            return Thread::GetStaticFieldAddress(this);
        }
        else {
            PTR_BYTE base = 0;
            if (!IsRVA()) // for RVA the base is ignored
                base = GetBase();
            return GetStaticAddress((void *)dac_cast<TADDR>(base)); 
        }
    }

    VOID    CheckRunClassInitThrowing()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END;

        GetEnclosingMethodTable()->CheckRunClassInitThrowing();
    }
#endif //DACCESS_COMPILE

    Module *GetModule()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetApproxEnclosingMethodTable()->GetModule();
    }

    BOOL IsZapped()
    {
        WRAPPER_NO_CONTRACT;

        // Field Desc's are currently always saved into the same module as their 
        // corresponding method table.
        return GetApproxEnclosingMethodTable()->IsZapped();
    }

    Module *GetLoaderModule()
    {
        WRAPPER_NO_CONTRACT;

        // Field Desc's are currently always saved into the same module as their 
        // corresponding method table.
        return GetApproxEnclosingMethodTable()->GetLoaderModule();
    }

    void GetSig(PCCOR_SIGNATURE *ppSig, DWORD *pcSig)
    {
        CONTRACTL
        {
          NOTHROW;
          GC_NOTRIGGER;
          MODE_ANY;
        }
        CONTRACTL_END

        if (FAILED(GetMDImport()->GetSigOfFieldDef(GetMemberDef(), pcSig, ppSig)))
        {   // Class loader already asked for signature, so this should always succeed (unless there's a 
            // bug or a new code path)
            _ASSERTE(!"If this ever fires, then this method should return HRESULT");
            *ppSig = NULL;
            *pcSig = 0;
        }
    }

    SigPointer GetSigPointer()
    {
        WRAPPER_NO_CONTRACT;

        PCCOR_SIGNATURE pSig;
        DWORD           cSig;

        GetSig(&pSig, &cSig);

        return SigPointer(pSig, cSig);
    }

    // This is slow (uses MetaData), don't use it!
    LPCUTF8 GetName()
    {
        CONTRACTL
        {
          THROWS;
          GC_NOTRIGGER;
          MODE_ANY;
        }
        CONTRACTL_END
        
        LPCSTR szName;
        IfFailThrow(GetMDImport()->GetNameOfFieldDef(GetMemberDef(), &szName));
        _ASSERTE(szName != NULL);
        return szName;
    }
    // This is slow (uses MetaData), don't use it!
    __checkReturn 
    HRESULT GetName_NoThrow(LPCUTF8 *pszName)
    {
        CONTRACTL
        {
          NOTHROW;
          GC_NOTRIGGER;
          MODE_ANY;
        }
        CONTRACTL_END
        
        return GetMDImport()->GetNameOfFieldDef(GetMemberDef(), pszName);
    }
    
    void PrecomputeNameHash();
    BOOL MightHaveName(ULONG nameHashValue);

    // <TODO>@TODO: </TODO>This is slow, don't use it!
    DWORD   GetAttributes()
    {
        CONTRACTL
        {
          NOTHROW;
          GC_NOTRIGGER;
          MODE_ANY;
        }
        CONTRACTL_END

        DWORD dwAttributes;
        if (FAILED(GetMDImport()->GetFieldDefProps(GetMemberDef(), &dwAttributes)))
        {   // Class loader already asked for attributes, so this should always succeed (unless there's a 
            // bug or a new code path)
            _ASSERTE(!"If this ever fires, then this method should return HRESULT");
            return 0;
        }
        return dwAttributes;
    }
    
    // Mini-Helpers
    DWORD   IsPublic()
    {
        WRAPPER_NO_CONTRACT;

        return IsFdPublic(GetFieldProtection());
    }

    DWORD   IsProtected()
    {
        WRAPPER_NO_CONTRACT;
        return IsFdFamily(GetFieldProtection());
    }

    DWORD   IsPrivate()
    {
        WRAPPER_NO_CONTRACT;

        return IsFdPrivate(GetFieldProtection());
    }

    IMDInternalImport *GetMDImport()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetModule()->GetMDImport();
    }

#ifndef DACCESS_COMPILE
    IMetaDataImport *GetRWImporter()
    {
        WRAPPER_NO_CONTRACT;

        return GetModule()->GetRWImporter();
    }
#endif // DACCESS_COMPILE

    TypeHandle LookupFieldTypeHandle(ClassLoadLevel level = CLASS_LOADED, BOOL dropGenericArgumentLevel = FALSE);

    TypeHandle LookupApproxFieldTypeHandle()
    {
        WRAPPER_NO_CONTRACT;
        return LookupFieldTypeHandle(CLASS_LOAD_APPROXPARENTS, TRUE);
    }

    // Instance FieldDesc can be shared between generic instantiations. So List<String>._items
    // is really the  same as List<__Canon>._items. Hence, the FieldDesc itself
    // cannot know the exact field type. This function returns the approximate field type.
    // For eg. this will return "__Canon[]" for List<String>._items.
    TypeHandle GetFieldTypeHandleThrowing(ClassLoadLevel level = CLASS_LOADED, BOOL dropGenericArgumentLevel = FALSE);

    TypeHandle GetApproxFieldTypeHandleThrowing()
    {
        WRAPPER_NO_CONTRACT;
        return GetFieldTypeHandleThrowing(CLASS_LOAD_APPROXPARENTS, TRUE);
    }

    // Given a type handle of an object and a method that comes from some 
    // superclass of the class of that object, find the instantiation of 
    // that superclass, i.e. the class instantiation which will be relevant
    // to interpreting the signature of the method.  The type handle of
    // the object does not need to be given in all circumstances, in 
    // particular it is only needed for FieldDescs pFD that
    // return true for pFD->GetApproxEnclosingMethodTable()->IsSharedByGenericInstantiations().
    // In other cases it is allowed to be null and will be ignored.
    // 
    // Will return NULL if the field is not in a generic class.
    Instantiation GetExactClassInstantiation(TypeHandle possibleObjType);

    // Instance FieldDesc can be shared between generic instantiations. So List<String>._items
    // is really the  same as List<__Canon>._items. Hence, the FieldDesc itself
    // cannot know the exact field type. You need to specify the owner
    // like List<String> in order to get the exact type which would be "String[]"
    TypeHandle GetExactFieldType(TypeHandle owner);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        SUPPORTS_DAC;
        DAC_ENUM_DTHIS();
    }
#endif

#ifndef DACCESS_COMPILE
    REFLECTFIELDREF GetStubFieldInfo();
#endif
};

#endif // _FIELD_H_

