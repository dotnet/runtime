// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: typedesc.h
//


//

//
// ============================================================================


#ifndef TYPEDESC_H
#define TYPEDESC_H
#include <specstrings.h>

class TypeHandleList;

/*************************************************************************/
/* TypeDesc is a discriminated union of all types that can not be directly
   represented by a simple MethodTable*.   The discrimintor of the union at the present
   time is the CorElementType numeration.  The subclass of TypeDesc are
   the possible variants of the union.  


   ParamTypeDescs only include byref, array and pointer types.  They do NOT
   include instantaitions of generic types, which are represented by MethodTables.
*/ 


typedef DPTR(class TypeDesc) PTR_TypeDesc;

class TypeDesc 
{
public:
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
#ifndef DACCESS_COMPILE
    TypeDesc(CorElementType type) { 
        LIMITED_METHOD_CONTRACT;

        m_typeAndFlags = type;
    }
#endif

    // This is the ELEMENT_TYPE* that would be used in the type sig for this type
    // For enums this is the uderlying type
    inline CorElementType GetInternalCorElementType() { 
        LIMITED_METHOD_DAC_CONTRACT;

        return (CorElementType) (m_typeAndFlags & 0xff);
    }

    // Get the exact parent (superclass) of this type  
    TypeHandle GetParent();

    // Returns the name of the array.  Note that it returns
    // the length of the returned string 
    static void ConstructName(CorElementType kind,
                              TypeHandle param,
                              int rank,
                              SString &ssBuff);

    void GetName(SString &ssBuf);

    //-------------------------------------------------------------------
    // CASTING
    // 
    // There are two variants of the "CanCastTo" method:
    //
    // CanCastTo
    // - restore encoded pointers on demand
    // - might throw, might trigger GC
    // - return type is boolean (FALSE = cannot cast, TRUE = can cast)
    //
    // CanCastToNoGC
    // - do not restore encoded pointers on demand
    // - does not throw, does not trigger GC
    // - return type is three-valued (CanCast, CannotCast, MaybeCast)
    // - MaybeCast indicates that the test tripped on an encoded pointer
    //   so the caller should now call CanCastTo if it cares
    // 

    BOOL CanCastTo(TypeHandle type, TypeHandlePairList *pVisited);
    TypeHandle::CastResult CanCastToNoGC(TypeHandle type);

    static BOOL CanCastParam(TypeHandle fromParam, TypeHandle toParam, TypeHandlePairList *pVisited);
    static TypeHandle::CastResult CanCastParamNoGC(TypeHandle fromParam, TypeHandle toParam);

#ifndef DACCESS_COMPILE
    BOOL IsEquivalentTo(TypeHandle type COMMA_INDEBUG(TypeHandlePairList *pVisited));
#endif

    // BYREF
    BOOL IsByRef() {              // BYREFS are often treated specially 
        WRAPPER_NO_CONTRACT;

        return(GetInternalCorElementType() == ELEMENT_TYPE_BYREF);
    }

    // PTR
    BOOL IsPointer() {
        WRAPPER_NO_CONTRACT;

        return(GetInternalCorElementType() == ELEMENT_TYPE_PTR);
    }

    // ARRAY, SZARRAY
    BOOL IsArray();

    // VAR, MVAR
    BOOL IsGenericVariable();

    // ELEMENT_TYPE_FNPTR
    BOOL IsFnPtr();

    // VALUETYPE
    BOOL IsNativeValueType();

    // Is actually ParamTypeDesc (ARRAY, SZARRAY, BYREF, PTR)
    BOOL HasTypeParam();

#ifdef FEATURE_PREJIT
    void Save(DataImage *image);
    void Fixup(DataImage *image);

    BOOL NeedsRestore(DataImage *image)
    {
        WRAPPER_NO_CONTRACT;
        return ComputeNeedsRestore(image, NULL);
    }

    BOOL ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited);
#endif

    void DoRestoreTypeKey();
    void Restore();
    BOOL IsRestored();
    BOOL IsRestored_NoLogging();
    void SetIsRestored();

    inline BOOL HasUnrestoredTypeKey() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return (m_typeAndFlags & TypeDesc::enum_flag_UnrestoredTypeKey) != 0;       
    }

    BOOL HasTypeEquivalence() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_typeAndFlags & TypeDesc::enum_flag_HasTypeEquivalence) != 0;       
    }

    BOOL IsFullyLoaded() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_typeAndFlags & TypeDesc::enum_flag_IsNotFullyLoaded) == 0;       
    }

    VOID SetIsFullyLoaded()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd(&m_typeAndFlags, ~TypeDesc::enum_flag_IsNotFullyLoaded);
    }

    ClassLoadLevel GetLoadLevel();

    void DoFullyLoad(Generics::RecursionGraph *pVisited, ClassLoadLevel level,
                     DFLPendingList *pPending, BOOL *pfBailed, const InstantiationContext *pInstContext);

    // The module that defined the underlying type
    PTR_Module GetModule();

    // The ngen'ed module where this type-desc lives
    PTR_Module GetZapModule();

    // The module where this type lives for the purposes of loading and prejitting
    // See ComputeLoaderModule for more information
    PTR_Module GetLoaderModule();
    
    // The assembly that defined this type (== GetModule()->GetAssembly())
    Assembly* GetAssembly();

    PTR_MethodTable  GetMethodTable();               // only meaningful for ParamTypeDesc
    TypeHandle GetTypeParam();                       // only meaningful for ParamTypeDesc
    Instantiation GetClassOrArrayInstantiation();    // only meaningful for ParamTypeDesc; see above

    TypeHandle GetBaseTypeParam();                   // only allowed for ParamTypeDesc, helper method used to avoid recursion

    // Note that if the TypeDesc, e.g. a function pointer type, involves parts that may
    // come from either a SharedDomain or an AppDomain then special rules apply to GetDomain.
    // It returns the SharedDomain if all the
    // constituent parts of the type are SharedDomain (i.e. domain-neutral), 
    // and returns an AppDomain if any of the parts are from an AppDomain, 
    // i.e. are domain-bound.  If any of the parts are domain-bound
    // then they will all belong to the same domain.
    PTR_BaseDomain GetDomain();
    BOOL IsDomainNeutral();

    PTR_LoaderAllocator GetLoaderAllocator()
    {
        SUPPORTS_DAC;

        return GetLoaderModule()->GetLoaderAllocator();
    }

 protected:
    // See methodtable.h for details of the flags with the same name there
    enum
    {
        enum_flag_NeedsRestore           = 0x00000100, // Only used during ngen
        enum_flag_PreRestored            = 0x00000200, // Only used during ngen
        enum_flag_Unrestored             = 0x00000400, 
        enum_flag_UnrestoredTypeKey      = 0x00000800,
        enum_flag_IsNotFullyLoaded       = 0x00001000,
        enum_flag_DependenciesLoaded     = 0x00002000,
        enum_flag_HasTypeEquivalence     = 0x00004000
    };
    //
    // Low-order 8 bits of this flag are used to store the CorElementType, which
    // discriminates what kind of TypeDesc we are
    //
    // The remaining bits are available for flags
    //
    DWORD m_typeAndFlags;
};


/*************************************************************************/
// This variant is used for parameterized types that have exactly one argument
// type.  This includes arrays, byrefs, pointers.  

typedef DPTR(class ParamTypeDesc) PTR_ParamTypeDesc;


class ParamTypeDesc : public TypeDesc {
    friend class TypeDesc;
    friend class JIT_TrialAlloc;
    friend class CheckAsmOffsets;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

public:
#ifndef DACCESS_COMPILE
    ParamTypeDesc(CorElementType type, MethodTable* pMT, TypeHandle arg) 
        : TypeDesc(type), m_Arg(arg), m_hExposedClassObject(0) {

        LIMITED_METHOD_CONTRACT;

        m_TemplateMT.SetValue(pMT);

        // ParamTypeDescs start out life not fully loaded
        m_typeAndFlags |= TypeDesc::enum_flag_IsNotFullyLoaded;

        // Param type descs can only be equivalent if their constituent bits are equivalent.
        if (arg.HasTypeEquivalence())
        {
            m_typeAndFlags |= TypeDesc::enum_flag_HasTypeEquivalence;
        }

        INDEBUGIMPL(Verify());
    }
#endif 

    INDEBUGIMPL(BOOL Verify();)

    OBJECTREF GetManagedClassObject();

    OBJECTREF GetManagedClassObjectIfExists()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        OBJECTREF objRet = NULL;
        GET_LOADERHANDLE_VALUE_FAST(GetLoaderAllocator(), m_hExposedClassObject, &objRet);
        return objRet;
    }
    OBJECTREF GetManagedClassObjectFast()
    {
        LIMITED_METHOD_CONTRACT;

        OBJECTREF objRet = NULL;
        LoaderAllocator::GetHandleValueFast(m_hExposedClassObject, &objRet);
        return objRet;
    }

    TypeHandle GetModifiedType()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Arg;
    }

    TypeHandle GetTypeParam();

#ifdef FEATURE_PREJIT
    void Save(DataImage *image);
    void Fixup(DataImage *image);
    BOOL ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited);
#endif

    BOOL OwnsTemplateMethodTable();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
    
    friend class StubLinkerCPU;

#ifdef FEATURE_ARRAYSTUB_AS_IL
    friend class ArrayOpLinker;
#endif
protected:
    // the m_typeAndFlags field in TypeDesc tell what kind of parameterized type we have
    FixupPointer<PTR_MethodTable> m_TemplateMT; // The shared method table, some variants do not use this field (it is null)
    TypeHandle      m_Arg;              // The type that is being modified
    LOADERHANDLE    m_hExposedClassObject;  // handle back to the internal reflection Type object
};


/*************************************************************************/
/* An ArrayTypeDesc represents a Array of some pointer type. */

class ArrayTypeDesc : public ParamTypeDesc
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
public:
#ifndef DACCESS_COMPILE
    ArrayTypeDesc(MethodTable* arrayMT, TypeHandle elementType) :
        ParamTypeDesc(arrayMT->IsMultiDimArray() ? ELEMENT_TYPE_ARRAY : ELEMENT_TYPE_SZARRAY, arrayMT, elementType)
#ifdef FEATURE_COMINTEROP
      , m_pCCWTemplate(NULL)
#endif // FEATURE_COMINTEROP
    {
        STATIC_CONTRACT_SO_TOLERANT;
        WRAPPER_NO_CONTRACT;
        INDEBUG(Verify());
    }

//private:    TypeHandle      m_Arg;              // The type that is being modified


    // placement new operator
    void* operator new(size_t size, void* spot) {   return (spot); }

#endif

    TypeHandle GetArrayElementTypeHandle() {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return GetTypeParam();
    }

    unsigned GetRank() {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY)
            return 1;
        else
            return dac_cast<PTR_ArrayClass>(GetMethodTable()->GetClass())->GetRank();
    }

    MethodTable* GetParent()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(!m_TemplateMT.IsNull());
        _ASSERTE(m_TemplateMT.GetValue()->IsArray());
        _ASSERTE(m_TemplateMT.GetValue()->ParentEquals(g_pArrayClass));

        return g_pArrayClass;
    }

#ifdef FEATURE_COMINTEROP
    ComCallWrapperTemplate *GetComCallWrapperTemplate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCCWTemplate;
    }

    BOOL SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        TypeHandle th(this);
        g_IBCLogger.LogTypeMethodTableWriteableAccess(&th);

        return (InterlockedCompareExchangeT(EnsureWritablePages(&m_pCCWTemplate), pTemplate, NULL) == NULL);
    }
#endif // FEATURE_COMINTEROP

    INDEBUG(BOOL Verify();)

#ifdef FEATURE_PREJIT
    void Fixup(DataImage *image);
#endif

    MethodTable * GetTemplateMethodTable() {
        WRAPPER_NO_CONTRACT;
        MethodTable * pTemplateMT = m_TemplateMT.GetValue();
        _ASSERTE(pTemplateMT->IsArray());
        return pTemplateMT;
    }

    TADDR GetTemplateMethodTableMaybeTagged() {
        WRAPPER_NO_CONTRACT;
        return m_TemplateMT.GetValueMaybeTagged();
    }

#ifdef FEATURE_COMINTEROP
    ComCallWrapperTemplate *m_pCCWTemplate;
#endif // FEATURE_COMINTEROP
};

/*************************************************************************/
// These are for verification of generic code and reflection over generic code.
// Each TypeVarTypeDesc represents a class or method type variable, as specified by a GenericParam entry.
// The type variables are tied back to the class or method that *defines* them.
// This is done through typedef or methoddef tokens.

class TypeVarTypeDesc : public TypeDesc 
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
public:

#ifndef DACCESS_COMPILE

    TypeVarTypeDesc(PTR_Module pModule, mdToken typeOrMethodDef, unsigned int index, mdGenericParam token) :
        TypeDesc(TypeFromToken(typeOrMethodDef) == mdtTypeDef ? ELEMENT_TYPE_VAR : ELEMENT_TYPE_MVAR)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(pModule));
            PRECONDITION(TypeFromToken(typeOrMethodDef) == mdtTypeDef || TypeFromToken(typeOrMethodDef) == mdtMethodDef);
            PRECONDITION(index >= 0);
            PRECONDITION(TypeFromToken(token) == mdtGenericParam);
        }
        CONTRACTL_END;

        m_pModule = pModule;
        m_typeOrMethodDef = typeOrMethodDef;
        m_token = token;
        m_index = index;
        m_hExposedClassObject = 0;
        m_constraints = NULL;
        m_numConstraints = (DWORD)-1;
    }
#endif // #ifndef DACCESS_COMPILE

    // placement new operator
    void* operator new(size_t size, void* spot) { LIMITED_METHOD_CONTRACT;  return (spot); }

    PTR_Module GetModule()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pModule;
    }

    unsigned int GetIndex() 
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_index; 
    }

    mdGenericParam GetToken() 
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_token; 
    }

    mdToken GetTypeOrMethodDef() 
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_typeOrMethodDef; 
    }

    OBJECTREF GetManagedClassObject();
    OBJECTREF GetManagedClassObjectIfExists()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        OBJECTREF objRet = NULL;
        GET_LOADERHANDLE_VALUE_FAST(GetLoaderAllocator(), m_hExposedClassObject, &objRet);
        return objRet;
    }
    OBJECTREF GetManagedClassObjectFast()
    {
        LIMITED_METHOD_CONTRACT;

        OBJECTREF objRet = NULL;
        LoaderAllocator::GetHandleValueFast(m_hExposedClassObject, &objRet);
        return objRet;
    }

    // Load the owning type. Note that the result is not guaranteed to be full loaded
    MethodDesc * LoadOwnerMethod();
    TypeHandle LoadOwnerType();
    
    BOOL ConstraintsLoaded() { LIMITED_METHOD_CONTRACT; return m_numConstraints != (DWORD)-1; }

    // Return NULL if no constraints are specified 
    // Return an array of type handles if constraints are specified,
    // with the number of constraints returned in pNumConstraints
    TypeHandle* GetCachedConstraints(DWORD *pNumConstraints);
    TypeHandle* GetConstraints(DWORD *pNumConstraints, ClassLoadLevel level = CLASS_LOADED);

    // Load the constraints if not already loaded
    void LoadConstraints(ClassLoadLevel level = CLASS_LOADED);

    // Check the constraints on this type parameter hold in the supplied context for the supplied type
    BOOL SatisfiesConstraints(SigTypeContext *pTypeContext, TypeHandle thArg,
                              const InstantiationContext *pInstContext = NULL);

    // Check whether the constraints on this type force it to be a reference type (i.e. it is impossible
    // to instantiate it with a value type).
    BOOL ConstrainedAsObjRef();

    // Check whether the constraints on this type force it to be a value type (i.e. it is impossible to
    // instantiate it with a reference type).
    BOOL ConstrainedAsValueType();

#ifdef FEATURE_PREJIT
    void Save(DataImage *image);
    void Fixup(DataImage *image);
#endif // FEATURE_PREJIT
    
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
    
protected:
    BOOL ConstrainedAsObjRefHelper();

    // Module containing the generic definition, also the loader module for this type desc
    PTR_Module m_pModule;

    // Declaring type or method
    mdToken m_typeOrMethodDef;

    // Constraints, determined on first call to GetConstraints
    Volatile<DWORD> m_numConstraints;    // -1 until number has been determined
    PTR_TypeHandle m_constraints;

    // slot index back to the internal reflection Type object
    LOADERHANDLE m_hExposedClassObject;    

    // token for GenericParam entry
    mdGenericParam    m_token; 

    // index within declaring type or method, numbered from zero
    unsigned int m_index;
};

/*************************************************************************/
/* represents a function type.  */

typedef SPTR(class FnPtrTypeDesc) PTR_FnPtrTypeDesc;

class FnPtrTypeDesc : public TypeDesc
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

public:
#ifndef DACCESS_COMPILE
    FnPtrTypeDesc(BYTE callConv, DWORD numArgs, TypeHandle * retAndArgTypes) 
        : TypeDesc(ELEMENT_TYPE_FNPTR), m_NumArgs(numArgs), m_CallConv(callConv)
    {
        LIMITED_METHOD_CONTRACT;
        for (DWORD i = 0; i <= numArgs; i++)
        {
            m_RetAndArgTypes[i] = retAndArgTypes[i];
        }
    }
#endif //!DACCESS_COMPILE

    DWORD GetNumArgs() 
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_NumArgs;
    }

    BYTE GetCallConv() 
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(FitsIn<BYTE>(m_CallConv));
        return static_cast<BYTE>(m_CallConv);
    }

    // Return a pointer to the types of the signature, return type followed by argument types
    // The type handles are guaranteed to be fixed up
    TypeHandle * GetRetAndArgTypes();
    // As above, but const version
    const TypeHandle * GetRetAndArgTypes() const
    {
        WRAPPER_NO_CONTRACT;
        return const_cast<FnPtrTypeDesc *>(this)->GetRetAndArgTypes();
    }

    // As above, but the type handles might be zap-encodings that need fixing up explicitly
    PTR_TypeHandle GetRetAndArgTypesPointer()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return PTR_TypeHandle(m_RetAndArgTypes);
    }

#ifndef DACCESS_COMPILE
    
    // Returns TRUE if all return and argument types are externally visible.
    BOOL IsExternallyVisible() const;
    // Returns TRUE if any of return or argument types is part of an assembly loaded for introspection.
    BOOL IsIntrospectionOnly() const;
    // Returns TRUE if any of return or argument types is part of an assembly loaded for introspection.
    // Instantiations of generic types are also recursively checked.
    BOOL ContainsIntrospectionOnlyTypes() const;
    
#endif //DACCESS_COMPILE

#ifdef FEATURE_PREJIT
    void Save(DataImage *image);
    void Fixup(DataImage *image);
#endif //FEATURE_PREJIT

#ifdef DACCESS_COMPILE
    static ULONG32 DacSize(TADDR addr)
    {
        DWORD numArgs = *PTR_DWORD(addr + offsetof(FnPtrTypeDesc, m_NumArgs));
        return (offsetof(FnPtrTypeDesc, m_RetAndArgTypes) +
            (numArgs * sizeof(TypeHandle)));
    }

    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif //DACCESS_COMPILE
    
protected:
    // Number of arguments
    DWORD m_NumArgs;

    // Calling convention (actually just a single byte)
    DWORD m_CallConv;

    // Return type first, then argument types
    TypeHandle m_RetAndArgTypes[1];
}; // class FnPtrTypeDesc

#endif // TYPEDESC_H
