// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==++==
//
//

//
// ==--==
//
// File: METHODTABLEBUILDER.H
//


//

//
// ============================================================================

#ifndef METHODTABLEBUILDER_H
#define METHODTABLEBUILDER_H

//---------------------------------------------------------------------------------------
//
// MethodTableBuilder simply acts as a holder for the
// large algorithm that "compiles" a type into
// a MethodTable/EEClass/DispatchMap/VTable etc. etc.
//
// The user of this class (the ClassLoader) currently builds the EEClass
// first, and does a couple of other things too, though all
// that work should probably be folded into BuildMethodTableThrowing.
//
class MethodTableBuilder
{

public:

    friend class EEClass;

    typedef UINT16 SLOT_INDEX;
    typedef ClrSafeInt<SLOT_INDEX> S_SLOT_INDEX;
    static const UINT16 INVALID_SLOT_INDEX = static_cast<UINT16>(-1);
    static const UINT16 MAX_SLOT_INDEX = static_cast<UINT16>(-1) - 10;

    // Information gathered by the class loader relating to generics
    // Fields in this structure are initialized very early in class loading
    // See code:ClassLoader.CreateTypeHandleForTypeDefThrowing
    struct bmtGenericsInfo
    {
        SigTypeContext typeContext;     // Type context used for metadata parsing
        WORD numDicts;                  // Number of dictionaries including this class
        BYTE *pVarianceInfo;            // Variance annotations on type parameters, NULL if none specified
        BOOL fTypicalInstantiation;     // TRUE if this is generic type definition
        BOOL fSharedByGenericInstantiations; // TRUE if this is canonical type shared by instantiations
        BOOL fContainsGenericVariables; // TRUE if this is an open type

        inline bmtGenericsInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
        inline DWORD GetNumGenericArgs() const { LIMITED_METHOD_CONTRACT; return typeContext.m_classInst.GetNumArgs(); }
        inline BOOL HasInstantiation() const { LIMITED_METHOD_CONTRACT; return typeContext.m_classInst.GetNumArgs() != 0; }
        inline BOOL IsTypicalTypeDefinition() const { LIMITED_METHOD_CONTRACT; return !HasInstantiation() || fTypicalInstantiation; }

        inline Instantiation GetInstantiation() const
        {
            LIMITED_METHOD_CONTRACT;
            return typeContext.m_classInst;
        }

#ifdef _DEBUG
        // Typical instantiation (= open type). Non-NULL only when loading any non-typical instantiation.
        // NULL if 'this' is a typical instantiation or a non-generic type.
        MethodTable * dbg_pTypicalInstantiationMT;

        inline MethodTable * Debug_GetTypicalMethodTable() const
        {
            LIMITED_METHOD_CONTRACT;
            return dbg_pTypicalInstantiationMT;
        }
#endif //_DEBUG
    };  // struct bmtGenericsInfo

    MethodTableBuilder(
        MethodTable *       pHalfBakedMT,
        EEClass *           pHalfBakedClass,
        StackingAllocator * pStackingAllocator,
        AllocMemTracker *   pAllocMemTracker)
        : m_pHalfBakedClass(pHalfBakedClass),
          m_pHalfBakedMT(pHalfBakedMT),
          m_pStackingAllocator(pStackingAllocator),
          m_pAllocMemTracker(pAllocMemTracker)
    {
        LIMITED_METHOD_CONTRACT;
        SetBMTData(
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL);
    }
public:
    //==========================================================================
    // This function is very specific about how it constructs a EEClass.
    //==========================================================================
    static EEClass * CreateClass(Module *pModule,
                            mdTypeDef cl,
                            BOOL fHasLayout,
                            BOOL fDelegate,
                            BOOL fIsEnum,
                            const bmtGenericsInfo *bmtGenericsInfo,
                            LoaderAllocator *pAllocator,
                            AllocMemTracker *pamTracker);

    static void GatherGenericsInfo(Module *pModule,
                                   mdTypeDef cl,
                                   Instantiation inst,
                                   bmtGenericsInfo *bmtGenericsInfo,
                                   StackingAllocator *pStackingAllocator);

    MethodTable *
    BuildMethodTableThrowing(
        LoaderAllocator *          pAllocator,
        Module *                   pLoaderModule,
        Module *                   pModule,
        mdToken                    cl,
        BuildingInterfaceInfo_t *  pBuildingInterfaceList,
        const LayoutRawFieldInfo * pLayoutRawFieldInfos,
        MethodTable *              pParentMethodTable,
        const bmtGenericsInfo *    bmtGenericsInfo,
        SigPointer                 parentInst,
        WORD                       wNumInterfaces);

    LPCWSTR GetPathForErrorMessages();

    BOOL ChangesImplementationOfVirtualSlot(SLOT_INDEX idx);

private:
    enum METHOD_IMPL_TYPE
    {
        METHOD_IMPL_NOT,
        METHOD_IMPL
    };

    enum METHOD_TYPE
    {
        // The values of the enum are in sync with MethodClassification.
        // GetMethodClassification depends on this
        METHOD_TYPE_NORMAL  = 0,
        METHOD_TYPE_FCALL   = 1,
        METHOD_TYPE_NDIRECT = 2,
        METHOD_TYPE_EEIMPL  = 3,
        METHOD_TYPE_INSTANTIATED = 5,
#ifdef FEATURE_COMINTEROP
        METHOD_TYPE_COMINTEROP = 6,
#endif
    };

private:
    // Determine if this is the special SIMD type System.Numerics.Vector<T>, and set its size.
    BOOL CheckIfSIMDAndUpdateSize();

    // <NICE> Get rid of this.</NICE>
    PTR_EEClass m_pHalfBakedClass;
    PTR_MethodTable m_pHalfBakedMT;

    // GetHalfBakedClass: The EEClass you get back from this function may not have all its fields filled in yet.
    // Thus you have to make sure that the relevant item which you are accessing has
    // been correctly initialized in the EEClass/MethodTable construction sequence
    // at the point at which you access it.
    //
    // Gradually we will move the code to a model where the process of constructing an EEClass/MethodTable
    // is more obviously correct, e.g. by relying much less on reading information using GetHalfBakedClass
    // and GetHalfBakedMethodTable.
    //
    // <NICE> Get rid of this.</NICE>
    PTR_EEClass GetHalfBakedClass() { LIMITED_METHOD_CONTRACT; return m_pHalfBakedClass; }
    PTR_MethodTable GetHalfBakedMethodTable() { LIMITED_METHOD_CONTRACT; return m_pHalfBakedMT; }

    HRESULT GetCustomAttribute(mdToken parentToken, WellKnownAttribute attribute, const void  **ppData, ULONG *pcbData)
    {
        WRAPPER_NO_CONTRACT;
        if (GetModule()->IsReadyToRun())
        {
            if (!GetModule()->GetReadyToRunInfo()->MayHaveCustomAttribute(attribute, parentToken))
                return S_FALSE;
        }

        return GetMDImport()->GetCustomAttributeByName(parentToken, GetWellKnownAttributeName(attribute), ppData, pcbData);
    }

    // <NOTE> The following functions are used during MethodTable construction to access/set information about the type being constructed.
    // Beware that some of the fields of the underlying EEClass/MethodTable being constructed may not
    // be initialized.  Because of this, ideally the code will gradually be cleaned up so that
    // none of these functions are used and instead we use the data in the bmt structures below
    // or we explicitly pass around the data as arguments. </NOTE>
    //
    // <NICE> Get rid of all of these.</NICE>
    mdTypeDef GetCl()    { WRAPPER_NO_CONTRACT; return bmtInternal->pType->GetTypeDefToken(); }
    BOOL IsGlobalClass() { WRAPPER_NO_CONTRACT; return GetCl() == COR_GLOBAL_PARENT_TOKEN; }
    DWORD GetAttrClass() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetAttrClass(); }
    WORD GetNumHandleRegularStatics() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetNumHandleRegularStatics(); }
    WORD GetNumHandleThreadStatics() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetNumHandleThreadStatics(); }
    WORD GetNumStaticFields() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetNumStaticFields(); }
    WORD GetNumInstanceFields() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetNumInstanceFields(); }
    BOOL IsInterface() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsInterface(); }
    BOOL HasOverLayedField() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasOverLayedField(); }
    BOOL IsComImport() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsComImport(); }
#ifdef FEATURE_COMINTEROP
    void SetIsComClassInterface() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetIsComClassInterface(); }
#endif // FEATURE_COMINTEROP
    BOOL IsEnum() { WRAPPER_NO_CONTRACT; return bmtProp->fIsEnum; }
    BOOL HasNonPublicFields() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasNonPublicFields(); }
    BOOL IsValueClass() { WRAPPER_NO_CONTRACT; return bmtProp->fIsValueClass; }
    BOOL IsUnsafeValueClass() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsUnsafeValueClass(); }
    BOOL IsAbstract() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsAbstract(); }
    BOOL HasLayout() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasLayout(); }
    BOOL IsDelegate() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsDelegate(); }
    BOOL IsNested() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsNested(); }
    BOOL HasFieldsWhichMustBeInited() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasFieldsWhichMustBeInited(); }
    BOOL IsBlittable() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsBlittable(); }
    PTR_MethodDescChunk GetChunks() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetChunks(); }
    BOOL HasExplicitFieldOffsetLayout() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasExplicitFieldOffsetLayout(); }
    BOOL IsManagedSequential() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->IsManagedSequential(); }
    BOOL HasExplicitSize() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->HasExplicitSize(); }

#ifdef _DEBUG
    LPCUTF8 GetDebugClassName() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetDebugClassName(); }
#endif // _DEBUG
    Assembly *GetAssembly() { WRAPPER_NO_CONTRACT; return GetModule()->GetAssembly(); }
    Module *GetModule() { WRAPPER_NO_CONTRACT; return bmtInternal->pModule; }
    ClassLoader *GetClassLoader() { WRAPPER_NO_CONTRACT; return GetModule()->GetClassLoader(); }
    IMDInternalImport* GetMDImport()  { WRAPPER_NO_CONTRACT; return bmtInternal->pInternalImport; }
    FieldDesc *GetApproxFieldDescListRaw() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetFieldDescList(); }
    EEClassLayoutInfo *GetLayoutInfo() { WRAPPER_NO_CONTRACT; return GetHalfBakedClass()->GetLayoutInfo(); }

    // <NOTE> The following functions are used during MethodTable construction to setup information
    // about the type being constructed in particular information stored in the EEClass.
    // USE WITH CAUTION!!  TRY NOT TO ADD MORE OF THESE!! </NOTE>
    //
    // <NICE> Get rid of all of these - we should be able to evaluate these conditions BEFORE
    // we create the EEClass object, and thus set the flags immediately at the point
    // we create that object.</NICE>
    void SetUnsafeValueClass() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetUnsafeValueClass(); }
    void SetCannotBeBlittedByObjectCloner() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetCannotBeBlittedByObjectCloner(); }
    void SetHasFieldsWhichMustBeInited() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetHasFieldsWhichMustBeInited(); }
    void SetHasNonPublicFields() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetHasNonPublicFields(); }
    void SetModuleDynamicID(DWORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetModuleDynamicID(x); }
    void SetNumHandleRegularStatics(WORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNumHandleRegularStatics(x); }
    void SetNumHandleThreadStatics(WORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNumHandleThreadStatics(x); }
    void SetNumBoxedRegularStatics(WORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNumBoxedRegularStatics(x); }
    void SetNumBoxedThreadStatics(WORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNumBoxedThreadStatics(x); }
    void SetAlign8Candidate() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetAlign8Candidate(); }
    void SetHasOverLayedFields() { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetHasOverLayedFields(); }
    void SetNonGCRegularStaticFieldBytes(DWORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNonGCRegularStaticFieldBytes(x); }
    void SetNonGCThreadStaticFieldBytes(DWORD x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetNonGCThreadStaticFieldBytes(x); }
#ifdef _DEBUG
    void SetDebugClassName(LPUTF8 x) { WRAPPER_NO_CONTRACT; GetHalfBakedClass()->SetDebugClassName(x); }
#endif

    // Must be called prior to setting the value of any optional field on EEClass (on a debug build an assert
    // will fire if this invariant is violated).
    static void EnsureOptionalFieldsAreAllocated(EEClass *pClass, AllocMemTracker *pamTracker, LoaderHeap *pHeap);

    /************************************
     *  PRIVATE INTERNAL STRUCTS
     ************************************/
private:
    //The following structs are used in buildmethodtable
    // The 'bmt' in front of each struct reminds us these are for MethodTableBuilder

    // --------------------------------------------------------------------------------------------
    struct bmtErrorInfo
    {
        UINT resIDWhy;
        LPCUTF8 szMethodNameForError;
        mdToken dMethodDefInError;
        Module* pModule;
        mdTypeDef cl;
        OBJECTREF *pThrowable;

        // Set the reason and the offending method def. If the method information
        // is not from this class set the method name and it will override the method def.
        inline bmtErrorInfo()
            : resIDWhy(0),
              szMethodNameForError(NULL),
              dMethodDefInError(mdMethodDefNil),
              pThrowable(NULL)
            { LIMITED_METHOD_CONTRACT; }
    };

    // --------------------------------------------------------------------------------------------
    class bmtRTType
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Note that the immediate substitution is copied, but this assumes that
        // the remaining substitutions in the chain are in a stable memory location
        // for the lifetime of this object.
        bmtRTType(
            const Substitution & subst,
            MethodTable *        pMT)
            : m_subst(subst),
              m_pMT(pMT),
              m_pParent(NULL)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Returns the parent type. Takes advantage of the fact that an RT type will
        // have only RT types as parents. I don't anticipate this changing.
        bmtRTType *
        GetParentType() const
            { LIMITED_METHOD_CONTRACT; return m_pParent; }

        //-----------------------------------------------------------------------------------------
        // Sets the parent type. Used during construction of the type chain, due
        // to the fact that types point up the chain but substitutions point down.
        void
        SetParentType(
            bmtRTType * pParentType)
            { LIMITED_METHOD_CONTRACT; m_pParent = pParentType; }

        //-----------------------------------------------------------------------------------------
        bool
        IsNested() const
            { LIMITED_METHOD_CONTRACT; return GetMethodTable()->GetClass()->IsNested() != FALSE; }

        //-----------------------------------------------------------------------------------------
        mdTypeDef
        GetEnclosingTypeToken() const;

        //-----------------------------------------------------------------------------------------
        // Reference to the substitution for this type. Substitutions are linked
        // inline with the type chain; this is more efficient than creating an
        // entire type chain for each parent type and also keeps the type and
        // substitution tightly coupled for easier use.
        const Substitution &
        GetSubstitution() const
            { LIMITED_METHOD_CONTRACT; return m_subst; }

        //-----------------------------------------------------------------------------------------
        // Changes type's substitution - used for interface map building.
        void
        SetSubstitution(const Substitution & subst)
        {
            LIMITED_METHOD_CONTRACT;
            m_subst = subst;
        }

        //-----------------------------------------------------------------------------------------
        // Returns the runtime Module that owns this type.
        Module *
        GetModule() const
            { WRAPPER_NO_CONTRACT; return GetMethodTable()->GetModule(); }

        //-----------------------------------------------------------------------------------------
        // Returns the runtime MethodTable for the type.
        MethodTable *
        GetMethodTable() const
            { LIMITED_METHOD_CONTRACT; return m_pMT; }

        //-----------------------------------------------------------------------------------------
        // Returns the metadata token for this type.
        mdTypeDef
        GetTypeDefToken() const
            { WRAPPER_NO_CONTRACT; return GetMethodTable()->GetCl();}

        //-----------------------------------------------------------------------------------------
        // Returns the metadata attributes for this type.
        DWORD
        GetAttrs() const
            { WRAPPER_NO_CONTRACT; return GetMethodTable()->GetClass()->GetAttrClass(); }

        //-----------------------------------------------------------------------------------------
        // true if the type is an interface; false otherwise.
        bool
        IsInterface() const
            { WRAPPER_NO_CONTRACT; return GetMethodTable()->IsInterface() != FALSE; }

        //-----------------------------------------------------------------------------------------
        // Helper function to find a type associated with pTargetMT in the
        // chain pointed to by pType.
        static
        bmtRTType *
        FindType(
            bmtRTType *          pType,
            MethodTable *        pTargetMT);

    private:
        //-----------------------------------------------------------------------------------------
        Substitution    m_subst;
        MethodTable *   m_pMT;
        bmtRTType *     m_pParent;
    };  // class bmtRTType

    // --------------------------------------------------------------------------------------------
    // This creates a chain of bmtRTType objects representing pMT and all of pMT's parent types.
    bmtRTType *
    CreateTypeChain(
        MethodTable *        pMT,
        const Substitution & subst);

    // --------------------------------------------------------------------------------------------
    class bmtMDType
    {
    public:
        //-----------------------------------------------------------------------------------------
        bmtMDType(
            bmtRTType *             pParentType,
            Module *                pModule,
            mdTypeDef               tok,
            const SigTypeContext &  sigContext);

        //-----------------------------------------------------------------------------------------
        // Returns the parent type. This takes advantage of teh fact that an MD type
        // will always have an RT type as a parent. This could change, at which point
        // it would have to return a bmtTypeHandle.
        bmtRTType *
        GetParentType() const
            { LIMITED_METHOD_CONTRACT; return m_pParentType; }

        //-----------------------------------------------------------------------------------------
        // Used during construction of the type chain, due to the fact that types point
        // up the chain but substitutions point down.
        void
        SetParentType(
            bmtRTType * pParentType)
            { LIMITED_METHOD_CONTRACT; m_pParentType = pParentType; }

        //-----------------------------------------------------------------------------------------
        bool
        IsNested() const
            { LIMITED_METHOD_CONTRACT; return m_enclTok != mdTypeDefNil; }

        //-----------------------------------------------------------------------------------------
        mdTypeDef
        GetEnclosingTypeToken() const
            { LIMITED_METHOD_CONTRACT; return m_enclTok; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to the substitution. Currently, no substitution exists
        // for the type being built, but it adds uniformity to the types and so a NULL
        // substitution is created.
        const Substitution &
        GetSubstitution() const
            { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(m_subst.GetModule() == NULL); return m_subst; }

        //-----------------------------------------------------------------------------------------
        // Returns the runtime Module that owns this type.
        Module *
        GetModule() const
            { LIMITED_METHOD_CONTRACT; return m_pModule; }

        //-----------------------------------------------------------------------------------------
        // Returns the MethodTable for the type. This is  null until the very end
        // of BuildMethodTableThrowing when the MethodTable for this type is finally
        // created in SetupMethodTable2.
        MethodTable *
        GetMethodTable() const
            { LIMITED_METHOD_CONTRACT; return m_pMT; }

        //-----------------------------------------------------------------------------------------
        // Returns the token for the type.
        mdTypeDef
        GetTypeDefToken() const
            { LIMITED_METHOD_CONTRACT; return m_tok;}

        //-----------------------------------------------------------------------------------------
        // Returns the metadata attributes for the type.
        DWORD
        GetAttrs() const
            { WRAPPER_NO_CONTRACT; return m_dwAttrs; }

        //-----------------------------------------------------------------------------------------
        // true if the type is an interface; false otherwise.
        bool
        IsInterface() const
            { WRAPPER_NO_CONTRACT; return IsTdInterface(GetAttrs()); }

    private:
        //-----------------------------------------------------------------------------------------
        bmtRTType *     m_pParentType;
        Module *        m_pModule;
        mdTypeDef       m_tok;
        mdTypeDef       m_enclTok;
        SigTypeContext  m_sigContext;
        Substitution    m_subst;
        DWORD           m_dwAttrs;

        MethodTable *   m_pMT;
    };  // class bmtMDType

    // --------------------------------------------------------------------------------------------
    // This is similar to the known and loved TypeHandle class, but tailored for use during
    // type building. It allows for homogeneous collections of heterogeneous implementations.
    // Currently, it knows the difference between a bmtRTType and a bmtMDType and will
    // forward method calls such as GetModule, GetParentType and more to the appropriate
    // target.
    class bmtTypeHandle
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Creates a type handle for a bmtRTType pointer. For ease of use, this conversion
        // constructor is not declared as explicit.
        bmtTypeHandle(
            bmtRTType * pRTType)
            : m_handle(HandleFromRTType(pRTType))
            { NOT_DEBUG(static_assert_no_msg(sizeof(bmtTypeHandle) == sizeof(UINT_PTR));) INDEBUG(m_pAsRTType = pRTType;) }

        //-----------------------------------------------------------------------------------------
        // Creates a type handle for a bmtMDType pointer. For ease of use, this conversion
        // constructor is not declared as explicit.
        bmtTypeHandle(
            bmtMDType * pMDType)
            : m_handle(HandleFromMDType(pMDType))
            { NOT_DEBUG(static_assert_no_msg(sizeof(bmtTypeHandle) == sizeof(UINT_PTR));) INDEBUG(m_pAsMDType = pMDType;) }

        //-----------------------------------------------------------------------------------------
        // Copy constructor.
        bmtTypeHandle(
            const bmtTypeHandle &other)
            { LIMITED_METHOD_CONTRACT; m_handle = other.m_handle; INDEBUG(m_pAsRTType = other.m_pAsRTType;) }

        //-----------------------------------------------------------------------------------------
        // Default, null constructor.
        bmtTypeHandle()
            { LIMITED_METHOD_CONTRACT; m_handle = 0; INDEBUG(m_pAsRTType = NULL;) }

        //-----------------------------------------------------------------------------------------
        // Assignment operator
        bmtTypeHandle &
        operator=(
            const bmtTypeHandle &rhs)
            { LIMITED_METHOD_CONTRACT; m_handle = rhs.m_handle; INDEBUG(m_pAsRTType = rhs.m_pAsRTType;) return *this; }

        //-----------------------------------------------------------------------------------------
        // Returns true if null (constructed using default ctor, or assigned from one); otherwise false.
        bool
        IsNull() const
            { LIMITED_METHOD_CONTRACT; return m_handle == 0; }

        //-----------------------------------------------------------------------------------------
        // Returns true if this handle contains a bmtRTType pointer; otherwise returns false.
        bool
        IsRTType() const
            { LIMITED_METHOD_CONTRACT; return (m_handle & RTTYPE_FLAG) != 0; }

        //-----------------------------------------------------------------------------------------
        // Returns true if this handle contains a bmtMDType pointer; otherwise returns false.
        bool
        IsMDType() const
            { LIMITED_METHOD_CONTRACT; return (m_handle & MDTYPE_FLAG) != 0; }

        //-----------------------------------------------------------------------------------------
        // Returns pointer to bmtRTType. IsRTType is required
        // to return true before calling this method.
        bmtRTType *
        AsRTType() const
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsRTType());
            return (bmtRTType *) Decode(m_handle);
        }

        //-----------------------------------------------------------------------------------------
        // Returns pointer to bmtMDType. IsMDType is required
        // to return true before calling this method.
        bmtMDType *
        AsMDType() const
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsMDType());
            return (bmtMDType *) Decode(m_handle);
        }

        //-----------------------------------------------------------------------------------------
        // Returns the parent type handle, or the null type handle if no parent exists.
        bmtTypeHandle
        GetParentType() const;

        //-----------------------------------------------------------------------------------------
        bool
        IsNested() const;

        //-----------------------------------------------------------------------------------------
        mdTypeDef
        GetEnclosingTypeToken() const;

        //-----------------------------------------------------------------------------------------
        // Returns the runtime Module* for this type.
        Module *
        GetModule() const;

        //-----------------------------------------------------------------------------------------
        // Returns the token for the type.
        mdTypeDef
        GetTypeDefToken() const;

        //-----------------------------------------------------------------------------------------
        // Returns reference to the substitution for this type.
        const Substitution &
        GetSubstitution() const;

        //-----------------------------------------------------------------------------------------
        // Returns the MethodTable* for the type.
        MethodTable *
        GetMethodTable() const;

        //-----------------------------------------------------------------------------------------
        // Returns the metadata attributes for the type.
        DWORD
        GetAttrs() const;

        //-----------------------------------------------------------------------------------------
        // Returns true if this type is an interface; returns false otherwise.
        bool
        IsInterface() const;

        //-----------------------------------------------------------------------------------------
        static bool
        Equal(
            const bmtTypeHandle &lhs,
            const bmtTypeHandle &rhs)
        {
            return lhs.m_handle == rhs.m_handle;
        }

    protected:
        //-----------------------------------------------------------------------------------------
        static const UINT_PTR RTTYPE_FLAG = 0x1;
        static const UINT_PTR MDTYPE_FLAG = 0x2;
        static const UINT_PTR MASK_FLAG     = 0x3;

        //-----------------------------------------------------------------------------------------
        // Takes a pointer and encodes it with the flag.
        static UINT_PTR
        Encode(
            LPVOID   pv,
            UINT_PTR flag)
        {
            LIMITED_METHOD_CONTRACT;
            CONSISTENCY_CHECK((reinterpret_cast<UINT_PTR>(pv) & MASK_FLAG) == 0);
            return (reinterpret_cast<UINT_PTR>(pv) | flag);
        }

        //-----------------------------------------------------------------------------------------
        // Takes an encoded handle and removes encoding bits.
        static LPVOID
        Decode(
            UINT_PTR handle)
            { LIMITED_METHOD_CONTRACT; return reinterpret_cast<LPVOID>(handle & ~MASK_FLAG); }

        //-----------------------------------------------------------------------------------------
        // Uses encode to produce a handle for a bmtRTType*
        static UINT_PTR
        HandleFromRTType(
            bmtRTType * pRTType)
            { WRAPPER_NO_CONTRACT; return Encode(pRTType, RTTYPE_FLAG); }

        //-----------------------------------------------------------------------------------------
        // Uses encode to produce a handle for a bmtMDType*
        static UINT_PTR
        HandleFromMDType(
            bmtMDType * pMDType)
            { WRAPPER_NO_CONTRACT; return Encode(pMDType, MDTYPE_FLAG); }

        //-----------------------------------------------------------------------------------------
        UINT_PTR m_handle;

#ifdef _DEBUG
        //-----------------------------------------------------------------------------------------
        // Used in debug builds to quickly access the type in a debugger.
        union
        {
            bmtRTType * m_pAsRTType;
            bmtMDType * m_pAsMDType;
        };
#endif
    };  // class bmtTypeHandle

    // --------------------------------------------------------------------------------------------
    // MethodSignature encapsulates the name and metadata signature of a method, as well as
    // the scope (Module*) and substitution for the signature. It is intended to facilitate
    // passing around this tuple of information as well as providing efficient comparison
    // operations when looking for types.
    //
    // Meant to be passed around by reference or by value. Please make sure this is declared
    // on the stack or properly deleted after use.

    class MethodSignature
    {
    public:
        //-----------------------------------------------------------------------------------------
        // This is the constructor usually used, and is typically contained inside a
        // bmtMDMethod or bmtRTMethod.
        MethodSignature(
            Module *             pModule,
            mdToken              tok,
            const Substitution * pSubst)
            : m_pModule(pModule),
              m_tok(tok),
              m_szName(NULL),
              m_pSig(NULL),
              m_cSig(0),
              m_pSubst(pSubst),
              m_nameHash(INVALID_NAME_HASH)
            {
                CONTRACTL {
                    PRECONDITION(CheckPointer(pModule));
                    PRECONDITION(TypeFromToken(tok) == mdtMethodDef ||
                                 TypeFromToken(tok) == mdtMemberRef);
                } CONTRACTL_END;
                INDEBUG(CheckGetMethodAttributes();)
            }

        //-----------------------------------------------------------------------------------------
        // This constructor can be used with hard-coded signatures that are used for
        // locating .ctor and .cctor methods.
        MethodSignature(
            Module *             pModule,
            LPCUTF8              szName,
            PCCOR_SIGNATURE      pSig,
            size_t               cSig,
            const Substitution * pSubst = NULL)
            : m_pModule(pModule),
              m_tok(mdTokenNil),
              m_szName(szName),
              m_pSig(pSig),
              m_cSig(cSig),
              m_pSubst(pSubst),
              m_nameHash(INVALID_NAME_HASH)
            {
                CONTRACTL {
                    PRECONDITION(CheckPointer(pModule));
                    PRECONDITION(CheckPointer(szName));
                    PRECONDITION(CheckPointer(pSig));
                    PRECONDITION(cSig != 0);
                } CONTRACTL_END;
            }

        //-----------------------------------------------------------------------------------------
        // Copy constructor.
        MethodSignature(
            const MethodSignature & s)
            : m_pModule(s.m_pModule),
              m_tok(s.m_tok),
              m_szName(s.m_szName),
              m_pSig(s.m_pSig),
              m_cSig(s.m_cSig),
              m_pSubst(s.m_pSubst),
              m_nameHash(s.m_nameHash)
            { }

        MethodSignature GetSignatureWithoutSubstitution() const
        {
            LIMITED_METHOD_CONTRACT;
            MethodSignature sig = *this;
            sig.m_pSubst = NULL;
            return sig;
        }

        //-----------------------------------------------------------------------------------------
        // Returns the module that is the scope within which the signature itself lives.
        Module *
        GetModule() const
            { LIMITED_METHOD_CONTRACT; return m_pModule; }

        //-----------------------------------------------------------------------------------------
        // Returns the signature token. Note that this can be mdTokenNil if the second
        // constructor above is used.
        mdToken
        GetToken() const
            { LIMITED_METHOD_CONTRACT; return m_tok; }

        //-----------------------------------------------------------------------------------------
        // Returns the name of the method.
        inline LPCUTF8
        GetName() const
            { WRAPPER_NO_CONTRACT; CheckGetMethodAttributes(); return m_szName; }

        //-----------------------------------------------------------------------------------------
        // Returns the metadata signature for the method.
        inline PCCOR_SIGNATURE
        GetSignature() const
            { WRAPPER_NO_CONTRACT; CheckGetMethodAttributes(); return m_pSig; }

        //-----------------------------------------------------------------------------------------
        // Returns the signature length.
        inline size_t
        GetSignatureLength() const
            { WRAPPER_NO_CONTRACT; CheckGetMethodAttributes(); return m_cSig; }

        //-----------------------------------------------------------------------------------------
        // Returns the substitution to be used in interpreting the signature.
        const Substitution &
        GetSubstitution() const
            { return *m_pSubst; }

        //-----------------------------------------------------------------------------------------
        // Returns true if the names are equal; otherwise returns false. This is a
        // case-sensitive comparison.
        static bool
        NamesEqual(
            const MethodSignature & sig1,
            const MethodSignature & sig2);

        //-----------------------------------------------------------------------------------------
        // Returns true if the metadata signatures (PCCOR_SIGNATURE) are equivalent. (Type equivalence permitted)
        static bool
        SignaturesEquivalent(
            const MethodSignature & sig1,
            const MethodSignature & sig2,
            BOOL allowCovariantReturn);

        //-----------------------------------------------------------------------------------------
        // Returns true if the metadata signatures (PCCOR_SIGNATURE) are exactly equal. (No type equivalence permitted)
        static bool
        SignaturesExactlyEqual(
            const MethodSignature & sig1,
            const MethodSignature & sig2);
        //-----------------------------------------------------------------------------------------
        // This is a combined name and sig comparison. Semantically equivalent to
        // "NamesEqual(*this, rhs) && SignaturesEquivalent(*this, rhs)".
        bool
        Equivalent(
            const MethodSignature &rhs) const;

        //-----------------------------------------------------------------------------------------
        // This is a combined name and sig comparison. Semantically equivalent to
        // "NamesEqual(*this, rhs) && SignaturesExactlyEqual(*this, rhs)".
        bool
        ExactlyEqual(
            const MethodSignature &rhs) const;

        //-----------------------------------------------------------------------------------------
        // Conversion operator to Module*. This should possibly be removed.
        operator Module *() const
            { return GetModule(); }

        //-----------------------------------------------------------------------------------------
        // Conversion operator to LPCUTF8, returning name. This should possibly be removed.
        operator LPCUTF8() const
            { return GetName(); }

        //-----------------------------------------------------------------------------------------
        // Conversion operator to PCCOR_SIGNATURE. This should possibly be removed.
        operator PCCOR_SIGNATURE() const
            { return GetSignature(); }

    protected:
        //-----------------------------------------------------------------------------------------
        Module *                m_pModule;
        mdToken                 m_tok;
        mutable LPCUTF8         m_szName;   // mutable because it is lazily evaluated.
        mutable PCCOR_SIGNATURE m_pSig;     // mutable because it is lazily evaluated.
        mutable size_t          m_cSig;     // mutable because it is lazily evaluated.
        const Substitution *    m_pSubst;

        static const ULONG      INVALID_NAME_HASH = static_cast<ULONG>(-1);
        mutable ULONG           m_nameHash; // mutable because it is lazily evaluated.

        //-----------------------------------------------------------------------------------------
        inline void
        CheckGetMethodAttributes() const
        {
            WRAPPER_NO_CONTRACT;
            if (m_tok != mdTokenNil && m_szName == NULL)
            {
                GetMethodAttributes();
            }
        }

        //-----------------------------------------------------------------------------------------
        void
        GetMethodAttributes() const;

        //-----------------------------------------------------------------------------------------
        UINT32
        GetNameHash() const;

    private:
        //-----------------------------------------------------------------------------------
        // Private to prevent use.
        MethodSignature *
        operator&()
            { return this; }
    };  // class MethodSignature

    // --------------------------------------------------------------------------------------------
    class bmtRTMethod
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Constructor.
        bmtRTMethod(
            bmtRTType *     pOwningType,
            MethodDesc *    pMD);

        //-----------------------------------------------------------------------------------------
        // Returns owning type for this method.
        bmtRTType *
        GetOwningType() const
            { LIMITED_METHOD_CONTRACT; return m_pOwningType; }

        //-----------------------------------------------------------------------------------------
        // Returns MethodDesc* for this method.
        MethodDesc *
        GetMethodDesc() const
            { LIMITED_METHOD_CONTRACT; return m_pMD; }

        //-----------------------------------------------------------------------------------------
        // Returns reference to MethodSignature object for this type.
        const MethodSignature &
        GetMethodSignature() const
            { LIMITED_METHOD_CONTRACT; return m_methodSig; }

        //-----------------------------------------------------------------------------------------
        // Returns metadata declaration attributes for this method.
        DWORD
        GetDeclAttrs() const;

        //-----------------------------------------------------------------------------------------
        // Returns metadata implementation attributes for this method.
        DWORD
        GetImplAttrs() const;

        //-----------------------------------------------------------------------------------------
        // Returns the slot in which this method is placed.
        SLOT_INDEX
        GetSlotIndex() const;

    private:
        //-----------------------------------------------------------------------------------------
        bmtRTType *     m_pOwningType;
        MethodDesc *    m_pMD;
        MethodSignature m_methodSig;
    };  // class bmtRTMethod

    // --------------------------------------------------------------------------------------------
    // Encapsulates method data for a method described by metadata.
    class bmtMDMethod
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Constructor. This takes all the information already extracted from metadata interface
        // because the place that creates these types already has this data. Alternatively,
        // a constructor could be written to take a token and metadata scope instead. Also,
        // it might be interesting to move METHOD_TYPE and METHOD_IMPL_TYPE to setter functions.
        bmtMDMethod(
            bmtMDType * pOwningType,
            mdMethodDef tok,
            DWORD dwDeclAttrs,
            DWORD dwImplAttrs,
            DWORD dwRVA,
            METHOD_TYPE type,
            METHOD_IMPL_TYPE implType);

        //-----------------------------------------------------------------------------------------
        // Returns the type that owns the *declaration* of this method. This makes sure that a
        // method can be properly interpreted in the context of substitutions at any time.
        bmtMDType *
        GetOwningType() const
            { LIMITED_METHOD_CONTRACT; return m_pOwningType; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to the MethodSignature for this method.
        const MethodSignature &
        GetMethodSignature() const
            { LIMITED_METHOD_CONTRACT; return m_methodSig; }

        //-----------------------------------------------------------------------------------------
        // Sets the slot that this method is assigned to.
        void
        SetSlotIndex(SLOT_INDEX idx);

        //-----------------------------------------------------------------------------------------
        // Returns the slot that this method is assigned to.
        SLOT_INDEX
        GetSlotIndex() const
            { LIMITED_METHOD_CONTRACT; return m_slotIndex; }

        //-----------------------------------------------------------------------------------------
        // Returns the method type (normal, fcall, etc.) that this type was constructed with.
        METHOD_TYPE
        GetMethodType() const
            { LIMITED_METHOD_CONTRACT; return m_type; }

        //-----------------------------------------------------------------------------------------
        // Returns the method impl type (is or isn't) that this type was constructed with.
        METHOD_IMPL_TYPE
        GetMethodImplType() const
            { LIMITED_METHOD_CONTRACT; return m_implType; }

        //-----------------------------------------------------------------------------------------
        // Gets the MethodDesc* for this method. Defaults to NULL until SetMethodDesc is called
        // with a non-NULL MethodDesc* value.
        MethodDesc *
        GetMethodDesc() const
            { LIMITED_METHOD_CONTRACT; _ASSERTE(m_pMD != NULL); return m_pMD; }

        //-----------------------------------------------------------------------------------------
        // Once a MethodDesc* is created for this method, this method will store the association.
        void
        SetMethodDesc(MethodDesc * pMD)
            { LIMITED_METHOD_CONTRACT; _ASSERTE(m_pMD == NULL); m_pMD = pMD; }

        //-----------------------------------------------------------------------------------------
        // Virtual slots for ValueTypes are converted to stubs which unbox the incoming boxed
        // "this" argument, and forward the call to the unboxed entrypoint.
        bool
        IsUnboxing()
            { WRAPPER_NO_CONTRACT; return GetUnboxedSlotIndex() != INVALID_SLOT_INDEX; }

        //-----------------------------------------------------------------------------------------
        // This and SetUnboxedMethodDesc are used to indicate that this method exists as a dual
        // entrypoint method for a ValueType.
        void
        SetUnboxedSlotIndex(SLOT_INDEX idx);

        //-----------------------------------------------------------------------------------------
        // Returns the slot for the unboxed entrypoint. If no such slot exists, returns
        // INVALID_SLOT_INDEX.
        SLOT_INDEX
        GetUnboxedSlotIndex() const
            { LIMITED_METHOD_CONTRACT; return m_unboxedSlotIndex; }

        //-----------------------------------------------------------------------------------------
        // Returns the MethodDesc* for the unboxed entrypoint. If no such pointer exists, returns
        // NULL.
        MethodDesc *
        GetUnboxedMethodDesc() const
            { LIMITED_METHOD_CONTRACT; _ASSERTE(m_pMD != NULL); return m_pUnboxedMD; }

        //-----------------------------------------------------------------------------------------
        // Sets the MethodDesc* for the unboxed entrypoint.
        void
        SetUnboxedMethodDesc(MethodDesc * pUnboxingMD)
            { LIMITED_METHOD_CONTRACT; _ASSERTE(m_pUnboxedMD == NULL); m_pUnboxedMD = pUnboxingMD; }

        //-----------------------------------------------------------------------------------------
        // Returns the metadata declaration attributes for this method.
        DWORD
        GetDeclAttrs() const
            { LIMITED_METHOD_CONTRACT; return m_dwDeclAttrs; }

        //-----------------------------------------------------------------------------------------
        // Returns the metadata implementation attributes for this method.
        DWORD
        GetImplAttrs() const
            { LIMITED_METHOD_CONTRACT; return m_dwImplAttrs; }

        //-----------------------------------------------------------------------------------------
        // Returns the RVA for the metadata of this method.
        DWORD
        GetRVA() const
            { LIMITED_METHOD_CONTRACT; return m_dwRVA; }

    private:
        //-----------------------------------------------------------------------------------------
        bmtMDType *       m_pOwningType;

        DWORD             m_dwDeclAttrs;
        DWORD             m_dwImplAttrs;
        DWORD             m_dwRVA;
        METHOD_TYPE       m_type;               // Specific MethodDesc flavour
        METHOD_IMPL_TYPE  m_implType;           // Whether or not the method is a methodImpl body
        MethodSignature   m_methodSig;

        MethodDesc *      m_pMD;                // MethodDesc created and assigned to this method
        MethodDesc *      m_pUnboxedMD;         // Unboxing MethodDesc if this is a virtual method on a valuetype
        SLOT_INDEX        m_slotIndex;          // Vtable slot number this method is assigned to
        SLOT_INDEX        m_unboxedSlotIndex;
    };  // class bmtMDMethod

    // --------------------------------------------------------------------------------------------
    // Provides a homogeneous view over potentially different types similar to bmtTypeHandle and
    // TypeHandle. Currently can handle
    class bmtMethodHandle
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Constructor taking a bmtRTMethod*.
        bmtMethodHandle(
            bmtRTMethod * pRTMethod)
            : m_handle(HandleFromRTMethod(pRTMethod))
            { NOT_DEBUG(static_assert_no_msg(sizeof(bmtMethodHandle) == sizeof(UINT_PTR));) INDEBUG(m_pAsRTMethod = pRTMethod;) }

        //-----------------------------------------------------------------------------------------
        // Constructor taking a bmtMDMethod*.
        bmtMethodHandle(
            bmtMDMethod * pMDMethod)
            : m_handle(HandleFromMDMethod(pMDMethod))
            { NOT_DEBUG(static_assert_no_msg(sizeof(bmtMethodHandle) == sizeof(UINT_PTR));) INDEBUG(m_pAsMDMethod = pMDMethod;) }

        //-----------------------------------------------------------------------------------------
        // Copy constructor.
        bmtMethodHandle(
            const bmtMethodHandle &other)
            { LIMITED_METHOD_CONTRACT; m_handle = other.m_handle; INDEBUG(m_pAsRTMethod = other.m_pAsRTMethod;) }

        //-----------------------------------------------------------------------------------------
        // Default constructor. Handle defaults to NULL.
        bmtMethodHandle()
            { LIMITED_METHOD_CONTRACT; m_handle = 0; INDEBUG(m_pAsRTMethod = NULL;) }

        //-----------------------------------------------------------------------------------------
        // Assignment.
        bmtMethodHandle &
        operator=(
            const bmtMethodHandle &rhs)
        {
            LIMITED_METHOD_CONTRACT;
            m_handle = rhs.m_handle;
            INDEBUG(m_pAsRTMethod = rhs.m_pAsRTMethod;)
            return *this;
        }

        //-----------------------------------------------------------------------------------------
        // Returns true if default constructed or assigned to from a NULL handle.
        bool
        IsNull() const
            { LIMITED_METHOD_CONTRACT; return m_handle == 0; }

        //-----------------------------------------------------------------------------------------
        // Returns true if the handle points to a bmtRTMethod; returns false otherwise.
        bool
        IsRTMethod() const
            { LIMITED_METHOD_CONTRACT; return (m_handle & RTMETHOD_FLAG) != 0; }

        //-----------------------------------------------------------------------------------------
        // Returns true if the handle points to a bmtMDMethod; returns false otherwise.
        bool
        IsMDMethod() const
            { LIMITED_METHOD_CONTRACT; return (m_handle & MDMETHOD_FLAG) != 0; }

        //-----------------------------------------------------------------------------------------
        // Returns pointer to bmtRTMethod. IsRTMethod is required to return true before
        // calling this method.
        bmtRTMethod *
        AsRTMethod() const
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsRTMethod());
            return (bmtRTMethod *) Decode(m_handle);
        }

        //-----------------------------------------------------------------------------------------
        // Returns pointer to bmtMDMethod. IsMDMethod is required to return true before
        // calling this method.
        bmtMDMethod *
        AsMDMethod() const
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsMDMethod());
            return (bmtMDMethod *) Decode(m_handle);
        }

        //-----------------------------------------------------------------------------------------
        // Comparison operator. Returns true if handles point to the same object; returns
        // false otherwise.
        bool
        operator==(
            const bmtMethodHandle &rhs) const;

        bool operator !=(const bmtMethodHandle &rhs) const { return !((*this) == rhs); }

        //-----------------------------------------------------------------------------------------
        // Returns the owning type.
        bmtTypeHandle
        GetOwningType() const;

        //-----------------------------------------------------------------------------------------
        // Returns the metadata declaration attributes for this method.
        DWORD
        GetDeclAttrs() const;

        //-----------------------------------------------------------------------------------------
        // Returns the metadata implementation attributes for this method.
        DWORD
        GetImplAttrs() const;

        //-----------------------------------------------------------------------------------------
        // Returns the slot that this method is assigned to.
        SLOT_INDEX
        GetSlotIndex() const;

        //-----------------------------------------------------------------------------------------
        // Returns a reference to the MethodSignature for this method.
        const MethodSignature &
        GetMethodSignature() const;

        //-----------------------------------------------------------------------------------------
        // Returns the MethodDesc* associated with this method.
        MethodDesc *
        GetMethodDesc() const;

    protected:
        //-----------------------------------------------------------------------------------------
        static const UINT_PTR RTMETHOD_FLAG = 0x1;
        static const UINT_PTR MDMETHOD_FLAG = 0x2;
        static const UINT_PTR MASK_FLAG     = 0x3;

        //-----------------------------------------------------------------------------------------
        // Takes a pointer and encodes it with the flag.
        static UINT_PTR
        Encode(
            LPVOID   pv,
            UINT_PTR flag)
        {
            LIMITED_METHOD_CONTRACT;
            CONSISTENCY_CHECK((reinterpret_cast<UINT_PTR>(pv) & MASK_FLAG) == 0);
            return (reinterpret_cast<UINT_PTR>(pv) | flag);
        }

        //-----------------------------------------------------------------------------------------
        // Takes an encoded handle and removes encoding bits.
        static LPVOID
        Decode(
            UINT_PTR handle)
            { LIMITED_METHOD_CONTRACT; return reinterpret_cast<LPVOID>(handle & ~MASK_FLAG); }

        //-----------------------------------------------------------------------------------------
        // Uses encode to produce a handle for a bmtRTMethod*
        static UINT_PTR
        HandleFromRTMethod(
            bmtRTMethod * pRTMethod)
            { WRAPPER_NO_CONTRACT; return Encode(pRTMethod, RTMETHOD_FLAG); }

        //-----------------------------------------------------------------------------------------
        // Uses encode to produce a handle for a bmtMDMethod*
        static UINT_PTR
        HandleFromMDMethod(
            bmtMDMethod * pMDMethod)
            { WRAPPER_NO_CONTRACT; return Encode(pMDMethod, MDMETHOD_FLAG); }

        //-----------------------------------------------------------------------------------------
        // This is the value of the encoded pointer.
        UINT_PTR m_handle;

#ifdef _DEBUG
        //-----------------------------------------------------------------------------------------
        // Used in debug builds to quickly access the type in a debugger.
        union
        {
            bmtRTMethod * m_pAsRTMethod;
            bmtMDMethod * m_pAsMDMethod;
        };
#endif
    };  // class bmtMethodHandle

    // --------------------------------------------------------------------------------------------
    // Represents a method slot. It has a declaration and implementation value because these can
    // differ if the slot has been modified with a methodImpl. Otherwise, these two values are
    // typically identical.
    class bmtMethodSlot
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Constructor for an empty slot. Both handles default to null.
        bmtMethodSlot()
            : m_decl(),
              m_impl()
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Constructor with both values explicitly provided. Either use this constructor or assign
        // to each value individually using non-const Decl and Impl methods.
        bmtMethodSlot(
            const bmtMethodHandle & decl,
            const bmtMethodHandle & impl)
            : m_decl(decl),
              m_impl(impl)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Copy constructor.
        bmtMethodSlot(
            const bmtMethodSlot & other)
            : m_decl(other.m_decl),
              m_impl(other.m_impl)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to the declaration method for this slot. This can be used as a
        // getter or a setter.
        bmtMethodHandle &
        Decl()
            { LIMITED_METHOD_CONTRACT; return m_decl; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to the implementation method for this slot. This can be used as a
        // getter or a setter.
        bmtMethodHandle &
        Impl()
            { LIMITED_METHOD_CONTRACT; return m_impl; }

        //-----------------------------------------------------------------------------------------
        // Const version of Decl.
        const bmtMethodHandle &
        Decl() const
            { LIMITED_METHOD_CONTRACT; return m_decl; }

        //-----------------------------------------------------------------------------------------
        // Const version of Impl.
        const bmtMethodHandle &
        Impl() const
            { LIMITED_METHOD_CONTRACT; return m_impl; }

    private:
        bmtMethodHandle     m_decl;
        bmtMethodHandle     m_impl;
    };  // class bmtMethodSlot

    // --------------------------------------------------------------------------------------------
    struct bmtProperties
    {
        bool fIsValueClass;
        bool fIsEnum;
        bool fNoSanityChecks;
        bool fSparse;                           // Set to true if a sparse interface is being used.
        bool fHasVirtualStaticMethods;          // Set to true if the interface type declares virtual static methods.

        // Com Interop, ComWrapper classes extend from ComObject
        bool fIsComObjectType;                  // whether this class is an instance of ComObject class
#ifdef FEATURE_COMINTEROP
        bool fIsMngStandardItf;                 // Set to true if the interface is a manages standard interface.
        bool fComEventItfType;                  // Set to true if the class is a special COM event interface.
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_TYPEEQUIVALENCE
        bool fHasTypeEquivalence;               // Set to true if the class is decorated by TypeIdentifierAttribute, or through some other technique is influenced by type equivalence
        bool fIsTypeEquivalent;                 // Set to true if the class is decorated by TypeIdentifierAttribute
#endif

        bool fDynamicStatics;                   // Set to true if the statics will be allocated in the dynamic
        bool fGenericsStatics;                  // Set to true if the there are per-instantiation statics

        bool fIsIntrinsicType;                  // Set to true if the type has an [Intrinsic] attribute on it
        bool fIsHardwareIntrinsic;              // Set to true if the class is a hardware intrinsic

        DWORD dwNonGCRegularStaticFieldBytes;
        DWORD dwNonGCThreadStaticFieldBytes;

        inline bmtProperties() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtProperties

    // --------------------------------------------------------------------------------------------
    // Holds an array of bmtMethodSlot values.
    class bmtMethodSlotTable
    {
      public:
        //-----------------------------------------------------------------------------------------
        // Create a table that can hold maxSlotIdx slots. All slots up to maxSlotIdx is initialized
        // with the default constructor.
        bmtMethodSlotTable(
            SLOT_INDEX maxSlotIdx,
            StackingAllocator * pStackingAllocator)
            : m_curSlotIdx(0),
              m_maxSlotIdx(maxSlotIdx),
              m_rgSlots(new (pStackingAllocator) bmtMethodSlot[maxSlotIdx])
            { CONTRACTL { THROWS; } CONTRACTL_END; }

        //-----------------------------------------------------------------------------------------
        // Subscript operator
        template <typename INDEX_TYPE>
        bmtMethodSlot & operator[](INDEX_TYPE idx) const
            { WRAPPER_NO_CONTRACT; ValidateIdx(idx); return m_rgSlots[idx]; }

        //-----------------------------------------------------------------------------------------
        // Pushes the value of slot to the end of the array.
        bool
        AddMethodSlot(const bmtMethodSlot & slot)
        {
            LIMITED_METHOD_CONTRACT;
            CONSISTENCY_CHECK(m_curSlotIdx <= m_maxSlotIdx);
            if (m_curSlotIdx == m_maxSlotIdx)
                return false;
            (*this)[m_curSlotIdx++] = slot;
            return true;
        }

        //-----------------------------------------------------------------------------------------
        // The current size of the used entries in the array.
        SLOT_INDEX
        GetSlotCount()
            { LIMITED_METHOD_CONTRACT; return m_curSlotIdx; }

        //-----------------------------------------------------------------------------------------
        // Used to iterate the contents of the array.
        typedef IteratorUtil::ArrayIterator<bmtMethodSlot> Iterator;

        Iterator
        IterateSlots()
            { return Iterator(m_rgSlots, GetSlotCount()); }

      private:
        //-----------------------------------------------------------------------------------------
        SLOT_INDEX      m_curSlotIdx;
        SLOT_INDEX      m_maxSlotIdx;
        bmtMethodSlot * m_rgSlots;

        template <typename INDEX_TYPE>
        void
        ValidateIdx(
            INDEX_TYPE idx) const
            { CONSISTENCY_CHECK(idx < m_curSlotIdx); }
    };  // class bmtMethodSlotTable

    // --------------------------------------------------------------------------------------------
    struct bmtParentInfo;

    // --------------------------------------------------------------------------------------------
    // This type is used in creating the slot layout that will be used in the MethodTable.
    struct bmtVtable
    {
#ifdef _DEBUG
        //-----------------------------------------------------------------------------------------
        // Used to make sure no virtual methods are added to the vtable after non-virtuals have
        // begun to be added.
        bool m_fIsVirtualSlotSectionSealed;

        bool
        IsVirtualSlotSectionSealed() const
            { LIMITED_METHOD_CONTRACT; return m_fIsVirtualSlotSectionSealed; }

        void
        SealVirtualSlotSection()
            { LIMITED_METHOD_CONTRACT; m_fIsVirtualSlotSectionSealed = true; }
#endif

        //-----------------------------------------------------------------------------------------
        // Implemented using a bmtMethodSlotTable
        bmtMethodSlotTable * pSlotTable;

        // Used to keep track of the default and static type constructors.
        bmtMDMethod * pDefaultCtor;
        bmtMDMethod * pCCtor;

        // Upper bound on size of vtable. Used in initializing pSlotTable
        DWORD dwMaxVtableSize;

        // Used to keep track of how many virtual and total slots are in the vtable
        SLOT_INDEX cVirtualSlots;
        SLOT_INDEX cTotalSlots;

        // Number of slots allocated in Vtable
        SLOT_INDEX cVtableSlots;

        // The dispatch map builder for this type.
        //@TODO: This should be moved.
        DispatchMapBuilder *pDispatchMapBuilder;

        //-----------------------------------------------------------------------------------------
        // Appends this method to the vtable as a newslot virtual. Decl and Impl are both set to be
        // the value of pMethod.
        bool
        AddVirtualMethod(bmtMDMethod * pMethod)
        {
            CONSISTENCY_CHECK(!IsVirtualSlotSectionSealed());
            pMethod->SetSlotIndex(pSlotTable->GetSlotCount());
            if (!pSlotTable->AddMethodSlot(bmtMethodSlot(pMethod, pMethod)))
                return false;
            ++cVirtualSlots;
            ++cTotalSlots;
            return true;
        }

        //-----------------------------------------------------------------------------------------
        // Overwrites an existing slot's Decl and Impl values that of pMethod.
        void
        SetVirtualMethodOverride(SLOT_INDEX idx, bmtMDMethod * pMethod)
        {
            CONSISTENCY_CHECK(!IsVirtualSlotSectionSealed());
            pMethod->SetSlotIndex(idx);
            (*pSlotTable)[idx] = bmtMethodSlot(pMethod, pMethod);
        }

        //-----------------------------------------------------------------------------------------
        // Overwrites an existing slot's Impl value (but *NOT* Decl) that of pMethod.
        void
        SetVirtualMethodImpl(SLOT_INDEX idx, bmtMDMethod * pImplMethod)
        {
            LIMITED_METHOD_CONTRACT;
            (*pSlotTable)[idx] = bmtMethodSlot((*pSlotTable)[idx].Decl(), pImplMethod);
        }

        //-----------------------------------------------------------------------------------------
        // Appends this method to the vtable as a newslot non-virtual. Decl and Impl are both set to be
        // the value of pMethod.
        bool
        AddNonVirtualMethod(bmtMDMethod * pMethod)
        {
            INDEBUG(SealVirtualSlotSection());
            CONSISTENCY_CHECK(!IsMdVirtual(pMethod->GetDeclAttrs()) || IsMdStatic(pMethod->GetDeclAttrs()));
            pMethod->SetSlotIndex(pSlotTable->GetSlotCount());
            if (!pSlotTable->AddMethodSlot(bmtMethodSlot(pMethod, pMethod)))
                return false;
            ++cTotalSlots;
            return true;
        }

        //-----------------------------------------------------------------------------------------
        // Adds this method as an unboxed entrypoint to the vtable as a newslot non-virtual.
        bool
        AddUnboxedMethod(bmtMDMethod * pMethod)
        {
            INDEBUG(SealVirtualSlotSection());
            CONSISTENCY_CHECK(IsMdVirtual(pMethod->GetDeclAttrs()));
            pMethod->SetUnboxedSlotIndex(pSlotTable->GetSlotCount());
            if (!pSlotTable->AddMethodSlot(bmtMethodSlot(pMethod, pMethod)))
                return false;
            ++cTotalSlots;
            return true;
        }

        //-----------------------------------------------------------------------------------------
        // If a default constructor has been set, this returns the slot assigned to the method;
        // otherwise returns INVALID_SLOT_INDEX.
        SLOT_INDEX
        GetDefaultCtorSlotIndex() const
        {
            if (pDefaultCtor != NULL)
            {
                return pDefaultCtor->GetSlotIndex();
            }
            else
        {
                return INVALID_SLOT_INDEX;
            }
        }

        //-----------------------------------------------------------------------------------------
        // If a static type constructor has been set, this returns the slot assigned to the method;
        // otherwise returns INVALID_SLOT_INDEX.
        SLOT_INDEX
        GetClassCtorSlotIndex() const
        {
            if (pCCtor != NULL)
            {
                return pCCtor->GetSlotIndex();
            }
            else
        {
                return INVALID_SLOT_INDEX;
            }
        }

        //-----------------------------------------------------------------------------------------
        // Subscript operator
        bmtMethodSlot & operator[](SLOT_INDEX idx) const
            { WRAPPER_NO_CONTRACT; return (*pSlotTable)[idx]; }

        //-----------------------------------------------------------------------------------------
        inline bmtVtable()
            : INDEBUG_COMMA(m_fIsVirtualSlotSectionSealed(false))
              pSlotTable(NULL),
              pDefaultCtor(NULL),
              pCCtor(NULL),
              dwMaxVtableSize(0),
              cVirtualSlots(0),
              cTotalSlots(0),
              pDispatchMapBuilder(NULL)
            { LIMITED_METHOD_CONTRACT; }

        typedef bmtMethodSlotTable::Iterator Iterator;

        Iterator
        IterateSlots()
            { return pSlotTable->IterateSlots(); }
    };  // struct bmtVtable

    // --------------------------------------------------------------------------------------------
    typedef FixedCapacityStackingAllocatedUTF8StringHash<bmtRTMethod *> MethodNameHash;

    // --------------------------------------------------------------------------------------------
    struct bmtParentInfo
    {
        bmtMethodSlotTable *pSlotTable;

        typedef bmtMethodSlotTable::Iterator Iterator;

        //-----------------------------------------------------------------------------------------
        // Iterate the slots of the parent type.
        Iterator
        IterateSlots()
            { return pSlotTable->IterateSlots(); }

        //-----------------------------------------------------------------------------------------
        // Subscript operator
        bmtMethodSlot & operator[](SLOT_INDEX idx) const
            { WRAPPER_NO_CONTRACT; return (*pSlotTable)[idx]; }

        DWORD NumParentPointerSeries;
        MethodNameHash *pParentMethodHash;

        inline bmtParentInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtParentInfo

    // --------------------------------------------------------------------------------------------
    // This will create bmtMethodSlotTable fully describing the vtable of the parent type. This
    // currently includes both virtual and non-virtual, though for the purposes of building the
    // current type virtual methods are the only ones that are necessary and so we could remove
    // the non-virtual method importing if it proves to be a performance bottleneck.
    void
    ImportParentMethods();

    // --------------------------------------------------------------------------------------------
    // Copies the virtual slots from the parent into the current type's vtable, effectively
    // performing the virtual method inheritance step of type layout.
    void
    CopyParentVtable();

    // --------------------------------------------------------------------------------------------
    // The ECMA spec declares that interfaces get placed differently depending on how they
    // are declared (see comment before the implementation of PlaceInterfaceMethods for details).
    // This is used to keep track of the declaration conditions as interfaces are expanded.
    struct InterfaceDeclarationScope
    {
        //-----------------------------------------------------------------------------------------
        // States that the interface has been declared by a parent.
        bool fIsInterfaceDeclaredOnParent;

        //-----------------------------------------------------------------------------------------
        // States that the interface has been explicitly declared in the interface implementation
        // list of this type.
        bool fIsInterfaceDeclaredOnType;

        // If both of the above members are FALSE, then the interface was not declared by a
        // parent and was not explicitly declared in the interface implementation list, but it
        // was declared transitively through one of the interfaces appearing in the implementation
        // list.

        //-----------------------------------------------------------------------------------------
        InterfaceDeclarationScope(
            bool fIsInterfaceDeclaredOnParent,
            bool fIsInterfaceDeclaredOnType)
        {
            this->fIsInterfaceDeclaredOnParent = fIsInterfaceDeclaredOnParent;
            this->fIsInterfaceDeclaredOnType = fIsInterfaceDeclaredOnType;
        }
    };  // struct InterfaceDeclarationScope

    // --------------------------------------------------------------------------------------------
    // This type contains information about the implementation of a particular interface slot.
    class bmtInterfaceSlotImpl
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Default constructor.
        bmtInterfaceSlotImpl()
            : m_decl(),
              m_implSlotIndex(INVALID_SLOT_INDEX)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Constructor.
        bmtInterfaceSlotImpl(
            const bmtMethodHandle & decl,
            SLOT_INDEX              implSlotIndex)
            : m_decl(decl),
              m_implSlotIndex(implSlotIndex)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Copy constructor
        bmtInterfaceSlotImpl(
            const bmtInterfaceSlotImpl & other)
            : m_decl(other.m_decl),
              m_implSlotIndex(other.m_implSlotIndex)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Returns a mutable reference to the decl of the slot.
        bmtMethodHandle &
        Decl()
            { LIMITED_METHOD_CONTRACT; return m_decl; }

        //-----------------------------------------------------------------------------------------
        // Returns a mutable reference to the slot index for the impl of the slot.
        SLOT_INDEX &
        Impl()
            { LIMITED_METHOD_CONTRACT; return m_implSlotIndex; }

        //-----------------------------------------------------------------------------------------
        // Returns a constant reference to the decl of the slot.
        const bmtMethodHandle &
        Decl() const
            { LIMITED_METHOD_CONTRACT; return m_decl; }

        //-----------------------------------------------------------------------------------------
        // Returns a constant reference to the slot index for the impl of the slot.
        const SLOT_INDEX &
        Impl() const
            { LIMITED_METHOD_CONTRACT; return m_implSlotIndex; }

    private:
        bmtMethodHandle     m_decl;
        SLOT_INDEX          m_implSlotIndex;
    };  // class bmtInterfaceSlotImpl

    // --------------------------------------------------------------------------------------------
    // This type contains information about the implementation of an interface by the type that
    // is being built. It includes the declaration context in the form of an
    // InterfaceDeclarationScope (see comments on type for explanation) as well as an array of
    // bmtInterfaceSlotImpl values, with the number of entries corresponding to the number of
    // virtual methods declared on the interface. The slots are constructed with default values
    // which are interpreted as meaning that the slot has no implementation. Only when an
    // implementation is found for a slot is the slot updated. Note that this does not include
    // overrides for methods in slots that already contributed to this interface's implementation,
    // which can happen when an interface implementation is inherited.
    class bmtInterfaceEntry
    {
    public:
        //-----------------------------------------------------------------------------------------
        // Constructor. A default constructor would not be appropriate.
        bmtInterfaceEntry(
            bmtRTType *                         pItfType,
            const InterfaceDeclarationScope &   declScope)
            : m_pType(pItfType),
              m_pImplTable(NULL),       // Lazily created
              m_cImplTable(0),
              m_declScope(declScope),
              m_equivalenceSet(0),
              m_fEquivalenceSetWithMultipleEntries(false)
            { LIMITED_METHOD_CONTRACT; }

        //-----------------------------------------------------------------------------------------
        // Returns the bmtRTType for the interface type.
        bmtRTType *
        GetInterfaceType() const
            { LIMITED_METHOD_CONTRACT; return m_pType; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to a bool. The value is true if the interface is explicitly
        // declared within the type's interface list; false otherwise.
        bool &
        IsDeclaredOnType()
            { LIMITED_METHOD_CONTRACT; return m_declScope.fIsInterfaceDeclaredOnType; }

        //-----------------------------------------------------------------------------------------
        // const version
        const bool &
        IsDeclaredOnType() const
            { LIMITED_METHOD_CONTRACT; return m_declScope.fIsInterfaceDeclaredOnType; }

        //-----------------------------------------------------------------------------------------
        // Returns a reference to a bool. The value is true if the interface is implemented
        // by a parent type; false otherwise. Const version only because this does not need to
        // be changed in a dynamic fashion.
        const bool &
        IsImplementedByParent()
            { LIMITED_METHOD_CONTRACT; return m_declScope.fIsInterfaceDeclaredOnParent; }

        //-----------------------------------------------------------------------------------------
        // Used to iterate the interface implementation slots.
        typedef IteratorUtil::ArrayIterator<bmtInterfaceSlotImpl>
            InterfaceSlotIterator;

        InterfaceSlotIterator
        IterateInterfaceSlots(
            StackingAllocator * pStackingAllocator)
        {
            WRAPPER_NO_CONTRACT;
            CheckCreateSlotTable(pStackingAllocator);
            return InterfaceSlotIterator(m_pImplTable, m_cImplTable);
        }

        //-----------------------------------------------------------------------------------------
        // Returns the number of interface implementation slots.
        SLOT_INDEX
        GetInterfaceSlotImplCount()
            { LIMITED_METHOD_CONTRACT; return m_cImplTable; }

        //-----------------------------------------------------------------------------------------
        // Subscript operator.
        bmtInterfaceSlotImpl &
        operator[](
            SLOT_INDEX idx)
        {
            LIMITED_METHOD_CONTRACT;
            CONSISTENCY_CHECK(CheckPointer(m_pImplTable));
            CONSISTENCY_CHECK(idx < m_cImplTable);
            return m_pImplTable[idx];
        }

        //-----------------------------------------------------------------------------------------
        const bmtInterfaceSlotImpl &
        operator[](
            SLOT_INDEX idx) const
        {
            return (*const_cast<bmtInterfaceEntry *>(this))[idx];
        }

        //-----------------------------------------------------------------------------------------
        void SetInterfaceEquivalenceSet(UINT32 iEquivalenceSet, bool fEquivalenceSetWithMultipleEntries)
        {
            LIMITED_METHOD_CONTRACT;
            // The equivalence set of 0 indicates the value has not yet been calculated
            // We should set the equivalence set to only one value
            _ASSERTE((m_equivalenceSet == 0) || (m_equivalenceSet == iEquivalenceSet));
            m_equivalenceSet = iEquivalenceSet;
            m_fEquivalenceSetWithMultipleEntries = fEquivalenceSetWithMultipleEntries;
        }

        UINT32 GetInterfaceEquivalenceSet()
        {
            LIMITED_METHOD_CONTRACT;
            // The equivalence set of 0 indicates the value has not yet been calculated.
            // We should not be calling this method before calculating equivalence sets
            _ASSERTE(m_equivalenceSet != 0);
            return m_equivalenceSet;
        }

        bool InEquivalenceSetWithMultipleEntries()
        {
            LIMITED_METHOD_CONTRACT;
            // The equivalence set of 0 indicates the value has not yet been calculated.
            // We should not be calling this method before calculating equivalence sets
            _ASSERTE(m_equivalenceSet != 0);
            return m_fEquivalenceSetWithMultipleEntries;
        }

    private:
        //-----------------------------------------------------------------------------------------
        void
        CheckCreateSlotTable(
            StackingAllocator * pStackingAllocator)
        {
            LIMITED_METHOD_CONTRACT;
            if (m_pImplTable == NULL)
            {
                CreateSlotTable(pStackingAllocator);
            }
        }

        //-----------------------------------------------------------------------------------------
        // This creates the interface slot implementation table and correctly creates interface
        // methods and sets them in the Decl property for each slot.
        void
        CreateSlotTable(
            StackingAllocator * pStackingAllocator);

        //-----------------------------------------------------------------------------------------
        bmtRTType *               m_pType;
        bmtInterfaceSlotImpl *    m_pImplTable;
        SLOT_INDEX                m_cImplTable;
        InterfaceDeclarationScope m_declScope;
        UINT32                    m_equivalenceSet;
        bool                      m_fEquivalenceSetWithMultipleEntries;
    };  // class bmtInterfaceEntry

    // --------------------------------------------------------------------------------------------
    // Contains the list of implemented interfaces as an array of bmtInterfaceEntry values.
    struct bmtInterfaceInfo
    {
        bmtInterfaceEntry * pInterfaceMap;
        DWORD dwInterfaceMapSize;               // count of entries in interface map
        DWORD dwInterfaceMapAllocated;          // upper bound on size of interface map
#ifdef _DEBUG
        // Should we inject interface duplicates for this type? (Parent has its own value stored in
        // code:MethodTable::dbg_m_fHasInjectedInterfaceDuplicates)
        BOOL dbg_fShouldInjectInterfaceDuplicates;
#endif //_DEBUG

        //-----------------------------------------------------------------------------------------
        // Used to iterate the interface entries in the map.
        typedef IteratorUtil::ArrayIterator<bmtInterfaceEntry> MapIterator;

        MapIterator
        IterateInterfaceMap()
            { return MapIterator(pInterfaceMap, dwInterfaceMapSize); }

        //-----------------------------------------------------------------------------------------
        // Constructor
        inline bmtInterfaceInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtInterfaceInfo

    // --------------------------------------------------------------------------------------------
    // Contains information on fields derived from the metadata of the type.
    struct bmtEnumFieldInfo
    {
        // Counts instance fields
        DWORD dwNumInstanceFields;

        // Counts both regular statics and thread statics. Currently RVA
        // get lumped in with "regular statics".
        DWORD dwNumStaticFields;
        DWORD dwNumStaticObjRefFields;
        DWORD dwNumStaticBoxedFields;

        // We keep a separate count for just thread statics
        DWORD dwNumThreadStaticFields;
        DWORD dwNumThreadStaticObjRefFields;
        DWORD dwNumThreadStaticBoxedFields;

        DWORD dwNumDeclaredFields;           // For calculating amount of FieldDesc's to allocate

        IMDInternalImport *m_pInternalImport;

        //-----------------------------------------------------------------------------------------
        inline bmtEnumFieldInfo(IMDInternalImport *pInternalImport)
        {
            LIMITED_METHOD_CONTRACT;
            memset((void *)this, NULL, sizeof(*this));
            m_pInternalImport = pInternalImport;
        }
    };  // struct bmtEnumFieldInfo

    // --------------------------------------------------------------------------------------------
    // This contains information specifically about the methods declared by the type being built.
    struct bmtMethodInfo
    {
        //-----------------------------------------------------------------------------------------
        // The array and bounds of the bmtMDMethod array
        SLOT_INDEX      m_cDeclaredMethods;
        SLOT_INDEX      m_cMaxDeclaredMethods;
        bmtMDMethod **  m_rgDeclaredMethods;

        //-----------------------------------------------------------------------------------------
        DWORD           dwNumDeclaredNonAbstractMethods; // For calculating approx generic dictionary size
        DWORD           dwNumberMethodImpls;    // Number of method impls defined for this type
        DWORD           dwNumberInexactMethodImplCandidates; // Number of inexact method impl candidates (used for type equivalent interfaces)

        //-----------------------------------------------------------------------------------------
        // Constructor
        inline bmtMethodInfo()
            { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }

        //-----------------------------------------------------------------------------------------
        // Add a declared method to the array
        void
        AddDeclaredMethod(
            bmtMDMethod * pMethod)
        {
            CONSISTENCY_CHECK(m_cDeclaredMethods < m_cMaxDeclaredMethods);
            m_rgDeclaredMethods[m_cDeclaredMethods++] = pMethod;
        }

        //-----------------------------------------------------------------------------------------
        // Subscript operator
        bmtMDMethod *
        operator[](
            SLOT_INDEX idx) const
        {
            CONSISTENCY_CHECK(idx < m_cDeclaredMethods);
            return m_rgDeclaredMethods[idx];
        }

        //-----------------------------------------------------------------------------------------
        // Returns the number of declared methods.
        SLOT_INDEX
        GetDeclaredMethodCount()
            { LIMITED_METHOD_CONTRACT; return m_cDeclaredMethods; }

        //-----------------------------------------------------------------------------------------
        // Searches the declared methods for a method with a token value equal to tok.
        bmtMDMethod *
        FindDeclaredMethodByToken(
            mdMethodDef tok)
        {
            LIMITED_METHOD_CONTRACT;
            for (SLOT_INDEX i = 0; i < m_cDeclaredMethods; ++i)
            {
                if ((*this)[i]->GetMethodSignature().GetToken() == tok)
                {
                    return (*this)[i];
                }
            }
            return NULL;
        }
    };  // struct bmtMethodInfo

    // --------------------------------------------------------------------------------------------
    // Stores metadata info for a
    struct bmtMetaDataInfo
    {
        //-----------------------------------------------------------------------------------------
        DWORD    cFields;                   // # meta-data fields of this class
        mdToken *pFields;                   // Enumeration of metadata fields
        DWORD   *pFieldAttrs;               // Enumeration of the attributes of the fields

        //-----------------------------------------------------------------------------------------
        // Stores the method impl tokens as a pair structure to enable qsort to be
        // performed on the array.
        struct MethodImplTokenPair
        {
            mdToken methodBody;             // MethodDef's for the bodies of MethodImpls. Must be defined in this type.
            mdToken methodDecl;             // Method token that body implements. Is a MethodDef or MemberRef
            // Does this methodimpl need to be considered during inexact methodimpl processing
            bool    fConsiderDuringInexactMethodImplProcessing;
            // If when considered during inexact methodimpl processing it does not match any declaration method, throw.
            // This is to detect situations where a methodimpl does not match any method on any equivalent interface.
            bool    fThrowIfUnmatchedDuringInexactMethodImplProcessing;
            UINT32  interfaceEquivalenceSet;// Equivalence set in the interface map to examine
            bool    fRequiresCovariantReturnTypeChecking;
            static int __cdecl Compare(const void *elem1, const void *elem2);
            static BOOL Equal(const MethodImplTokenPair *elem1, const MethodImplTokenPair *elem2);
        };

        //-----------------------------------------------------------------------------------------
        MethodImplTokenPair *rgMethodImplTokens;
        Substitution *pMethodDeclSubsts;    // Used to interpret generic variables in the interface of the declaring type

        bool fHasCovariantOverride;

        //-----------------------------------------------------------------------------------------
        inline bmtMetaDataInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtMetaDataInfo

    // --------------------------------------------------------------------------------------------
    // Stores a bunch of random info related to method and field descs. This should be separated
    // into appropriate data structures.
    struct bmtMethAndFieldDescs
    {
        //-----------------------------------------------------------------------------------------
        FieldDesc **ppFieldDescList;        // FieldDesc pointer (or NULL if field not preserved) for each field


        //-----------------------------------------------------------------------------------------
        inline bmtMethAndFieldDescs() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtMethAndFieldDescs

    // --------------------------------------------------------------------------------------------
    // Information about the placement of fields during field layout.
    struct bmtFieldPlacement
    {
        // For compacting field placement
        DWORD InstanceFieldStart[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];

        DWORD NumInstanceFieldsOfSize[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];
        DWORD FirstInstanceFieldOfSize[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];
        DWORD GCPointerFieldStart;
        DWORD NumInstanceGCPointerFields;   // does not include inherited pointer fields
        DWORD NumGCPointerSeries;
        DWORD NumInstanceFieldBytes;

        bool  fIsByRefLikeType;
        bool  fHasFixedAddressValueTypes;
        bool  fHasSelfReferencingStaticValueTypeField_WithRVA;

        // These data members are specific to regular statics
        DWORD RegularStaticFieldStart[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];            // Byte offset where to start placing fields of this size
        DWORD NumRegularStaticFieldsOfSize[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];       // # Fields of this size
        DWORD NumRegularStaticGCPointerFields;   // does not include inherited pointer fields
        DWORD NumRegularStaticGCBoxedFields;   // does not include inherited pointer fields

        // These data members are specific to thread statics
        DWORD ThreadStaticFieldStart[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];            // Byte offset where to start placing fields of this size
        DWORD NumThreadStaticFieldsOfSize[MAX_LOG2_PRIMITIVE_FIELD_SIZE+1];       // # Fields of this size
        DWORD NumThreadStaticGCPointerFields;   // does not include inherited pointer fields
        DWORD NumThreadStaticGCBoxedFields;   // does not include inherited pointer fields

        inline bmtFieldPlacement() { LIMITED_METHOD_CONTRACT; memset((void *)this, 0, sizeof(*this)); }
    };  // struct bmtFieldPlacement

    // --------------------------------------------------------------------------------------------
    // Miscelaneous information about the type being built.
    struct bmtInternalInfo
    {
        //-----------------------------------------------------------------------------------------
        // Metadata for accessing information on the type
        IMDInternalImport *pInternalImport;
        Module *pModule;

        //-----------------------------------------------------------------------------------------
        // Parent method table. It is identical to pType->GetParentType()->GetMethodTable(),
        // except for EnC. pParentMT is initialized but pType is not when InitializeFieldDesc
        // is directly called by EnC.
        MethodTable * pParentMT;

        //-----------------------------------------------------------------------------------------
        // The representation of the type being built
        bmtMDType * pType;

        //-----------------------------------------------------------------------------------------
        // Constructor
        inline bmtInternalInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtInternalInfo


    // --------------------------------------------------------------------------------------------
    // Used for analyzing overlapped fields defined by explicit layout types.
    enum bmtFieldLayoutTag {empty, nonoref, oref};

    // --------------------------------------------------------------------------------------------
    // used for calculating pointer series for tdexplicit
    struct bmtGCSeriesInfo
    {
        UINT numSeries;
        struct Series {
            UINT offset;
            UINT len;
        } *pSeries;
        bmtGCSeriesInfo() : numSeries(0), pSeries(NULL) {LIMITED_METHOD_CONTRACT;}
    };  // struct bmtGCSeriesInfo

    // --------------------------------------------------------------------------------------------
    struct bmtMethodImplInfo
    {
        //-----------------------------------------------------------------------------------------
        // This struct represents the resolved methodimpl pair.
        struct Entry
        {
            bmtMethodHandle declMethod;
            bmtMDMethod *   pImplMethod;
            mdToken         declToken;

            Entry(bmtMDMethod *   pImplMethodIn,
                  bmtMethodHandle declMethodIn,
                  mdToken declToken)
              : declMethod(declMethodIn),
                pImplMethod(pImplMethodIn),
                declToken(declToken)
              {}

            Entry()
              : declMethod(),
                pImplMethod(NULL),
                declToken()
              {}
        };

        //-----------------------------------------------------------------------------------------
        // The allocated array of entries and the count indicating how many entries are in use.
    private:
        Entry *rgEntries;
        DWORD        cMaxIndex;

        //-----------------------------------------------------------------------------------------
        // Returns the MethodDesc* for the implementation of the methodimpl pair.
        MethodDesc*
        GetBodyMethodDesc(
            DWORD i)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(i < pIndex);
            return GetImplementationMethod(i)->GetMethodDesc();
        }

    public:

        DWORD        pIndex;     // Next open spot in array, we load the BodyDesc's up in order of
                                 // appearance in the type's list of methods (a body can appear
                                 // more then once in the list of MethodImpls)


        //-----------------------------------------------------------------------------------------
        // Add a methodimpl to the list.
        void
        AddMethodImpl(
            bmtMDMethod * pImplMethod,
            bmtMethodHandle declMethod,
            mdToken declToken,
            StackingAllocator * pStackingAllocator);

        //-----------------------------------------------------------------------------------------
        // Get the decl method for a particular methodimpl entry.
        bmtMethodHandle
        GetDeclarationMethod(
            DWORD i)
            { LIMITED_METHOD_CONTRACT; _ASSERTE(i < pIndex); return rgEntries[i].declMethod; }

        //-----------------------------------------------------------------------------------------
        // Get the decl method for a particular methodimpl entry.
        mdToken
        GetDeclarationToken(
            DWORD i)
            { LIMITED_METHOD_CONTRACT; _ASSERTE(i < pIndex); return rgEntries[i].declToken; }

        //-----------------------------------------------------------------------------------------
        // Get the impl method for a particular methodimpl entry.
        bmtMDMethod *
        GetImplementationMethod(
            DWORD i)
            { LIMITED_METHOD_CONTRACT; _ASSERTE(i < pIndex); return rgEntries[i].pImplMethod; }

        //-----------------------------------------------------------------------------------------
        // Constructor
        inline bmtMethodImplInfo()
            { LIMITED_METHOD_CONTRACT; memset((void*) this, NULL, sizeof(*this)); }

        //-----------------------------------------------------------------------------------------
        // Returns TRUE if tok acts as a body for any methodImpl entry. FALSE, otherwise.
        BOOL IsBody(
            mdToken tok);
    };  // struct bmtMethodImplInfo

    // --------------------------------------------------------------------------------------------
    // These are all the memory allocators available to MethodTableBuilder

    StackingAllocator * m_pStackingAllocator;
    AllocMemTracker *   m_pAllocMemTracker;

    StackingAllocator *
    GetStackingAllocator()
        { LIMITED_METHOD_CONTRACT; return m_pStackingAllocator; }

    LoaderAllocator *
    GetLoaderAllocator()
        { LIMITED_METHOD_CONTRACT; return bmtAllocator; }

    AllocMemTracker *
    GetMemTracker()
        { LIMITED_METHOD_CONTRACT; return m_pAllocMemTracker; }

    BYTE *
    AllocateFromHighFrequencyHeap(S_SIZE_T cbMem);

    BYTE *
    AllocateFromLowFrequencyHeap(S_SIZE_T cbMem);

    // --------------------------------------------------------------------------------------------
    // The following structs, defined as private members of MethodTableBuilder, contain the necessary local
    // parameters needed for BuildMethodTable

    // Look at the struct definitions for a detailed list of all parameters available
    // to BuildMethodTable.

    LoaderAllocator *bmtAllocator;
    bmtErrorInfo *bmtError;
    bmtProperties *bmtProp;
    bmtVtable *bmtVT;
    bmtParentInfo *bmtParent;
    bmtInterfaceInfo *bmtInterface;
    bmtMetaDataInfo *bmtMetaData;
    bmtMethodInfo *bmtMethod;
    bmtMethAndFieldDescs *bmtMFDescs;
    bmtFieldPlacement *bmtFP;
    bmtInternalInfo *bmtInternal;
    bmtGCSeriesInfo *bmtGCSeries;
    bmtMethodImplInfo *bmtMethodImpl;
    const bmtGenericsInfo *bmtGenerics;
    bmtEnumFieldInfo *bmtEnumFields;

    void SetBMTData(
        LoaderAllocator *bmtAllocator,
        bmtErrorInfo *bmtError,
        bmtProperties *bmtProp,
        bmtVtable *bmtVT,
        bmtParentInfo *bmtParent,
        bmtInterfaceInfo *bmtInterface,
        bmtMetaDataInfo *bmtMetaData,
        bmtMethodInfo *bmtMethod,
        bmtMethAndFieldDescs *bmtMFDescs,
        bmtFieldPlacement *bmtFP,
        bmtInternalInfo *bmtInternal,
        bmtGCSeriesInfo *bmtGCSeries,
        bmtMethodImplInfo *bmtMethodImpl,
        const bmtGenericsInfo *bmtGenerics,
        bmtEnumFieldInfo *bmtEnumFields);

    // --------------------------------------------------------------------------------------------
    // Returns the parent bmtRTType pointer. Can be null if no parent exists.
    inline bmtRTType *
    GetParentType()
        { WRAPPER_NO_CONTRACT; return bmtInternal->pType->GetParentType(); }

    // --------------------------------------------------------------------------------------------
    // Takes care of checking against NULL on the pointer returned by GetParentType. Returns true
    // if the type being built has a parent; returns false otherwise.
    // NOTE: false will typically only be returned for System.Object and interfaces.
    inline bool
    HasParent()
    {
        LIMITED_METHOD_CONTRACT; return bmtInternal->pParentMT != NULL;
    }

    // --------------------------------------------------------------------------------------------
    inline MethodTable *
    GetParentMethodTable()
    {
        LIMITED_METHOD_CONTRACT; return bmtInternal->pParentMT;
    }

    // --------------------------------------------------------------------------------------------
    // Created to help centralize knowledge of where all the information about each method is
    // stored. Eventually, this can hopefully be removed and it should be sufficient to iterate
    // over the array of bmtMDMethod* that hold all the declared methods.
    class DeclaredMethodIterator
    {
      private:
        const int            m_numDeclaredMethods;
        bmtMDMethod ** const m_declaredMethods;
        int                  m_idx; // not SLOT_INDEX?
#ifdef _DEBUG
        bmtMDMethod *        m_debug_pMethod;
#endif

      public:
        inline                  DeclaredMethodIterator(MethodTableBuilder &mtb);
        inline int              CurrentIndex();
        inline BOOL             Next();
        inline BOOL             Prev();
        inline void             ResetToEnd();
        inline mdToken          Token() const;
        inline DWORD            Attrs();
        inline DWORD            RVA();
        inline DWORD            ImplFlags();
        inline LPCSTR           Name();
        inline PCCOR_SIGNATURE  GetSig(DWORD *pcbSig);
        inline METHOD_IMPL_TYPE MethodImpl();
        inline BOOL             IsMethodImpl();
        inline METHOD_TYPE      MethodType();
        inline bmtMDMethod     *GetMDMethod() const;
        inline MethodDesc      *GetIntroducingMethodDesc();
        inline bmtMDMethod *    operator->();
        inline bmtMDMethod *    operator*() { WRAPPER_NO_CONTRACT; return GetMDMethod(); }
    };  // class DeclaredMethodIterator
    friend class DeclaredMethodIterator;

    inline SLOT_INDEX NumDeclaredMethods() { LIMITED_METHOD_CONTRACT; return bmtMethod->GetDeclaredMethodCount(); }
    inline DWORD NumDeclaredFields() { LIMITED_METHOD_CONTRACT; return bmtEnumFields->dwNumDeclaredFields; }

    // --------------------------------------------------------------------------------------------
    // Used to report an error building this type.
    static VOID DECLSPEC_NORETURN
    BuildMethodTableThrowException(
                                  HRESULT hr,
                                  const bmtErrorInfo & bmtError);

    // --------------------------------------------------------------------------------------------
    // Used to report an error building this type.
    inline VOID DECLSPEC_NORETURN
    BuildMethodTableThrowException(
                                  HRESULT hr,
                                  UINT idResWhy,
                                  mdMethodDef tokMethodDef)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        bmtError->resIDWhy = idResWhy;
        bmtError->dMethodDefInError = tokMethodDef;
        bmtError->szMethodNameForError = NULL;
        bmtError->cl = GetCl();
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // --------------------------------------------------------------------------------------------
    // Used to report an error building this type.
    inline VOID DECLSPEC_NORETURN
    BuildMethodTableThrowException(
        HRESULT hr,
        UINT idResWhy,
        LPCUTF8 szMethodName)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        bmtError->resIDWhy = idResWhy;
        bmtError->dMethodDefInError = mdMethodDefNil;
        bmtError->szMethodNameForError = szMethodName;
        bmtError->cl = GetCl();
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // --------------------------------------------------------------------------------------------
    // Used to report an error building this type.
    inline VOID DECLSPEC_NORETURN
    BuildMethodTableThrowException(
                                  UINT idResWhy,
                                  mdMethodDef tokMethodDef = mdMethodDefNil)
    {
        WRAPPER_NO_CONTRACT;
        BuildMethodTableThrowException(COR_E_TYPELOAD, idResWhy, tokMethodDef);
    }

    // --------------------------------------------------------------------------------------------
    // Used to report an error building this type.
    inline VOID DECLSPEC_NORETURN
    BuildMethodTableThrowException(
                                  UINT idResWhy,
                                  LPCUTF8 szMethodName)
    {
        WRAPPER_NO_CONTRACT;
        BuildMethodTableThrowException(COR_E_TYPELOAD, idResWhy, szMethodName);
    }

private:
    // --------------------------------------------------------------------------------------------
    // To be removed. Creates a hash table of all the names of the virtual methods in pMT,
    // and associates them with their corresponding bmtRTMethod* values.
    MethodNameHash *CreateMethodChainHash(
        MethodTable *pMT);

    // --------------------------------------------------------------------------------------------
    // Only used in the resolve phase of the classloader. These are used to calculate
    // the interface implementation map. The reason it is done in this way is that the
    // interfaces must be resolved in light of generic types and substitutions, and the fact
    // that substitutions can make interfaces resolve to be identical when given a child's
    // instantiation.
    //
    // NOTE: See DevDiv bug 795 for details.

    void ExpandApproxInterface(
        bmtInterfaceInfo *          bmtInterface, // out parameter, various parts cumulatively written to.
        const Substitution *        pNewInterfaceSubstChain,
        MethodTable *               pNewInterface,
        InterfaceDeclarationScope   declScope
        COMMA_INDEBUG(MethodTable * dbg_pClassMT));

    void ExpandApproxDeclaredInterfaces(
        bmtInterfaceInfo *          bmtInterface, // out parameter, various parts cumulatively written to.
        bmtTypeHandle               thType,
        InterfaceDeclarationScope   declScope
        COMMA_INDEBUG(MethodTable * dbg_pClassMT));

    void ExpandApproxInheritedInterfaces(
        bmtInterfaceInfo *      bmtInterface, // out parameter, various parts cumulatively written to.
        bmtRTType *             pParentType);

    void LoadApproxInterfaceMap();

public:
    //------------------------------------------------------------------------
    // Loading exact interface instantiations.(slow technique)
    //
    // These place the exact interface instantiations into the interface map at the
    // appropriate locations.

    struct bmtExactInterfaceInfo
    {
        DWORD nAssigned;
        MethodTable **pExactMTs;

        // Array of substitutions for each interface in the interface map
        Substitution * pInterfaceSubstitution;
        SigTypeContext typeContext;     // Exact type context used to supply final instantiation to substitution chains

        inline bmtExactInterfaceInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtExactInterfaceInfo

private:
    static void
    ExpandExactInterface(
        bmtExactInterfaceInfo *     bmtInfo,
        MethodTable *               pIntf,
        const Substitution *        pSubstForTypeLoad_OnStack,  // Allocated on stack!
        const Substitution *        pSubstForComparing_OnStack, // Allocated on stack!
        StackingAllocator *         pStackingAllocator
        COMMA_INDEBUG(MethodTable * dbg_pClassMT));

public:
    static void
    ExpandExactDeclaredInterfaces(
        bmtExactInterfaceInfo *     bmtInfo,
        Module *                    pModule,
        mdToken                     typeDef,
        const Substitution *        pSubstForTypeLoad,
        Substitution *              pSubstForComparing,
        StackingAllocator *     pStackingAllocator
        COMMA_INDEBUG(MethodTable * dbg_pClassMT));

    static void
    ExpandExactInheritedInterfaces(
        bmtExactInterfaceInfo * bmtInfo,
        MethodTable *           pParentMT,
        const Substitution *    pSubstForTypeLoad,
        Substitution *          pSubstForComparing,
        StackingAllocator *     pStackingAllocator);

public:
    // --------------------------------------------------------------------------------------------
    // Interface ambiguity checks when loading exact interface instantiations
    //
    // These implement the check that the exact instantiation does not introduce any
    // ambiguity in the interface dispatch logic, i.e. amongst the freshly declared interfaces.

    struct bmtInterfaceAmbiguityCheckInfo
    {
        MethodTable *pMT;
        DWORD nAssigned;
        MethodTable **ppExactDeclaredInterfaces;
        Substitution **ppInterfaceSubstitutionChains;
        SigTypeContext typeContext;

        inline bmtInterfaceAmbiguityCheckInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };  // struct bmtInterfaceAmbiguityCheckInfo

    static void
    InterfacesAmbiguityCheck(
        bmtInterfaceAmbiguityCheckInfo *,
        Module *pModule,
        mdToken typeDef,
        const Substitution *pSubstChain,
        StackingAllocator *pStackingAllocator);

private:
    static void
    InterfaceAmbiguityCheck(
        bmtInterfaceAmbiguityCheckInfo *,
        const Substitution *pSubstChain,
        MethodTable *pIntfMT,
        StackingAllocator *pStackingAllocator);

public:
    static void
    LoadExactInterfaceMap(
        MethodTable *pMT);

    // --------------------------------------------------------------------------------------------
    // Copy virtual slots inherited from parent:
    //
    // In types created at runtime, inherited virtual slots are initialized using approximate parent
    // during method table building. This method will update them based on the exact parent.
    // In types loaded from NGen image, inherited virtual slots from cross-module parents are not
    // initialized. This method will initialize them based on the actually loaded exact parent
    // if necessary.
    //
    static void
    CopyExactParentSlots(
        MethodTable *pMT,
        MethodTable *pApproxParentMT);

    // --------------------------------------------------------------------------------------------
    // This is used at load time, using metadata-based comparisons. It returns the array of dispatch
    // map TypeIDs to be used for pDeclIntfMT.
    //
    // Arguments:
    //    rg/c DispatchMapTypeIDs - Array of TypeIDs and its count of elements.
    //    pcIfaceDuplicates - Number of duplicate occurrences of the interface in the interface map (ideally <=
    //         count of elements TypeIDs).
    //
    void
    ComputeDispatchMapTypeIDs(
        MethodTable *        pDeclInftMT,
        const Substitution * pDeclIntfSubst,
        DispatchMapTypeID *  rgDispatchMapTypeIDs,
        UINT32               cDispatchMapTypeIDs,
        UINT32 *             pcIfaceDuplicates);

private:
    // --------------------------------------------------------------------------------------------
    // Looks for a virtual method in the parent matching methodSig. pMethodConstraintsMatch is
    // set if a match is found indicating whether or not the method constraint check passes.
    bmtRTMethod *
    LoaderFindMethodInParentClass(
        const MethodSignature & methodSig,
        BOOL *              pMethodConstraintsMatch);

    // --------------------------------------------------------------------------------------------
    //
    VOID
    ResolveInterfaces(
        WORD cEntries,
        BuildingInterfaceInfo_t* pEntries);

    // --------------------------------------------------------------------------------------------
    VOID
    ComputeModuleDependencies();

    // --------------------------------------------------------------------------------------------
    // Finds a method declaration from a MemberRef or Def. It handles the case where
    // the Ref or Def point back to this class even though it has not been fully
    // laid out.
    HRESULT
    FindMethodDeclarationForMethodImpl(
        mdToken  pToken,       // Token that is being located (MemberRef or MemberDef)
        mdToken* pDeclaration, // Method definition for Member
        BOOL fSameClass);      // Does the declaration need to be in this class

    BOOL
    IsEligibleForCovariantReturns(mdToken methodDeclToken);

    // --------------------------------------------------------------------------------------------
    // Enumerates the method impl token pairs and resolves the impl tokens to mdtMethodDef
    // tokens, since we currently have the limitation that all impls are in the current class.
    VOID
    EnumerateMethodImpls();

    // --------------------------------------------------------------------------------------------
    // Enumerates the methods declared by the class and populates the bmtMethod member with
    // bmtMDMethods* for each declared method.
    VOID
    EnumerateClassMethods();

    // --------------------------------------------------------------------------------------------
    // Enumerates the fields declared by the type and populates bmtEnumFields.
    VOID
    EnumerateClassFields();

    // --------------------------------------------------------------------------------------------
    // Allocate temporary memory for tracking all information used in building the MethodTable
    VOID
    AllocateWorkingSlotTables();

    // --------------------------------------------------------------------------------------------
    // Allocates all of the FieldDeses required after enumerating the fields declared by the type.
    VOID
    AllocateFieldDescs();

    // --------------------------------------------------------------------------------------------
    // Initializes all allocated FieldDescs
    VOID
    InitializeFieldDescs(
        FieldDesc *,
        const LayoutRawFieldInfo*,
        bmtInternalInfo*,
        const bmtGenericsInfo*,
        bmtMetaDataInfo*,
        bmtEnumFieldInfo*,
        bmtErrorInfo*,
        MethodTable***,
        bmtMethAndFieldDescs*,
        bmtFieldPlacement*,
        unsigned * totalDeclaredSize);

    // --------------------------------------------------------------------------------------------
    // Verify self-referencing static ValueType fields with RVA (when the size of the ValueType is known).
    void
    VerifySelfReferencingStaticValueTypeFields_WithRVA(
        MethodTable ** pByValueClassCache);

    // --------------------------------------------------------------------------------------------
    // Returns TRUE if dwByValueClassToken refers to the type being built; otherwise returns FALSE.
    BOOL
    IsSelfReferencingStaticValueTypeField(
        mdToken                 dwByValueClassToken,
        bmtInternalInfo*        bmtInternal,
        const bmtGenericsInfo * bmtGenericsInfo,
        PCCOR_SIGNATURE         pMemberSignature,
        DWORD                   cMemberSignature);

    // --------------------------------------------------------------------------------------------
    // Performs rudimentary stand-alone validation of methods declared by the type.
    VOID
    ValidateMethods();

    // --------------------------------------------------------------------------------------------
    // Initialize an allocated MethodDesc.
    VOID
    InitMethodDesc(
        MethodDesc *        pNewMD,
        DWORD               Classification,
        mdToken             tok,
        DWORD               dwImplFlags,
        DWORD               dwMemberAttrs,
        BOOL                fEnC,
        DWORD               RVA,          // Only needed for NDirect case
        IMDInternalImport * pIMDII,  // Needed for NDirect, EEImpl(Delegate) cases
        LPCSTR              pMethodName // Only needed for mcEEImpl (Delegate) case
        COMMA_INDEBUG(LPCUTF8             pszDebugMethodName)
        COMMA_INDEBUG(LPCUTF8             pszDebugClassName)
        COMMA_INDEBUG(LPCUTF8             pszDebugMethodSignature));

    // --------------------------------------------------------------------------------------------
    // Convert code:MethodTableBuilder::METHOD_TYPE to code:MethodClassification
    static DWORD
    GetMethodClassification(METHOD_TYPE type);

    // --------------------------------------------------------------------------------------------
    // Essentially, this is a helper method that combines calls to InitMethodDesc and
    // SetSecurityFlagsOnMethod. It then assigns the newly initialized MethodDesc to
    // the bmtMDMethod.
    VOID
    InitNewMethodDesc(
        bmtMDMethod * pMethod,
        MethodDesc * pNewMD);

    // --------------------------------------------------------------------------------------------
    // For every declared virtual method, determines if the method is an overload or requires a
    // new slot, performs the proper checks to ensure that an override is valid, and then
    // places the method in the appropriate slot in bmtVT and sets the SLOT_INDEX value in the
    // bmtMDMethod and it's MethodDesc.
    VOID
    PlaceVirtualMethods();

    // --------------------------------------------------------------------------------------------
    // For every declared non-virtual method, places the method in the next available slot in
    // the non-virtual section of bmtVT and sets the SLOT_INDEX value in the bmtMDMethod and it's
    // MethodDesc.
    VOID
    PlaceNonVirtualMethods();

    // --------------------------------------------------------------------------------------------
    // Determine the equivalence sets within the interface map
    // See comment in implementation for more details.
    VOID ComputeInterfaceMapEquivalenceSet();

    // --------------------------------------------------------------------------------------------
    // Given an interface in our interface map, and a particular method on that interface, place
    // a method from the parent types implementation of an equivalent interface into that method
    // slot. Used by PlaceInterfaceMethods to make equivalent interface implementations have the
    // same behavior as if the parent interface was implemented on this type instead of an equivalent interface.
    // See comment in implementation for example of where this is necessary.
    VOID PlaceMethodFromParentEquivalentInterfaceIntoInterfaceSlot(
        bmtInterfaceEntry::InterfaceSlotIterator &itfSlotIt,
        bmtInterfaceEntry *     pCurItfEntry,
        DispatchMapTypeID ** prgInterfaceDispatchMapTypeIDs,
        DWORD dwCurInterface);

    // --------------------------------------------------------------------------------------------
    // Matches interface methods with implementation methods in this type or a parent type.
    // See comment in implementation for more details.
    VOID
    PlaceInterfaceMethods();

    // --------------------------------------------------------------------------------------------
    // For every MethodImpl pair (represented by Entry) in bmtMethodImpl, place the body in the
    // appropriate interface or virtual slot.
    VOID
    PlaceMethodImpls();

    // --------------------------------------------------------------------------------------------
    // This will take the array of bmtMetaData->rgMethodImplTokens and further resolve the tokens
    // to their corresponding bmtMDMethod or bmtRTMethod pointers and then populate the array
    // in bmtMethodImpl, which will be used by PlaceMethodImpls
    VOID
    ProcessMethodImpls();

    // --------------------------------------------------------------------------------------------
    // This will take the array of bmtMetaData->rgMethodImplTokens and further resolve the tokens
    // to their corresponding bmtMDMethod or bmtRTMethod pointers and then populate the array
    // in bmtMethodImpl for the methodimpls which can resolve to more than one declaration method,
    // which will be used by PlaceMethodImpls
    VOID
    ProcessInexactMethodImpls();

    // --------------------------------------------------------------------------------------------
    // Find the decl method on a given interface entry that matches the method name+signature specified
    // If none is found, return a null method handle
    bmtMethodHandle
    FindDeclMethodOnInterfaceEntry(bmtInterfaceEntry *pItfEntry, MethodSignature &declSig);

    // --------------------------------------------------------------------------------------------
    // Find the decl method within the class hierarchy method name+signature specified
    // If none is found, return a null method handle
    bmtMethodHandle
    FindDeclMethodOnClassInHierarchy(const DeclaredMethodIterator& it, MethodTable * pDeclMT, MethodSignature &declSig);

    // --------------------------------------------------------------------------------------------
    // Throws if an entry already exists that has been MethodImpl'd. Adds the interface slot and
    // implementation method to the mapping used by virtual stub dispatch.
    VOID
    AddMethodImplDispatchMapping(
        DispatchMapTypeID typeID,
        SLOT_INDEX        slotNumber,
        bmtMDMethod *     pImplMethod);

    // --------------------------------------------------------------------------------------------
    // Throws if the signatures (excluding names) are not equal or the constraints don't match.
    // dwConstraintErrorCode is an input argument that states what error to throw in such a case
    // as the constraints don't match.
    VOID
    MethodImplCompareSignatures(
        bmtMethodHandle     hDecl,
        bmtMethodHandle     hImpl,
        BOOL                allowCovariantReturn,
        DWORD               dwConstraintErrorCode);

    // --------------------------------------------------------------------------------------------
    // This will provide the array of decls for the slots implemented by a methodImpl MethodDesc.
    // These are then used to map a slot in a MethodTable to the declaration method to be used in
    // name+sig matching through method calls and child types.
    VOID
    WriteMethodImplData(
        bmtMDMethod *       pImplMethod,
        DWORD               cSlots,
        DWORD *             rgSlots,
        mdToken *           rgTokens,
        RelativePointer<MethodDesc *> *       rgDeclMD);

    // --------------------------------------------------------------------------------------------
    // Places a methodImpl pair where the decl is declared by the type being built.
    VOID
    PlaceLocalDeclarationOnClass(
        bmtMDMethod *    pDecl,
        bmtMDMethod *    pImpl,
        DWORD*           slots,
        RelativePointer<MethodDesc *> *     replaced,
        DWORD*           pSlotIndex,
        DWORD            dwMaxSlotSize);

    // --------------------------------------------------------------------------------------------
    // Places a methodImpl pair where the decl is declared by a parent type.
    VOID
    PlaceParentDeclarationOnClass(
        bmtRTMethod *     pDecl,
        bmtMDMethod *     pImpl,
        DWORD*            slots,
        RelativePointer<MethodDesc *> *      replaced,
        DWORD*            pSlotIndex,
        DWORD             dwMaxSlotSize);

    // --------------------------------------------------------------------------------------------
    // Places a methodImpl pair on a class where the decl is declared by an interface.
    VOID
    PlaceInterfaceDeclarationOnClass(
        bmtRTMethod *     pDecl,
        bmtMDMethod *     pImpl);

    // --------------------------------------------------------------------------------------------
    // Places a methodImpl pair on an interface where the decl is declared by an interface.
    VOID
    PlaceInterfaceDeclarationOnInterface(
        bmtMethodHandle   hDecl,
        bmtMDMethod *     pImpl,
        DWORD*            slots,
        RelativePointer<MethodDesc *> *      replaced,
        DWORD*            pSlotIndex,
        DWORD             dwMaxSlotSize);

    // --------------------------------------------------------------------------------------------
    // This will validate that all interface methods that were matched during
    // layout also validate against type constraints.
    VOID
    ValidateInterfaceMethodConstraints();

    // --------------------------------------------------------------------------------------------
    // Used to allocate and initialize MethodDescs (both the boxed and unboxed entrypoints)
    VOID
    AllocAndInitMethodDescs();

    // --------------------------------------------------------------------------------------------
    // Allocates and initializes one method desc chunk.
    //
    // Arguments:
    //    startIndex - index of first method in bmtMethod array.
    //    count - number of methods in this chunk (contiguous region from startIndex)
    //    sizeOfMethodDescs - total expected size of MethodDescs in this chunk
    //
    // Used by AllocAndInitMethodDescs.
    //
    VOID
    AllocAndInitMethodDescChunk(COUNT_T startIndex, COUNT_T count, SIZE_T sizeOfMethodDescs);

    // --------------------------------------------------------------------------------------------
    // MethodTableBuilder equivant of
    //      code:MethodDesc::IsUnboxingStub && code:MethodDesc::IsTightlyBoundToMethodTable.
    // Returns true if the MethodTable has to have true slot for unboxing stub of this method.
    // Used for MethodDesc layout.
    BOOL
    NeedsTightlyBoundUnboxingStub(bmtMDMethod * pMDMethod);

    // --------------------------------------------------------------------------------------------
    // MethodTableBuilder equivalent of code:MethodDesc::HasNativeCodeSlot.
    // Used for MethodDesc layout.
    BOOL
    NeedsNativeCodeSlot(bmtMDMethod * pMDMethod);

    // --------------------------------------------------------------------------------------------
    // Used to allocate and initialize the dictionary used with generic types.
    VOID
    AllocAndInitDictionary();

    VOID
    PlaceRegularStaticFields();

    VOID
    PlaceThreadStaticFields();

    VOID
    PlaceInstanceFields(
        MethodTable **);

    BOOL
    CheckForVtsEventMethod(
        IMDInternalImport  *pImport,
        MethodDesc         *pMD,
        DWORD               dwAttrs,
        LPCUTF8             szAttrName,
        MethodDesc        **ppMethodDesc);


    VOID
    CheckForSystemTypes();

    VOID SetupMethodTable2(
        Module* pLoaderModule
#ifdef FEATURE_PREJIT
        , Module* pComputedPZM
#endif // FEATURE_PREJIT
        );

    VOID HandleGCForValueClasses(
        MethodTable **);

    BOOL HasDefaultInterfaceImplementation(bmtRTType *pIntfType, MethodDesc *pIntfMD);
    VOID VerifyVirtualMethodsImplemented(MethodTable::MethodData * hMTData);

    VOID CheckForTypeEquivalence(
        WORD                     cBuildingInterfaceList,
        BuildingInterfaceInfo_t *pBuildingInterfaceList);

    VOID EnsureRIDMapsCanBeFilled();

    VOID CheckForRemotingProxyAttrib();

#ifdef FEATURE_COMINTEROP

    VOID GetCoClassAttribInfo();

#endif // FEATURE_COMINTEROP

    VOID CheckForSpecialTypes();

#ifdef FEATURE_READYTORUN

    VOID CheckLayoutDependsOnOtherModules(MethodTable * pDependencyMT);

    BOOL NeedsAlignedBaseOffset();

#endif // FEATURE_READYTORUN

    VOID SetFinalizationSemantics();

    VOID HandleExplicitLayout(
        MethodTable **pByValueClassCache);

    static ExplicitFieldTrust::TrustLevel CheckValueClassLayout(
        MethodTable * pMT,
        BYTE *    pFieldLayout,
        DWORD *  pFirstObjectOverlapOffset);

    void FindPointerSeriesExplicit(
        UINT   instanceSliceSize,
        BYTE * pFieldLayout);

    VOID    HandleGCForExplicitLayout();

    VOID    CheckForHFA(MethodTable ** pByValueClassCache);

    VOID    CheckForNativeHFA();

#ifdef UNIX_AMD64_ABI
    // checks whether the struct is enregisterable.
    void SystemVAmd64CheckForPassStructInRegister();
    // Store the eightbyte classification into the EEClass
    void StoreEightByteClassification(SystemVStructRegisterPassingHelper* helper);

#endif // UNIX_AMD64_ABI

    // this accesses the field size which is temporarily stored in m_pMTOfEnclosingClass
    // during class loading. Don't use any other time
    DWORD GetFieldSize(FieldDesc *pFD);

    bool IsEnclosingNestedTypePair(
        bmtTypeHandle hBase,
        bmtTypeHandle hChild);

    bool IsBaseTypeAlsoEnclosingType(
        bmtTypeHandle hBase,
        bmtTypeHandle hChild);

    BOOL TestOverrideForAccessibility(
        bmtMethodHandle hParentMethod,
        bmtTypeHandle   hChildType);

    VOID TestOverRide(
        bmtMethodHandle hParentMethod,
        bmtMethodHandle hChildMethod);

    VOID TestMethodImpl(
        bmtMethodHandle hDeclMethod,
        bmtMethodHandle hImplMethod);

    // Heuristic to detemine if we would like instances of this class 8 byte aligned
    BOOL ShouldAlign8(
        DWORD dwR8Fields,
        DWORD dwTotalFields);

    MethodTable * AllocateNewMT(Module *pLoaderModule,
                                DWORD dwVtableSlots,
                                DWORD dwVirtuals,
                                DWORD dwGCSize,
                                DWORD dwNumInterfaces,
                                DWORD dwNumDicts,
                                DWORD dwNumTypeSlots,
                                MethodTable *pMTParent,
                                ClassLoader *pClassLoader,
                                LoaderAllocator *pAllocator,
                                BOOL isIFace,
                                BOOL fDynamicStatics,
                                BOOL fHasGenericsStaticsInfo,
                                BOOL fHasVirtualStaticMethods
#ifdef FEATURE_COMINTEROP
                                , BOOL bHasDynamicInterfaceMap
#endif
#ifdef FEATURE_PREJIT
                                , Module *pComputedPZM
#endif // FEATURE_PREJIT
                                , AllocMemTracker *pamTracker
        );

};  // class MethodTableBuilder

#include "methodtablebuilder.inl"

#endif // !METHODTABLEBUILDER_H
