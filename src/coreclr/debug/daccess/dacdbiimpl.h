// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DacDbiImpl.h
//

//
// Implement the interface between the DAC and DBI.
//*****************************************************************************

#ifndef _DACDBI_IMPL_H_
#define _DACDBI_IMPL_H_

// Prototype for creation functions

STDAPI
DLLEXPORT
CLRDataCreateInstance(REFIID iid,
    ICLRDataTarget * pLegacyTarget,
    void ** iface);

STDAPI
DLLEXPORT
DacDbiInterfaceInstance(
    ICorDebugDataTarget * pTarget,
    CORDB_ADDRESS baseAddress,
    IDacDbiInterface::IAllocator * pAllocator,
    IDacDbiInterface::IMetaDataLookup * pMetaDataLookup,
    IDacDbiInterface ** ppInterface);

//---------------------------------------------------------------------------------------
//
// This implements the DAC/DBI interface. See that interface declaration for
// full documentation on these methods.
//
// Assumptions:
//    This class is free-threaded and provides its own synchronization.
//
// Notes:
//    It inherits from ClrDataAccess to get the DAC-management implementation, and to
//    override GetMDImport.
//
class DacDbiInterfaceImpl :
    public ClrDataAccess,
    public IDacDbiInterface
{
public:
    // Ctor to instantiate a DAC reader around a given data-target.
    DacDbiInterfaceImpl(ICorDebugDataTarget * pTarget, CORDB_ADDRESS baseAddress, IAllocator * pAllocator, IMetaDataLookup * pLookup);

    // Destructor.
    virtual ~DacDbiInterfaceImpl(void);

    // Overridden from ClrDataAccess. Gets an internal metadata importer for the file.
    virtual IMDInternalImport* GetMDImport(
        const PEFile* pPEFile,
        const ReflectionModule * pReflectionModule,
        bool fThrowEx);


    // Check whether the version of the DBI matches the version of the runtime.
    HRESULT CheckDbiVersion(const DbiVersion * pVersion);

    // Flush the DAC cache. This should be called when target memory changes.
    HRESULT FlushCache();

    // enable or disable DAC target consistency checks
    void DacSetTargetConsistencyChecks(bool fEnableAsserts);

    // Destroy the interface object. The client should call this when it's done
    // with the IDacDbiInterface to free up any resources.
    void Destroy();

    IAllocator * GetAllocator()
    {
        return m_pAllocator;
    }


    // Is Left-side started up?
    BOOL IsLeftSideInitialized();

    // Get an LS Appdomain via an AppDomain unique ID.
    // Fails if the AD is not found or if the ID is invalid.
    VMPTR_AppDomain GetAppDomainFromId(ULONG appdomainId);

    // Get the AppDomain ID for an AppDomain.
    ULONG GetAppDomainId(VMPTR_AppDomain vmAppDomain);

    // Get the managed AppDomain object for an AppDomain.
    VMPTR_OBJECTHANDLE GetAppDomainObject(VMPTR_AppDomain vmAppDomain);

    // Get the full AD friendly name for the appdomain.
    void GetAppDomainFullName(
        VMPTR_AppDomain vmAppDomain,
        IStringHolder * pStrName);

    // Get the values of the JIT Optimization and EnC flags.
    void GetCompilerFlags (VMPTR_DomainFile vmDomainFile,
                           BOOL * pfAllowJITOpts,
                           BOOL * pfEnableEnC);

    // Helper function for SetCompilerFlags to set EnC status
    bool CanSetEnCBits(Module * pModule);

    // Set the values of the JIT optimization and EnC flags.
    HRESULT SetCompilerFlags(VMPTR_DomainFile vmDomainFile,
                             BOOL             fAllowJitOpts,
                             BOOL             fEnableEnC);


    // Initialize the native/IL sequence points and native var info for a function.
    void GetNativeCodeSequencePointsAndVarInfo(VMPTR_MethodDesc  vmMethodDesc,
                                               CORDB_ADDRESS     startAddr,
                                               BOOL              fCodeAvailable,
                                               NativeVarData *   pNativeVarData,
                                               SequencePoints *  pSequencePoints);

    bool IsThreadSuspendedOrHijacked(VMPTR_Thread vmThread);


    bool AreGCStructuresValid();
    HRESULT CreateHeapWalk(HeapWalkHandle *pHandle);
    void DeleteHeapWalk(HeapWalkHandle handle);

    HRESULT WalkHeap(HeapWalkHandle handle,
                     ULONG count,
                     OUT COR_HEAPOBJECT * objects,
                     OUT ULONG *fetched);

    HRESULT GetHeapSegments(OUT DacDbiArrayList<COR_SEGMENT> *pSegments);


    bool IsValidObject(CORDB_ADDRESS obj);

    bool GetAppDomainForObject(CORDB_ADDRESS obj, OUT VMPTR_AppDomain * pApp, OUT VMPTR_Module *pModule, OUT VMPTR_DomainFile *mod);



    HRESULT CreateRefWalk(RefWalkHandle * pHandle, BOOL walkStacks, BOOL walkFQ, UINT32 handleWalkMask);
    void DeleteRefWalk(RefWalkHandle handle);
    HRESULT WalkRefs(RefWalkHandle handle, ULONG count, OUT DacGcReference * objects, OUT ULONG *pFetched);

    HRESULT GetTypeID(CORDB_ADDRESS obj, COR_TYPEID *pID);

    HRESULT GetTypeIDForType(VMPTR_TypeHandle vmTypeHandle, COR_TYPEID *pID);

    HRESULT GetObjectFields(COR_TYPEID id, ULONG32 celt, COR_FIELD *layout, ULONG32 *pceltFetched);
    HRESULT GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout);
    HRESULT GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout);
    void GetGCHeapInformation(COR_HEAPINFO * pHeapInfo);
    HRESULT GetPEFileMDInternalRW(VMPTR_PEFile vmPEFile, OUT TADDR* pAddrMDInternalRW);
    HRESULT GetReJitInfo(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ReJitInfo* pReJitInfo);
    HRESULT GetActiveRejitILCodeVersionNode(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ILCodeVersionNode* pVmILCodeVersionNode);
    HRESULT GetReJitInfo(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_ReJitInfo* pReJitInfo);
    HRESULT GetNativeCodeVersionNode(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_NativeCodeVersionNode* pVmNativeCodeVersionNode);
    HRESULT GetSharedReJitInfo(VMPTR_ReJitInfo vmReJitInfo, VMPTR_SharedReJitInfo* pSharedReJitInfo);
    HRESULT GetILCodeVersionNode(VMPTR_NativeCodeVersionNode vmNativeCodeVersionNode, VMPTR_ILCodeVersionNode* pVmILCodeVersionNode);
    HRESULT GetSharedReJitInfoData(VMPTR_SharedReJitInfo sharedReJitInfo, DacSharedReJitInfo* pData);
    HRESULT GetILCodeVersionNodeData(VMPTR_ILCodeVersionNode vmILCodeVersionNode, DacSharedReJitInfo* pData);
    HRESULT GetDefinesBitField(ULONG32 *pDefines);
    HRESULT GetMDStructuresVersion(ULONG32* pMDStructuresVersion);
    HRESULT EnableGCNotificationEvents(BOOL fEnable);

private:
    void TypeHandleToExpandedTypeInfoImpl(AreValueTypesBoxed              boxed,
                                       VMPTR_AppDomain                 vmAppDomain,
                                       TypeHandle                      typeHandle,
                                       DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Get the number of fixed arguments to a function, i.e., the explicit args and the "this" pointer.
    SIZE_T GetArgCount(MethodDesc * pMD);

    // Get locations and code offsets for local variables and arguments in a function
    void GetNativeVarData(MethodDesc *    pMethodDesc,
                          CORDB_ADDRESS   startAddr,
                          SIZE_T          fixedArgCount,
                          NativeVarData * pVarInfo);

    // Get the native/IL sequence points for a function
    void GetSequencePoints(MethodDesc *    pMethodDesc,
                           CORDB_ADDRESS    startAddr,
                           SequencePoints * pNativeMap);

    // Helper to compose a IL->IL and IL->Native mapping
    void ComposeMapping(const InstrumentedILOffsetMapping * pProfilerILMap, ICorDebugInfo::OffsetMapping nativeMap[], ULONG32* pEntryCount);

    // Helper function to convert an instrumented IL offset to the corresponding original IL offset.
    ULONG TranslateInstrumentedILOffsetToOriginal(ULONG                               ilOffset,
                                                  const InstrumentedILOffsetMapping * pMapping);

public:
//----------------------------------------------------------------------------------
    // class MapSortILMap:  A template class that will sort an array of DebuggerILToNativeMap.
    // This class is intended to be instantiated on the stack / in temporary storage, and used
    // to reorder the sequence map.
    //----------------------------------------------------------------------------------
    class MapSortILMap : public CQuickSort<DebuggerILToNativeMap>
    {
      public:
        //Constructor
        MapSortILMap(DebuggerILToNativeMap * map,
                  int count)
          : CQuickSort<DebuggerILToNativeMap>(map, count) {}

        // secondary key comparison--if two IL offsets are the same,
        // we determine order based on native offset
        int CompareInternal(DebuggerILToNativeMap * first,
                            DebuggerILToNativeMap * second);

        //Comparison operator
        int Compare(DebuggerILToNativeMap * first,
                    DebuggerILToNativeMap * second);
    };


    // GetILCodeAndSig returns the function's ILCode and SigToken given
    // a module and a token. The info will come from a MethodDesc, if
    // one exists or from metadata.
    //
    void GetILCodeAndSig(VMPTR_DomainFile vmDomainFile,
                         mdToken          functionToken,
                         TargetBuffer *   pCodeInfo,
                         mdToken *        pLocalSigToken);

    // Gets the following information about the native code blob for a function, if the native
    // code is available:
    //    its method desc
    //    whether it's an instantiated generic
    //    its EnC version number
    //    hot and cold region information.
    void GetNativeCodeInfo(VMPTR_DomainFile         vmDomainFile,
                           mdToken                  functionToken,
                           NativeCodeFunctionData * pCodeInfo);

    // Gets the following information about the native code blob for a function
    //    its method desc
    //    whether it's an instantiated generic
    //    its EnC version number
    //    hot and cold region information.
    void GetNativeCodeInfoForAddr(VMPTR_MethodDesc         vmMethodDesc,
                                  CORDB_ADDRESS            hotCodeStartAddr,
                                  NativeCodeFunctionData * pCodeInfo);

private:
    // Get start addresses and sizes for hot and cold regions for a native code blob
    void GetMethodRegionInfo(MethodDesc *             pMethodDesc,
                             NativeCodeFunctionData * pCodeInfo);

public:
    // Determine if a type is a ValueType
    BOOL IsValueType (VMPTR_TypeHandle th);

    // Determine if a type has generic parameters
    BOOL HasTypeParams (VMPTR_TypeHandle th);

    // Get type information for a class
    void GetClassInfo (VMPTR_AppDomain  vmAppDomain,
                       VMPTR_TypeHandle thExact,
                       ClassInfo *      pData);

    // get field information and object size for an instantiated generic type
    void GetInstantiationFieldInfo (VMPTR_DomainFile             vmDomainFile,
                                    VMPTR_TypeHandle             vmThExact,
                                    VMPTR_TypeHandle             vmThApprox,
                                    DacDbiArrayList<FieldData> * pFieldList,
                                    SIZE_T *                     pObjectSize);


    void GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed,
                                   VMPTR_AppDomain vmAppDomain,
                                   CORDB_ADDRESS addr,
                                   DebuggerIPCE_ExpandedTypeData *pTypeInfo);


    void GetObjectExpandedTypeInfoFromID(AreValueTypesBoxed boxed,
                                         VMPTR_AppDomain vmAppDomain,
                                         COR_TYPEID id,
                                         DebuggerIPCE_ExpandedTypeData *pTypeInfo);


    // @dbgtodo Microsoft inspection: change DebuggerIPCE_ExpandedTypeData to DacDbiStructures type hierarchy
    // once ICorDebugType and ICorDebugClass are DACized
    // use a type handle to get the information needed to create the corresponding RS CordbType instance
    void TypeHandleToExpandedTypeInfo(AreValueTypesBoxed                       boxed,
                                      VMPTR_AppDomain                          vmAppDomain,
                                      VMPTR_TypeHandle                         vmTypeHandle,
                                      DebuggerIPCE_ExpandedTypeData *          pTypeInfo);

    // Get type handle for a TypeDef token, if one exists. For generics this returns the open type.
    VMPTR_TypeHandle GetTypeHandle(VMPTR_Module vmModule,
                                   mdTypeDef metadataToken);

    // Get the approximate type handle for an instantiated type. This may be identical to the exact type handle,
    // but if we have code sharing for generics,it may differ in that it may have canonical type parameters.
    VMPTR_TypeHandle GetApproxTypeHandle(TypeInfoList * pTypeData);

    // Get the exact type handle from type data
    HRESULT GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData * pTypeData,
                               ArgInfoList *   pArgInfo,
                               VMPTR_TypeHandle& vmTypeHandle);

    // Retrieve the generic type params for a given MethodDesc.  This function is specifically
    // for stackwalking because it requires the generic type token on the stack.
    void GetMethodDescParams(VMPTR_AppDomain     vmAppDomain,
                             VMPTR_MethodDesc    vmMethodDesc,
                             GENERICS_TYPE_TOKEN genericsToken,
                             UINT32 *            pcGenericClassTypeParams,
                             TypeParamsList *    pGenericTypeParams);

    // Get the target field address of a context or thread local static.
    CORDB_ADDRESS GetThreadStaticAddress(VMPTR_FieldDesc vmField,
                                         VMPTR_Thread    vmRuntimeThread);

    // Get the target field address of a collectible types static.
    CORDB_ADDRESS GetCollectibleTypeStaticAddress(VMPTR_FieldDesc vmField,
                                                  VMPTR_AppDomain vmAppDomain);

    // Get information about a field added with Edit And Continue.
    void GetEnCHangingFieldInfo(const EnCHangingFieldInfo * pEnCFieldInfo,
                                FieldData *           pFieldData,
                                BOOL *                pfStatic);

    // GetTypeHandleParams gets the necessary data for a type handle, i.e. its
    // type parameters, e.g. "String" and "List<int>" from the type handle
    // for "Dict<String,List<int>>", and sends it back to the right side.
    // This should not fail except for OOM

    void GetTypeHandleParams(VMPTR_AppDomain  vmAppDomain,
                             VMPTR_TypeHandle vmTypeHandle,
                             TypeParamsList * pParams);

    // DacDbi API: GetSimpleType
    // gets the metadata token and domain file corresponding to a simple type
    void GetSimpleType(VMPTR_AppDomain    vmAppDomain,
                       CorElementType     simpleType,
                       mdTypeDef *        pMetadataToken,
                       VMPTR_Module     * pVmModule,
                       VMPTR_DomainFile * pVmDomainFile);

    BOOL IsExceptionObject(VMPTR_Object vmObject);

    void GetStackFramesFromException(VMPTR_Object vmObject, DacDbiArrayList<DacExceptionCallStackData>& dacStackFrames);

    // Returns true if the argument is a runtime callable wrapper
    BOOL IsRcw(VMPTR_Object vmObject);

    BOOL IsDelegate(VMPTR_Object vmObject);

    HRESULT GetDelegateType(VMPTR_Object delegateObject, DelegateType *delegateType);

    HRESULT GetDelegateFunctionData(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_DomainFile *ppFunctionDomainFile,
        OUT mdMethodDef *pMethodDef);

    HRESULT GetDelegateTargetObject(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_Object *ppTargetObj,
        OUT VMPTR_AppDomain *ppTargetAppDomain);

    HRESULT GetLoaderHeapMemoryRanges(OUT DacDbiArrayList<COR_MEMORY_RANGE> * pRanges);

    HRESULT IsModuleMapped(VMPTR_Module pModule, OUT BOOL *isModuleMapped);

    bool MetadataUpdatesApplied();

    // retrieves the list of COM interfaces implemented by vmObject, as it is known at
    // the time of the call (the list may change as new interface types become available
    // in the runtime)
    void GetRcwCachedInterfaceTypes(
                        VMPTR_Object vmObject,
                        VMPTR_AppDomain vmAppDomain,
                        BOOL bIInspectableOnly,
                        OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pDacInterfaces);

    // retrieves the list of interfaces pointers implemented by vmObject, as it is known at
    // the time of the call (the list may change as new interface types become available
    // in the runtime)
    void GetRcwCachedInterfacePointers(
                        VMPTR_Object vmObject,
                        BOOL bIInspectableOnly,
                        OUT DacDbiArrayList<CORDB_ADDRESS> * pDacItfPtrs);

    // retrieves a list of interface types corresponding to the passed in
    // list of IIDs. the interface types are retrieved from an app domain
    // IID / Type cache, that is updated as new types are loaded. will
    // have NULL entries corresponding to unknown IIDs in "iids"
    void GetCachedWinRTTypesForIIDs(
                        VMPTR_AppDomain vmAppDomain,
    					DacDbiArrayList<GUID> & iids,
	    				OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes);

    // retrieves the whole app domain cache of IID / Type mappings.
    void GetCachedWinRTTypes(
                        VMPTR_AppDomain vmAppDomain,
                        OUT DacDbiArrayList<GUID> * pGuids,
                        OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes);

private:
    // Helper to enumerate all possible memory ranges help by a loader allocator.
    void EnumerateMemRangesForLoaderAllocator(
        PTR_LoaderAllocator pLoaderAllocator,
        CQuickArrayList<COR_MEMORY_RANGE> *rangeAcummulator);

    void EnumerateMemRangesForJitCodeHeaps(
        CQuickArrayList<COR_MEMORY_RANGE> *rangeAcummulator);

    // Given a pointer to a managed function, obtain the method desc for it.
    // Equivalent to GetMethodDescPtrFromIp, except if the method isn't jitted
    // it will look for it in code stubs.
    // Returns:
    //   S_OK on success.
    //   If it's a jitted method, error codes equivalent to GetMethodDescPtrFromIp
    //   E_INVALIDARG if a non-jitted metod can't be located in the stubs.
    HRESULT GetMethodDescPtrFromIpEx(
        TADDR funcIp,
        OUT VMPTR_MethodDesc *ppMD);

    BOOL IsExceptionObject(MethodTable* pMT);

    // Get the approximate and exact type handles for a type
    void GetTypeHandles(VMPTR_TypeHandle  vmThExact,
                        VMPTR_TypeHandle  vmThApprox,
                        TypeHandle *      pThExact,
                        TypeHandle *      pThApprox);

    // Gets the total number of fields for a type.
    unsigned int GetTotalFieldCount(TypeHandle thApprox);

    // initializes various values of the ClassInfo data structure, including the
    // field count, generic args count, size and value class flag
    void InitClassData(TypeHandle  thApprox,
                       BOOL        fIsInstantiatedType,
                       ClassInfo * pData);

    // Gets the base table addresses for both GC and non-GC statics
    void GetStaticsBases(TypeHandle  thExact,
                         AppDomain * pAppDomain,
                         PTR_BYTE *  ppGCStaticsBase,
                         PTR_BYTE *  ppNonGCStaticsBase);

    // Computes the field info for pFD and stores it in pcurrentFieldData
    void ComputeFieldData(PTR_FieldDesc pFD,
                          PTR_BYTE    pGCStaticsBase,
                          PTR_BYTE    pNonGCStaticsBase,
                          FieldData * pCurrentFieldData);

    // Gets information for all the fields for a given type
    void CollectFields(TypeHandle                   thExact,
                       TypeHandle                   thApprox,
                       AppDomain *                  pAppDomain,
                       DacDbiArrayList<FieldData> * pFieldList);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is E_T_ARRAY
    void GetArrayTypeInfo(TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                          AppDomain *                     pAppDomain);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is
    // E_T_PTR or E_T_BYREF
    void GetPtrTypeInfo(AreValueTypesBoxed              boxed,
                        TypeHandle                      typeHandle,
                        DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                        AppDomain *                     pAppDomain);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is E_T_FNPTR
    void GetFnPtrTypeInfo(AreValueTypesBoxed              boxed,
                          TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                          AppDomain *                     pAppDomain);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is
    // E_T_CLASS or E_T_VALUETYPE
    void GetClassTypeInfo(TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                          AppDomain *                     pAppDomain);

    // Gets the correct CorElementType value from a type handle
    CorElementType GetElementType (TypeHandle typeHandle);

    // Gets additional information to convert a type handle to an instance of CordbType for the referent of an
    // E_T_BYREF or E_T_PTR or for the element type of an E_T_ARRAY or E_T_SZARRAY
    void TypeHandleToBasicTypeInfo(TypeHandle                   typeHandle,
                                   DebuggerIPCE_BasicTypeData * pTypeInfo,
                                   AppDomain *                  pAppDomain);

    // wrapper routines to set up for a call to ClassLoader functions to retrieve a type handle for a
    // particular kind of type

    // find a loaded type handle for a primitive type
    static TypeHandle FindLoadedElementType(CorElementType elementType);

    // find a loaded type handle for an array type (E_T_ARRAY or E_T_SZARRAY)
    static TypeHandle FindLoadedArrayType(CorElementType elementType, TypeHandle typeArg, unsigned rank);

    // find a loaded type handle for an address type (E_T_PTR or E_T_BYREF)
    static TypeHandle FindLoadedPointerOrByrefType(CorElementType elementType, TypeHandle typeArg);

    // find a loaded type handle for a function pointer type (E_T_FNPTR)
    static TypeHandle FindLoadedFnptrType(DWORD numTypeArgs, TypeHandle * pInst);

    // find a loaded type handle for a particular instantiation of a class type (E_T_CLASS or E_T_VALUETYPE)
    static TypeHandle FindLoadedInstantiation(Module *     pModule,
                                              mdTypeDef    mdToken,
                                              DWORD        nTypeArgs,
                                              TypeHandle * pInst);


    // TypeDataWalk
    // This class provides functionality to allow us to read type handles for generic type parameters or the
    // argument of an array or address type. It takes code sharing into account and allows us to get the canonical
    // form where necessary. It operates on a list of type arguments gathered on the RS and passed to the constructor.
    // See code:CordbType::GatherTypeData for more information.
    //
    class TypeDataWalk
    {
    private:
        // list of type arguments
        DebuggerIPCE_TypeArgData * m_pCurrentData;

        // number of type arguments still to be processed
        unsigned int m_nRemaining;

    public:
        typedef enum {kGetExact, kGetCanonical} TypeHandleReadType;
        // constructor
        TypeDataWalk(DebuggerIPCE_TypeArgData *pData, unsigned int nData);

        // Compute the type handle for a given type.
        // This is the top-level function that will return the type handle
        // for an arbitrary type. It uses mutual recursion with ReadLoadedTypeArg to get
        // the type handle for a (possibly parameterized) type. Note that the referent of
        // address types or the element type of an array type are viewed as type parameters.
        TypeHandle ReadLoadedTypeHandle(TypeHandleReadType retrieveWhich);

    private:
         // skip a single node from the list of type handles
        void Skip();

        // read and return a single node from the list of type parameters
        DebuggerIPCE_TypeArgData * ReadOne();

        //
        // These are for type arguments. They return null if the item could not be found.
        // They also optionally find the canonical form for the specified type
        // (used if generic code sharing is enabled) even if the exact form has not
        // yet been loaded for some reason
        //

        // Read a type handle when it is used in the position of a generic argument or
        // argument of an array type.  Take into account generic code sharing if we
        // have been requested to find the canonical representation amongst a set of shared-
        // code generic types.  That is, if generics code sharing is enabled then return "Object"
        // for all reference types, and canonicalize underneath value types, e.g. V<string> --> V<object>.
        //
        // Return TypeHandle() (null) if any of the type handles are not loaded.
        TypeHandle ReadLoadedTypeArg(TypeHandleReadType retrieveWhich);

        // Iterate through the type argument data, creating type handles as we go.
        // Return FALSE if any of the type handles are not loaded.
        BOOL ReadLoadedTypeHandles(TypeHandleReadType retrieveWhich, unsigned int nTypeArgs, TypeHandle *ppResults);

        // Read an instantiation of a generic type if it has already been created.
        TypeHandle ReadLoadedInstantiation(TypeHandleReadType retrieveWhich,
                                           Module *           pModule,
                                           mdTypeDef          mdToken,
                                           unsigned int       nTypeArgs);

        // These are helper functions to get the type handle for specific classes of types
        TypeHandle ArrayTypeArg(DebuggerIPCE_TypeArgData * pData, TypeHandleReadType retrieveWhich);
        TypeHandle PtrOrByRefTypeArg(DebuggerIPCE_TypeArgData * pData, TypeHandleReadType retrieveWhich);
        TypeHandle FnPtrTypeArg(DebuggerIPCE_TypeArgData * pData, TypeHandleReadType retrieveWhich);
        TypeHandle ClassTypeArg(DebuggerIPCE_TypeArgData * pData, TypeHandleReadType retrieveWhich);
        TypeHandle ObjRefOrPrimitiveTypeArg(DebuggerIPCE_TypeArgData * pData, CorElementType elementType);

    }; // class TypeDataWalk

    // get a typehandle for a class or valuetype from basic type data (metadata token
    // and domain file
    TypeHandle GetClassOrValueTypeHandle(DebuggerIPCE_BasicTypeData * pData);

    // get an exact type handle for an array type
    TypeHandle GetExactArrayTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                       ArgInfoList *                   pArgInfo);

    // get an exact type handle for a PTR or BYREF type
    TypeHandle GetExactPtrOrByRefTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                            ArgInfoList *                   pArgInfo);

    // get an exact type handle for a CLASS or VALUETYPE type
    TypeHandle GetExactClassTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                       ArgInfoList *                   pArgInfo);

    // get an exact type handle for a FNPTR type
    TypeHandle GetExactFnPtrTypeHandle(ArgInfoList * pArgInfo);

    // Convert basic type info for a type parameter that came from a top-level type to
    // the corresponding type handle. If the type parameter is an array or pointer
    // type, we simply extract the LS type handle from the VMPTR_TypeHandle that is
    // part of the type information. If the type parameter is a class or value type,
    // we use the metadata token and domain file in the type info to look up the
    // appropriate type handle. If the type parameter is any other types, we get the
    // type handle by having the loader look up the type handle for the element type.
    TypeHandle BasicTypeInfoToTypeHandle(DebuggerIPCE_BasicTypeData * pArgTypeData);

    // Convert type information for a top-level type to an exact type handle. This
    // information includes information about the element type if the top-level type is
    // an array type, the referent if the top-level type is a pointer type, or actual
    // parameters if the top-level type is a generic class or value type.
    TypeHandle ExpandedTypeInfoToTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                            ArgInfoList *                   pArgInfo);

    // Initialize information about a field added with EnC
    void InitFieldData(const FieldDesc *           pFD,
                       const PTR_CBYTE             pORField,
                       const EnCHangingFieldInfo * pEncFieldData,
                       FieldData *                 pFieldData);

    // Get the address of a field added with EnC.
    PTR_CBYTE GetPtrToEnCField(FieldDesc * pFD, const EnCHangingFieldInfo * pEnCFieldInfo);

    // Get the FieldDesc corresponding to a particular EnC field token
    FieldDesc * GetEnCFieldDesc(const EnCHangingFieldInfo * pEnCFieldInfo);

    // Finds information for a particular class field
    PTR_FieldDesc  FindField(TypeHandle thApprox, mdFieldDef fldToken);

// ============================================================================
// functions to get information about instances of ICDValue implementations
// ============================================================================

public:
    // Get object information for a TypedByRef object. Initializes the objRef and typedByRefType fields of
    // pObjectData (type info for the referent).
    void GetTypedByRefInfo(CORDB_ADDRESS             pTypedByRef,
                           VMPTR_AppDomain           vmAppDomain,
                           DebuggerIPCE_ObjectData * pObjectData);

    // Get the string length and offset to string base for a string object
    void GetStringData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData);

    // Get information for an array type referent of an objRef, including rank, upper and lower bounds,
    // element size and type, and the number of elements.
    void GetArrayData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData);

    // Get information about an object for which we have a reference, including the object size and
    // type information.
    void GetBasicObjectInfo(CORDB_ADDRESS             objectAddress,
                            CorElementType            type,
                            VMPTR_AppDomain           vmAppDomain,
                            DebuggerIPCE_ObjectData * pObjectData);

    // Returns the thread which owns the monitor lock on an object and the acquisition count
    MonitorLockInfo GetThreadOwningMonitorLock(VMPTR_Object vmObject);


    // Enumerate all threads waiting on the monitor event for an object
    void EnumerateMonitorEventWaitList(VMPTR_Object                   vmObject,
                                       FP_THREAD_ENUMERATION_CALLBACK fpCallback,
                                       CALLBACK_DATA                  pUserData);

private:
    // Helper function for CheckRef. Sanity check an object.
    HRESULT FastSanityCheckObject(PTR_Object objPtr);

    // Perform a sanity check on an object address to determine if this _could be_ a valid object. We can't
    // tell this for certain without walking the GC heap, but we do some fast tests to rule out clearly
    // invalid object addresses. See code:DacDbiInterfaceImpl::FastSanityCheckObject for more details.
    bool CheckRef(PTR_Object objPtr);

    // Initialize basic object information: type handle, object size, offset to fields and expanded type
    // information.
    void InitObjectData(PTR_Object                objPtr,
                        VMPTR_AppDomain           vmAppDomain,
                        DebuggerIPCE_ObjectData * pObjectData);

// ============================================================================
// Functions to test data safety. In these functions we determine whether a lock
// is held in a code path we need to execute for inspection. If so, we throw an
// exception.
// ============================================================================

#ifdef TEST_DATA_CONSISTENCY
public:
    void TestCrst(VMPTR_Crst vmCrst);
    void TestRWLock(VMPTR_SimpleRWLock vmRWLock);
#endif

// ============================================================================
// CordbAssembly, CordbModule
// ============================================================================

    using ClrDataAccess::GetModuleData;
    using ClrDataAccess::GetAddressType;

public:
    // Get the full path and file name to the assembly's manifest module.
    BOOL GetAssemblyPath(VMPTR_Assembly  vmAssembly,
                         IStringHolder * pStrFilename);

    void GetAssemblyFromDomainAssembly(VMPTR_DomainAssembly vmDomainAssembly, VMPTR_Assembly *vmAssembly);

    // Determines whether the runtime security system has assigned full-trust to this assembly.
    BOOL IsAssemblyFullyTrusted(VMPTR_DomainAssembly vmDomainAssembly);

    // get a type def resolved across modules
    void ResolveTypeReference(const TypeRefData * pTypeRefInfo,
                              TypeRefData *       pTargetRefInfo);

    // Get the full path and file name to the module (if any).
    BOOL GetModulePath(VMPTR_Module vmModule,
                       IStringHolder *  pStrFilename);

    // Get the full path and file name to the ngen image for the module (if any).
    BOOL GetModuleNGenPath(VMPTR_Module vmModule,
                           IStringHolder *  pStrFilename);

    // Implementation of IDacDbiInterface::GetModuleSimpleName
    void GetModuleSimpleName(VMPTR_Module vmModule, IStringHolder * pStrFilename);

    // Implementation of IDacDbiInterface::GetMetadata
    void GetMetadata(VMPTR_Module vmModule, TargetBuffer * pTargetBuffer);

    // Implementation of IDacDbiInterface::GetSymbolsBuffer
    void GetSymbolsBuffer(VMPTR_Module vmModule, TargetBuffer * pTargetBuffer, SymbolFormat * pSymbolFormat);

    // Gets properties for a module
    void GetModuleData(VMPTR_Module vmModule, ModuleInfo * pData);

    // Gets properties for a domainfile
    void GetDomainFileData(VMPTR_DomainFile vmDomainFile, DomainFileInfo * pData);

    void GetModuleForDomainFile(VMPTR_DomainFile vmDomainFile, OUT VMPTR_Module * pModule);

    // Yields true if the address is a CLR stub.
    BOOL IsTransitionStub(CORDB_ADDRESS address);

    // Get the "type" of address.
    AddressType GetAddressType(CORDB_ADDRESS address);


    // Enumerate the appdomains
    void EnumerateAppDomains(FP_APPDOMAIN_ENUMERATION_CALLBACK fpCallback,
                                void *                            pUserData);

    // Enumerate the assemblies in the appdomain.
    void  EnumerateAssembliesInAppDomain(VMPTR_AppDomain vmAppDomain,
                                           FP_ASSEMBLY_ENUMERATION_CALLBACK fpCallback,
                                           void *                           pUserData);

    // Enumerate the moduels in the given assembly.
    void EnumerateModulesInAssembly(
        VMPTR_DomainAssembly vmAssembly,
        FP_MODULE_ENUMERATION_CALLBACK fpCallback,
        void * pUserData
        );

    // When stopped at an event, request a synchronization.
    void RequestSyncAtEvent();

    //sets flag Debugger::m_sendExceptionsOutsideOfJMC on the LS
    HRESULT SetSendExceptionsOutsideOfJMC(BOOL sendExceptionsOutsideOfJMC);

    // Notify the debuggee that a debugger attach is pending.
    void MarkDebuggerAttachPending();

    // Notify the debuggee that a debugger is attached.
    void MarkDebuggerAttached(BOOL fAttached);

    // Enumerate connections in the process.
    void EnumerateConnections(FP_CONNECTION_CALLBACK fpCallback, void * pUserData);

    void EnumerateThreads(FP_THREAD_ENUMERATION_CALLBACK fpCallback, void * pUserData);

    bool IsThreadMarkedDead(VMPTR_Thread vmThread);

    // Return the handle of the specified thread.
    HANDLE GetThreadHandle(VMPTR_Thread vmThread);

    // Return the object handle for the managed Thread object corresponding to the specified thread.
    VMPTR_OBJECTHANDLE GetThreadObject(VMPTR_Thread vmThread);

    // Get the alocated bytes for this thread.
    void GetThreadAllocInfo(VMPTR_Thread vmThread, DacThreadAllocInfo* threadAllocInfo);

    // Set and reset the TSNC_DebuggerUserSuspend bit on the state of the specified thread
    // according to the CorDebugThreadState.
    void SetDebugState(VMPTR_Thread        vmThread,
                       CorDebugThreadState debugState);

    // Returns TRUE if there is a current exception which is unhandled
    BOOL HasUnhandledException(VMPTR_Thread vmThread);

    // Return the user state of the specified thread.
    CorDebugUserState GetUserState(VMPTR_Thread vmThread);

    // Returns the user state of the specified thread except for USER_UNSAFE_POINT.
    CorDebugUserState GetPartialUserState(VMPTR_Thread vmThread);

    // Return the connection ID of the specified thread.
    CONNID GetConnectionID(VMPTR_Thread vmThread);

    // Return the task ID of the specified thread.
    TASKID GetTaskID(VMPTR_Thread vmThread);

    // Return the OS thread ID of the specified thread
    DWORD TryGetVolatileOSThreadID(VMPTR_Thread vmThread);

    // Return the unique thread ID of the specified thread.
    DWORD GetUniqueThreadID(VMPTR_Thread vmThread);

    // Return the object handle to the managed Exception object of the current exception
    // on the specified thread.  The return value could be NULL if there is no current exception.
    VMPTR_OBJECTHANDLE GetCurrentException(VMPTR_Thread vmThread);

    // Return the object handle to the managed object for a given CCW pointer.
    VMPTR_OBJECTHANDLE GetObjectForCCW(CORDB_ADDRESS ccwPtr);

    // Return the object handle to the managed CustomNotification object of the current notification
    // on the specified thread.  The return value could be NULL if there is no current notification.
    // This will return non-null if and only if we are currently inside a CustomNotification Callback
    // (or a dump was generated while in this callback)
    VMPTR_OBJECTHANDLE GetCurrentCustomDebuggerNotification(VMPTR_Thread vmThread);


    // Return the current appdomain the specified thread is in.
    VMPTR_AppDomain GetCurrentAppDomain(VMPTR_Thread vmThread);

    // Given an assembly ref token and metadata scope (via the DomainFile), resolve the assembly.
    VMPTR_DomainAssembly ResolveAssembly(VMPTR_DomainFile vmScope, mdToken tkAssemblyRef);


    // Hijack the thread
    void Hijack(
        VMPTR_Thread                 vmThread,
        ULONG32                      dwThreadId,
        const EXCEPTION_RECORD *     pRecord,
        T_CONTEXT *                  pOriginalContext,
        ULONG32                      cbSizeContext,
        EHijackReason::EHijackReason reason,
        void *                       pUserData,
        CORDB_ADDRESS *              pRemoteContextAddr);

    // Return the filter CONTEXT on the LS.
    VMPTR_CONTEXT GetManagedStoppedContext(VMPTR_Thread vmThread);

    // Create and return a stackwalker on the specified thread.
    void CreateStackWalk(VMPTR_Thread       vmThread,
                         DT_CONTEXT *       pInternalContextBuffer,
                         StackWalkHandle *  ppSFIHandle);

    // Delete the stackwalk object
    void DeleteStackWalk(StackWalkHandle ppSFIHandle);

    // Get the CONTEXT of the current frame at which the stackwalker is stopped.
    void GetStackWalkCurrentContext(StackWalkHandle pSFIHandle,
                                    DT_CONTEXT *    pContext);

    void GetStackWalkCurrentContext(StackFrameIterator * pIter, DT_CONTEXT * pContext);

    // Set the stackwalker to the specified CONTEXT.
    void SetStackWalkCurrentContext(VMPTR_Thread           vmThread,
                                    StackWalkHandle        pSFIHandle,
                                    CorDebugSetContextFlag flag,
                                    DT_CONTEXT *           pContext);

    // Unwind the stackwalker to the next frame.
    BOOL UnwindStackWalkFrame(StackWalkHandle pSFIHandle);

    HRESULT CheckContext(VMPTR_Thread       vmThread,
                         const DT_CONTEXT * pContext);

    // Retrieve information about the current frame from the stackwalker.
    FrameType GetStackWalkCurrentFrameInfo(StackWalkHandle        pSFIHandle,
                                           DebuggerIPCE_STRData * pFrameData);

    // Return the number of internal frames on the specified thread.
    ULONG32 GetCountOfInternalFrames(VMPTR_Thread vmThread);

    // Enumerate the internal frames on the specified thread and invoke the provided callback on each of them.
    void EnumerateInternalFrames(VMPTR_Thread                           vmThread,
                                 FP_INTERNAL_FRAME_ENUMERATION_CALLBACK fpCallback,
                                 void *                                 pUserData);

    // Given the FramePointer of the parent frame and the FramePointer of the current frame,
    // check if the current frame is the parent frame.
    BOOL IsMatchingParentFrame(FramePointer fpToCheck, FramePointer fpParent);

    // Return the stack parameter size of the given method.
    ULONG32 GetStackParameterSize(CORDB_ADDRESS controlPC);

    // Return the FramePointer of the current frame at which the stackwalker is stopped.
    FramePointer GetFramePointer(StackWalkHandle pSFIHandle);

    FramePointer GetFramePointerWorker(StackFrameIterator * pIter);

    // Return TRUE if the specified CONTEXT is the CONTEXT of the leaf frame.
    // @dbgtodo  filter CONTEXT - Currently we check for the filter CONTEXT first.
    BOOL IsLeafFrame(VMPTR_Thread       vmThread,
                     const DT_CONTEXT * pContext);

    // DacDbi API: Get the context for a particular thread of the target process
    void GetContext(VMPTR_Thread vmThread, DT_CONTEXT * pContextBuffer);

    // This is a simple helper function to convert a CONTEXT to a DebuggerREGDISPLAY.  We need to do this
    // inside DDI because the RS has no notion of REGDISPLAY.
    void ConvertContextToDebuggerRegDisplay(const DT_CONTEXT * pInContext,
                                            DebuggerREGDISPLAY * pOutDRD,
                                            BOOL fActive);

    // Check if the given method is an IL stub or an LCD method.
    DynamicMethodType IsILStubOrLCGMethod(VMPTR_MethodDesc vmMethodDesc);

    // Return a TargetBuffer for the raw vararg signature.
    TargetBuffer GetVarArgSig(CORDB_ADDRESS   VASigCookieAddr,
                              CORDB_ADDRESS * pArgBase);

    // returns TRUE if the type requires 8-byte alignment
    BOOL RequiresAlign8(VMPTR_TypeHandle thExact);

    // Resolve the raw generics token to the real generics type token.  The resolution is based on the
    // given index.
    GENERICS_TYPE_TOKEN ResolveExactGenericArgsToken(DWORD               dwExactGenericArgsTokenIndex,
                                                     GENERICS_TYPE_TOKEN rawToken);

    // Enumerate all monitors blocking a thread
    void EnumerateBlockingObjects(VMPTR_Thread                           vmThread,
                                  FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK fpCallback,
                                  CALLBACK_DATA                          pUserData);

    // Returns a bitfield reflecting the managed debugging state at the time of
    // the jit attach.
    CLR_DEBUGGING_PROCESS_FLAGS GetAttachStateFlags();

protected:
    // This class used to be stateless, but we are relaxing the requirements
    // slightly to gain perf. We should still be stateless in the sense that an API call
    // should always return the same result regardless of the internal state. Hence
    // a caller can not distinguish that we have any state. Internally however we are
    // allowed to cache pieces of frequently used data to improve the perf of various
    // operations. All of this cached data should be flushed when the DAC is flushed.

    // But it can have helper methods.

    // The allocator object is conceptually stateless. It lets us allocate data buffers to hand back.
    IAllocator * m_pAllocator;

    // Callback to DBI to get internal metadata.
    IMetaDataLookup * m_pMetaDataLookup;


    // Metadata lookups is just a property on the PEFile in the normal builds,
    // and so VM code tends to access the same metadata importer many times in a row.
    // Cache the most-recently used to avoid excessive redundant lookups.

    // PEFile of Cached Importer. Invalidated between Flush calls. If this is Non-null,
    // then the importer is m_pCachedImporter, and we can avoid using IMetaDataLookup
    VMPTR_PEFile m_pCachedPEFile;

    // Value of cached importer, corresponds with m_pCachedPEFile.
    IMDInternalImport  * m_pCachedImporter;

    // Value of cached hijack function list, corresponds to g_pDebugger->m_rgHijackFunction
    BOOL m_isCachedHijackFunctionValid;
    TargetBuffer m_pCachedHijackFunction[Debugger::kMaxHijackFunctions];

    // Helper to write structured data to target.
    template<typename T>
    void SafeWriteStructOrThrow(CORDB_ADDRESS pRemotePtr, const T * pLocalBuffer);

    // Helper to read structured data from the target process.
    template<typename T>
    void SafeReadStructOrThrow(CORDB_ADDRESS pRemotePtr, T * pLocalBuffer);

    TADDR GetHijackAddress();

    void AlignStackPointer(CORDB_ADDRESS * pEsp);

    template <class T>
    CORDB_ADDRESS PushHelper(CORDB_ADDRESS * pEsp, const T * pData, BOOL fAlignStack);

    // Write an EXCEPTION_RECORD structure to the remote target at the specified address while taking
    // into account the number of exception parameters.
    void WriteExceptionRecordHelper(CORDB_ADDRESS pRemotePtr, const EXCEPTION_RECORD * pExcepRecord);

    typedef DPTR(struct DebuggerIPCControlBlock) PTR_DebuggerIPCControlBlock;

    // Get the address of the Debugger control block on the helper thread. Returns
    // NULL if the control block has not been successfully allocated
    CORDB_ADDRESS GetDebuggerControlBlockAddress();

    // Creates a VMPTR of an Object from a target address
    VMPTR_Object GetObject(CORDB_ADDRESS ptr);

    // sets state in the native binder
    HRESULT EnableNGENPolicy(CorDebugNGENPolicy ePolicy);

    // Sets the NGEN compiler flags. This restricts NGEN to only use images with certain
    // types of pregenerated code.
    HRESULT SetNGENCompilerFlags(DWORD dwFlags);

    // Gets the NGEN compiler flags currently in effect.
    HRESULT GetNGENCompilerFlags(DWORD *pdwFlags);

    // Creates a VMPTR of an Object from a target address pointing to an OBJECTREF
    VMPTR_Object GetObjectFromRefPtr(CORDB_ADDRESS ptr);

    // Get the target address from a VMPTR_OBJECTHANDLE, i.e., the handle address
    CORDB_ADDRESS GetHandleAddressFromVmHandle(VMPTR_OBJECTHANDLE vmHandle);

    // Gets the target address of an VMPTR of an Object
    TargetBuffer GetObjectContents(VMPTR_Object vmObj);

    // Create a VMPTR_OBJECTHANDLE from a CORDB_ADDRESS pointing to an object handle
    VMPTR_OBJECTHANDLE GetVmObjectHandle(CORDB_ADDRESS handleAddress);

    // Validate that the VMPTR_OBJECTHANDLE refers to a legitimate managed object
    BOOL IsVmObjectHandleValid(VMPTR_OBJECTHANDLE vmHandle);

    // if the specified module is a WinRT module then isWinRT will equal TRUE
    HRESULT IsWinRTModule(VMPTR_Module vmModule, BOOL& isWinRT);

    // Determines the app domain id for the object refered to by a given VMPTR_OBJECTHANDLE
    ULONG GetAppDomainIdFromVmObjectHandle(VMPTR_OBJECTHANDLE vmHandle);

private:
    bool IsThreadMarkedDeadWorker(Thread * pThread);

    // Check whether the specified thread is at a GC-safe place, i.e. in an interruptible region.
    BOOL IsThreadAtGCSafePlace(VMPTR_Thread vmThread);

    // Fill in the structure with information about the current frame at which the stackwalker is stopped
    void InitFrameData(StackFrameIterator *   pIter,
                       FrameType              ft,
                       DebuggerIPCE_STRData * pFrameData);

    // Helper method to fill in the address and the size of the hot and cold regions.
    void InitNativeCodeAddrAndSize(TADDR                      taStartAddr,
                                   DebuggerIPCE_JITFuncData * pJITFuncData);

    // Fill in the information about the parent frame.
    void InitParentFrameInfo(CrawlFrame * pCF,
                             DebuggerIPCE_JITFuncData * pJITFuncData);

    // Return the stack parameter size of the given method.
    ULONG32 GetStackParameterSize(EECodeInfo * pCodeInfo);

    typedef enum
    {
        kFromManagedToUnmanaged,
        kFromUnmanagedToManaged,
    } StackAdjustmentDirection;

    // Adjust the stack pointer in the CONTEXT for the stack parameter.
    void AdjustRegDisplayForStackParameter(REGDISPLAY *             pRD,
                                           DWORD                    cbStackParameterSize,
                                           BOOL                     fIsActiveFrame,
                                           StackAdjustmentDirection direction);

    // Given an explicit frame, return the corresponding type in terms of CorDebugInternalFrameType.
    CorDebugInternalFrameType GetInternalFrameType(Frame * pFrame);

    // Helper method to convert a REGDISPLAY to a CONTEXT.
    void UpdateContextFromRegDisp(REGDISPLAY * pRegDisp,
                                  T_CONTEXT *  pContext);

    // Check if a control PC is in one of the native functions which require special unwinding.
    bool IsRuntimeUnwindableStub(PCODE taControlPC);

    // Given the REGDISPLAY of a stack frame for one of the redirect functions, retrieve the original CONTEXT
    // before the thread redirection.
    PTR_CONTEXT RetrieveHijackedContext(REGDISPLAY * pRD);

    // Unwind special native stack frame which the runtime knows how to unwind.
    BOOL UnwindRuntimeStackFrame(StackFrameIterator * pIter);

    // Look up the EnC version number of a particular jitted instance of a managed method.
    void LookupEnCVersions(Module*          pModule,
                           VMPTR_MethodDesc vmMethodDesc,
                           mdMethodDef      mdMethod,
                           CORDB_ADDRESS    pNativeStartAddress,
                           SIZE_T *         pLatestEnCVersion,
                           SIZE_T *         pJittedInstanceEnCVersion = NULL);

    // @dbgtodo - This method should be removed once CordbFunctionBreakpoint and SetIP are moved OOP and
    // no longer use nativeCodeJITInfoToken.
    void SetDJIPointer(Module *                   pModule,
                       MethodDesc *               pMD,
                       mdMethodDef                mdMethod,
                       DebuggerIPCE_JITFuncData * pJITFuncData);

    // This is just a worker function for GetILCodeAndSig.  It returns the function's ILCode and SigToken
    // given a module, a token, and the RVA.  If a MethodDesc is provided, it has to be consistent with
    // the token and the RVA.
    mdSignature GetILCodeAndSigHelper(Module *       pModule,
                                      MethodDesc *   pMD,
                                      mdMethodDef    mdMethodToken,
                                      RVA            methodRVA,
                                      TargetBuffer * pIL);

public:
    // APIs for picking up the info needed for a debugger to look up an ngen image or IL image
    // from it's search path.
    bool GetMetaDataFileInfoFromPEFile(VMPTR_PEFile vmPEFile,
                                       DWORD &dwTimeStamp,
                                       DWORD &dwSize,
                                       bool  &isNGEN,
                                       IStringHolder* pStrFilename);

    bool GetILImageInfoFromNgenPEFile(VMPTR_PEFile vmPEFile,
                                      DWORD &dwTimeStamp,
                                      DWORD &dwSize,
                                      IStringHolder* pStrFilename);

};


// Global allocator for DD. Access is protected under the g_dacCritSec lock.
extern "C" IDacDbiInterface::IAllocator * g_pAllocator;


class DDHolder
{
public:
    DDHolder(DacDbiInterfaceImpl* pContainer, bool fAllowReentrant)
    {
        EnterCriticalSection(&g_dacCritSec);

        // If we're not re-entrant, then assert.
        if (!fAllowReentrant)
        {
            _ASSERTE(g_dacImpl == NULL);
        }

        // This cast is safe because ClrDataAccess can't call the DacDbi layer.
        m_pOldContainer = static_cast<DacDbiInterfaceImpl *> (g_dacImpl);
        m_pOldAllocator = g_pAllocator;

        g_dacImpl    = pContainer;
        g_pAllocator = pContainer->GetAllocator();

    }
    ~DDHolder()
    {
        // If an exception is being thrown, we won't be in the PAL (but in normal return paths it will).

        g_dacImpl    = m_pOldContainer;
        g_pAllocator = m_pOldAllocator;

        LeaveCriticalSection(&g_dacCritSec);
    }

protected:
    DacDbiInterfaceImpl * m_pOldContainer;
    IDacDbiInterface::IAllocator * m_pOldAllocator;
};


// Use this macro at the start of each DD function.
// This may nest if a DD primitive takes in a callback that then calls another DD primitive.
#define DD_ENTER_MAY_THROW \
    DDHolder __dacHolder(this, true); \


// Non-reentrant version of DD_ENTER_MAY_THROW. Asserts non-reentrancy.
// Use this macro at the start of each DD function.
// This may nest if a DD primitive takes in a callback that then calls another DD primitive.
#define DD_NON_REENTRANT_MAY_THROW \
    DDHolder __dacHolder(this, false); \

#include "dacdbiimpl.inl"

class DacRefWalker
{
public:
    DacRefWalker(ClrDataAccess *dac, BOOL walkStacks, BOOL walkFQ, UINT32 handleMask);
    ~DacRefWalker();

    HRESULT Init();
    HRESULT Next(ULONG celt, DacGcReference roots[], ULONG *pceltFetched);

private:
    UINT32 GetHandleWalkerMask();
    void Clear();
    HRESULT NextThread();

private:
    ClrDataAccess *mDac;
    BOOL mWalkStacks, mWalkFQ;
    UINT32 mHandleMask;

    // Stacks
    DacStackReferenceWalker *mStackWalker;

    // Handles
    DacHandleWalker *mHandleWalker;

    // FQ
    PTR_PTR_Object mFQStart;
    PTR_PTR_Object mFQEnd;
    PTR_PTR_Object mFQCurr;
};

#endif // _DACDBI_IMPL_H_
