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

    // IUnknown.
    // IDacDbiInterface now extends IUnknown, so DacDbiInterfaceImpl must resolve the
    // diamond inheritance by delegating to ClrDataAccess's existing IUnknown implementation
    // and adding support for the IDacDbiInterface IID.
    STDMETHOD(QueryInterface)(THIS_ IN REFIID interfaceId, OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    // Overridden from ClrDataAccess. Gets an internal metadata importer for the file.
    virtual IMDInternalImport* GetMDImport(
        const PEAssembly* pPEAssembly,
        const ReflectionModule * pReflectionModule,
        bool fThrowEx);


    // Check whether the version of the DBI matches the version of the runtime.
    HRESULT STDMETHODCALLTYPE CheckDbiVersion(const DbiVersion * pVersion);

    // Flush the DAC cache. This should be called when target memory changes.
    HRESULT STDMETHODCALLTYPE FlushCache();

    // enable or disable DAC target consistency checks
    HRESULT STDMETHODCALLTYPE DacSetTargetConsistencyChecks(BOOL fEnableAsserts);

    IAllocator * GetAllocator()
    {
        return m_pAllocator;
    }


    // Is Left-side started up?
    HRESULT STDMETHODCALLTYPE IsLeftSideInitialized(OUT BOOL * pResult);

    // Get the AppDomain ID for an AppDomain.
    HRESULT STDMETHODCALLTYPE GetAppDomainId(VMPTR_AppDomain vmAppDomain, OUT ULONG * pRetVal);

    // Get the managed AppDomain object for an AppDomain.
    HRESULT STDMETHODCALLTYPE GetAppDomainObject(VMPTR_AppDomain vmAppDomain, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Get the full AD friendly name for the appdomain.
    HRESULT STDMETHODCALLTYPE GetAppDomainFullName(VMPTR_AppDomain vmAppDomain, IStringHolder * pStrName);

    // Get the values of the JIT Optimization and EnC flags.
    HRESULT STDMETHODCALLTYPE GetCompilerFlags(VMPTR_Assembly vmAssembly,
                                                OUT BOOL * pfAllowJITOpts,
                                                OUT BOOL * pfEnableEnC);

    // Helper function for SetCompilerFlags to set EnC status
    bool CanSetEnCBits(Module * pModule);

    // Set the values of the JIT optimization and EnC flags.
    HRESULT STDMETHODCALLTYPE SetCompilerFlags(VMPTR_Assembly vmAssembly,
                                                BOOL           fAllowJitOpts,
                                                BOOL           fEnableEnC);


    // Initialize the native/IL sequence points and native var info for a function.
    HRESULT STDMETHODCALLTYPE GetNativeCodeSequencePointsAndVarInfo(VMPTR_MethodDesc vmMethodDesc, CORDB_ADDRESS startAddress, BOOL fCodeAvailable, OUT NativeVarData * pNativeVarData, OUT SequencePoints * pSequencePoints);

    HRESULT STDMETHODCALLTYPE IsThreadSuspendedOrHijacked(VMPTR_Thread vmThread, OUT BOOL * pResult);


    HRESULT STDMETHODCALLTYPE AreGCStructuresValid(OUT BOOL * pResult);
    HRESULT STDMETHODCALLTYPE CreateHeapWalk(HeapWalkHandle *pHandle);
    HRESULT STDMETHODCALLTYPE DeleteHeapWalk(HeapWalkHandle handle);

    HRESULT STDMETHODCALLTYPE WalkHeap(HeapWalkHandle handle,
                     ULONG count,
                     OUT COR_HEAPOBJECT * objects,
                     OUT ULONG *fetched);

    HRESULT STDMETHODCALLTYPE GetHeapSegments(OUT DacDbiArrayList<COR_SEGMENT> *pSegments);


    HRESULT STDMETHODCALLTYPE IsValidObject(CORDB_ADDRESS obj, OUT BOOL * pResult);

    HRESULT STDMETHODCALLTYPE CreateRefWalk(RefWalkHandle * pHandle, BOOL walkStacks, BOOL walkFQ, UINT32 handleWalkMask);
    HRESULT STDMETHODCALLTYPE DeleteRefWalk(RefWalkHandle handle);
    HRESULT STDMETHODCALLTYPE WalkRefs(RefWalkHandle handle, ULONG count, OUT DacGcReference * objects, OUT ULONG *pFetched);

    HRESULT STDMETHODCALLTYPE GetTypeID(CORDB_ADDRESS obj, COR_TYPEID *pID);

    HRESULT STDMETHODCALLTYPE GetTypeIDForType(VMPTR_TypeHandle vmTypeHandle, COR_TYPEID *pID);

    HRESULT STDMETHODCALLTYPE GetObjectFields(COR_TYPEID id, ULONG32 celt, COR_FIELD *layout, ULONG32 *pceltFetched);
    HRESULT STDMETHODCALLTYPE GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout);
    HRESULT STDMETHODCALLTYPE GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout);
    HRESULT STDMETHODCALLTYPE GetGCHeapInformation(OUT COR_HEAPINFO * pHeapInfo);
    HRESULT STDMETHODCALLTYPE GetPEFileMDInternalRW(VMPTR_PEAssembly vmPEAssembly, OUT TADDR* pAddrMDInternalRW);
#ifdef FEATURE_CODE_VERSIONING
    HRESULT STDMETHODCALLTYPE GetActiveRejitILCodeVersionNode(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ILCodeVersionNode* pVmILCodeVersionNode);
    HRESULT STDMETHODCALLTYPE GetNativeCodeVersionNode(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_NativeCodeVersionNode* pVmNativeCodeVersionNode);
    HRESULT STDMETHODCALLTYPE GetILCodeVersionNode(VMPTR_NativeCodeVersionNode vmNativeCodeVersionNode, VMPTR_ILCodeVersionNode* pVmILCodeVersionNode);
    HRESULT STDMETHODCALLTYPE GetILCodeVersionNodeData(VMPTR_ILCodeVersionNode vmILCodeVersionNode, DacSharedReJitInfo* pData);
#endif // FEATURE_CODE_VERSIONING
    HRESULT STDMETHODCALLTYPE AreOptimizationsDisabled(VMPTR_Module vmModule, mdMethodDef methodTk, OUT BOOL* pOptimizationsDisabled);
    HRESULT STDMETHODCALLTYPE GetDefinesBitField(ULONG32 *pDefines);
    HRESULT STDMETHODCALLTYPE GetMDStructuresVersion(ULONG32* pMDStructuresVersion);
    HRESULT STDMETHODCALLTYPE EnableGCNotificationEvents(BOOL fEnable);
    HRESULT STDMETHODCALLTYPE GetAssemblyFromModule(VMPTR_Module vmModule, OUT VMPTR_Assembly *pvmAssembly);
    HRESULT STDMETHODCALLTYPE ParseContinuation(CORDB_ADDRESS continuationAddress,
                                              OUT PCODE *pDiagnosticIP,
                                              OUT CORDB_ADDRESS *pNextContinuation,
                                              OUT UINT32 *pState);
    HRESULT STDMETHODCALLTYPE GetAsyncLocals(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeAddr, UINT32 state, OUT DacDbiArrayList<AsyncLocalData>* pAsyncLocals);
    HRESULT STDMETHODCALLTYPE GetGenericArgTokenIndex(VMPTR_MethodDesc vmMethod, OUT UINT32* pIndex);

private:
    void TypeHandleToExpandedTypeInfoImpl(AreValueTypesBoxed              boxed,
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
    HRESULT STDMETHODCALLTYPE GetILCodeAndSig(VMPTR_Assembly vmAssembly, mdToken functionToken, OUT TargetBuffer * pCodeInfo, OUT mdToken * pLocalSigToken);

    // Gets the following information about the native code blob for a function, if the native
    // code is available:
    //    its method desc
    //    whether it's an instantiated generic
    //    its EnC version number
    //    hot and cold region information.
    HRESULT STDMETHODCALLTYPE GetNativeCodeInfo(VMPTR_Assembly vmAssembly, mdToken functionToken, OUT NativeCodeFunctionData * pCodeInfo);

    // Gets the following information about the native code blob for a function
    //    its method desc
    //    whether it's an instantiated generic
    //    its EnC version number
    //    hot and cold region information
    //    its module
    //    its metadata token.
    HRESULT STDMETHODCALLTYPE GetNativeCodeInfoForAddr(CORDB_ADDRESS codeAddress, NativeCodeFunctionData * pCodeInfo, VMPTR_Module * pVmModule, mdToken * pFunctionToken);

private:
    // Get start addresses and sizes for hot and cold regions for a native code blob
    void GetMethodRegionInfo(MethodDesc *             pMethodDesc,
                             NativeCodeFunctionData * pCodeInfo);

public:
    // Determine if a type is a ValueType
    HRESULT STDMETHODCALLTYPE IsValueType(VMPTR_TypeHandle th, OUT BOOL * pResult);

    // Determine if a type has generic parameters
    HRESULT STDMETHODCALLTYPE HasTypeParams(VMPTR_TypeHandle th, OUT BOOL * pResult);

    // Get type information for a class
    HRESULT STDMETHODCALLTYPE GetClassInfo(VMPTR_TypeHandle thExact, ClassInfo * pData);

    // get field information and object size for an instantiated generic type
    HRESULT STDMETHODCALLTYPE GetInstantiationFieldInfo(VMPTR_Assembly vmAssembly, VMPTR_TypeHandle vmThExact, VMPTR_TypeHandle vmThApprox, OUT DacDbiArrayList<FieldData> * pFieldList, OUT SIZE_T * pObjectSize);


    HRESULT STDMETHODCALLTYPE GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed, CORDB_ADDRESS addr, OUT DebuggerIPCE_ExpandedTypeData * pTypeInfo);


    HRESULT STDMETHODCALLTYPE GetObjectExpandedTypeInfoFromID(AreValueTypesBoxed boxed, COR_TYPEID id, OUT DebuggerIPCE_ExpandedTypeData * pTypeInfo);


    // @dbgtodo Microsoft inspection: change DebuggerIPCE_ExpandedTypeData to DacDbiStructures type hierarchy
    // once ICorDebugType and ICorDebugClass are DACized
    // use a type handle to get the information needed to create the corresponding RS CordbType instance
    HRESULT STDMETHODCALLTYPE TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed, VMPTR_TypeHandle vmTypeHandle, DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Get type handle for a TypeDef token, if one exists. For generics this returns the open type.
    HRESULT STDMETHODCALLTYPE GetTypeHandle(VMPTR_Module vmModule, mdTypeDef metadataToken, OUT VMPTR_TypeHandle * pRetVal);

    // Get the approximate type handle for an instantiated type. This may be identical to the exact type handle,
    // but if we have code sharing for generics,it may differ in that it may have canonical type parameters.
    HRESULT STDMETHODCALLTYPE GetApproxTypeHandle(TypeInfoList * pTypeData, OUT VMPTR_TypeHandle * pRetVal);

    // Get the exact type handle from type data
    HRESULT STDMETHODCALLTYPE GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData * pTypeData,
                               ArgInfoList *   pArgInfo,
                               VMPTR_TypeHandle * pVmTypeHandle);

    // Retrieve the generic type params for a given MethodDesc.  This function is specifically
    // for stackwalking because it requires the generic type token on the stack.
    HRESULT STDMETHODCALLTYPE GetMethodDescParams(VMPTR_MethodDesc vmMethodDesc, GENERICS_TYPE_TOKEN genericsToken, OUT UINT32 * pcGenericClassTypeParams, OUT TypeParamsList * pGenericTypeParams);

    // Get the target field address of a context or thread local static.
    HRESULT STDMETHODCALLTYPE GetThreadStaticAddress(VMPTR_FieldDesc vmField, VMPTR_Thread vmRuntimeThread, OUT CORDB_ADDRESS * pRetVal);

    // Get the target field address of a collectible types static.
    HRESULT STDMETHODCALLTYPE GetCollectibleTypeStaticAddress(VMPTR_FieldDesc vmField, OUT CORDB_ADDRESS * pRetVal);

    // Get information about a field added with Edit And Continue.
    HRESULT STDMETHODCALLTYPE GetEnCHangingFieldInfo(const EnCHangingFieldInfo * pEnCFieldInfo, OUT FieldData * pFieldData, OUT BOOL * pfStatic);

    // GetTypeHandleParams gets the necessary data for a type handle, i.e. its
    // type parameters, e.g. "String" and "List<int>" from the type handle
    // for "Dict<String,List<int>>", and sends it back to the right side.
    // This should not fail except for OOM

    HRESULT STDMETHODCALLTYPE GetTypeHandleParams(VMPTR_TypeHandle vmTypeHandle, OUT TypeParamsList * pParams);

    // DacDbi API: GetSimpleType
    // gets the metadata token and assembly corresponding to a simple type
    HRESULT STDMETHODCALLTYPE GetSimpleType(CorElementType simpleType, OUT mdTypeDef * pMetadataToken, OUT VMPTR_Module * pVmModule);

    HRESULT STDMETHODCALLTYPE IsExceptionObject(VMPTR_Object vmObject, OUT BOOL * pResult);

    HRESULT STDMETHODCALLTYPE GetStackFramesFromException(VMPTR_Object vmObject, DacDbiArrayList<DacExceptionCallStackData>* pDacStackFrames);

    // Returns true if the argument is a runtime callable wrapper
    HRESULT STDMETHODCALLTYPE IsRcw(VMPTR_Object vmObject, OUT BOOL * pResult);

    HRESULT STDMETHODCALLTYPE IsDelegate(VMPTR_Object vmObject, OUT BOOL * pResult);

    HRESULT STDMETHODCALLTYPE GetDelegateType(VMPTR_Object delegateObject, DelegateType *delegateType);

    HRESULT STDMETHODCALLTYPE GetDelegateFunctionData(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_Assembly *ppFunctionAssembly,
        OUT mdMethodDef *pMethodDef);

    HRESULT STDMETHODCALLTYPE GetDelegateTargetObject(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_Object *ppTargetObj,
        OUT VMPTR_AppDomain *ppTargetAppDomain);

    HRESULT STDMETHODCALLTYPE GetLoaderHeapMemoryRanges(OUT DacDbiArrayList<COR_MEMORY_RANGE> * pRanges);

    HRESULT STDMETHODCALLTYPE IsModuleMapped(VMPTR_Module pModule, OUT BOOL *isModuleMapped);

    HRESULT STDMETHODCALLTYPE MetadataUpdatesApplied(OUT BOOL * pResult);

    // retrieves the list of interfaces pointers implemented by vmObject, as it is known at
    // the time of the call (the list may change as new interface types become available
    // in the runtime)
    HRESULT STDMETHODCALLTYPE GetRcwCachedInterfacePointers(VMPTR_Object vmObject, BOOL bIInspectableOnly, OUT DacDbiArrayList<CORDB_ADDRESS> * pDacItfPtrs);

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
    //   E_INVALIDARG if a non-jitted method can't be located in the stubs.
    HRESULT STDMETHODCALLTYPE GetMethodDescPtrFromIpEx(
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
                       DacDbiArrayList<FieldData> * pFieldList);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is E_T_ARRAY
    void GetArrayTypeInfo(TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is
    // E_T_PTR or E_T_BYREF
    void GetPtrTypeInfo(AreValueTypesBoxed              boxed,
                        TypeHandle                      typeHandle,
                        DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is E_T_FNPTR
    void GetFnPtrTypeInfo(AreValueTypesBoxed              boxed,
                          TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Gets additional information to convert a type handle to an instance of CordbType if the type is
    // E_T_CLASS or E_T_VALUETYPE
    void GetClassTypeInfo(TypeHandle                      typeHandle,
                          DebuggerIPCE_ExpandedTypeData * pTypeInfo);

    // Gets the correct CorElementType value from a type handle
    CorElementType GetElementType (TypeHandle typeHandle);

    // Gets additional information to convert a type handle to an instance of CordbType for the referent of an
    // E_T_BYREF or E_T_PTR or for the element type of an E_T_ARRAY or E_T_SZARRAY
    void TypeHandleToBasicTypeInfo(TypeHandle                   typeHandle,
                                   DebuggerIPCE_BasicTypeData * pTypeInfo);

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
    // and assembly
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
    // we use the metadata token and assembly in the type info to look up the
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
    HRESULT STDMETHODCALLTYPE GetTypedByRefInfo(CORDB_ADDRESS pTypedByRef, DebuggerIPCE_ObjectData * pObjectData);

    // Get the string length and offset to string base for a string object
    HRESULT STDMETHODCALLTYPE GetStringData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData);

    // Get information for an array type referent of an objRef, including rank, upper and lower bounds,
    // element size and type, and the number of elements.
    HRESULT STDMETHODCALLTYPE GetArrayData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData);

    // Get information about an object for which we have a reference, including the object size and
    // type information.
    HRESULT STDMETHODCALLTYPE GetBasicObjectInfo(CORDB_ADDRESS objectAddress, CorElementType type, DebuggerIPCE_ObjectData * pObjectData);

    // Returns the thread which owns the monitor lock on an object and the acquisition count
    HRESULT STDMETHODCALLTYPE GetThreadOwningMonitorLock(VMPTR_Object vmObject, OUT MonitorLockInfo * pRetVal);


    // Enumerate all threads waiting on the monitor event for an object
    HRESULT STDMETHODCALLTYPE EnumerateMonitorEventWaitList(VMPTR_Object vmObject, FP_THREAD_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData);

private:
    // Helper function for CheckRef. Sanity check an object.
    HRESULT STDMETHODCALLTYPE FastSanityCheckObject(PTR_Object objPtr);

    // Perform a sanity check on an object address to determine if this _could be_ a valid object. We can't
    // tell this for certain without walking the GC heap, but we do some fast tests to rule out clearly
    // invalid object addresses. See code:DacDbiInterfaceImpl::FastSanityCheckObject for more details.
    bool CheckRef(PTR_Object objPtr);

    // Initialize basic object information: type handle, object size, offset to fields and expanded type
    // information.
    void InitObjectData(PTR_Object                objPtr,
                        DebuggerIPCE_ObjectData * pObjectData);

// ============================================================================
// Functions to test data safety. In these functions we determine whether a lock
// is held in a code path we need to execute for inspection. If so, we throw an
// exception.
// ============================================================================

#ifdef TEST_DATA_CONSISTENCY
public:
    HRESULT STDMETHODCALLTYPE TestCrst(VMPTR_Crst vmCrst);
    HRESULT STDMETHODCALLTYPE TestRWLock(VMPTR_SimpleRWLock vmRWLock);
#endif

// ============================================================================
// CordbAssembly, CordbModule
// ============================================================================

    using ClrDataAccess::GetModuleData;
    using ClrDataAccess::GetAddressType;

public:
    // Get the full path and file name to the assembly's manifest module.
    HRESULT STDMETHODCALLTYPE GetAssemblyPath(VMPTR_Assembly vmAssembly, IStringHolder * pStrFilename, OUT BOOL * pResult);

    // get a type def resolved across modules
    HRESULT STDMETHODCALLTYPE ResolveTypeReference(const TypeRefData * pTypeRefInfo, TypeRefData * pTargetRefInfo);

    // Get the full path and file name to the module (if any).
    HRESULT STDMETHODCALLTYPE GetModulePath(VMPTR_Module vmModule, IStringHolder * pStrFilename, OUT BOOL * pResult);

    // Implementation of IDacDbiInterface::GetModuleSimpleName
    HRESULT STDMETHODCALLTYPE GetModuleSimpleName(VMPTR_Module vmModule, IStringHolder * pStrFilename);

    // Implementation of IDacDbiInterface::GetMetadata
    HRESULT STDMETHODCALLTYPE GetMetadata(VMPTR_Module vmModule, OUT TargetBuffer * pTargetBuffer);

    // Implementation of IDacDbiInterface::GetSymbolsBuffer
    HRESULT STDMETHODCALLTYPE GetSymbolsBuffer(VMPTR_Module vmModule, OUT TargetBuffer * pTargetBuffer, OUT SymbolFormat * pSymbolFormat);

    // Gets properties for a module
    HRESULT STDMETHODCALLTYPE GetModuleData(VMPTR_Module vmModule, OUT ModuleInfo * pData);

    // Gets properties for an assembly
    HRESULT STDMETHODCALLTYPE GetAssemblyInfo(VMPTR_Assembly vmAssembly, OUT AssemblyInfo * pData);

    HRESULT STDMETHODCALLTYPE GetModuleForAssembly(VMPTR_Assembly vmAssembly, OUT VMPTR_Module * pModule);

    // Yields true if the address is a CLR stub.
    HRESULT STDMETHODCALLTYPE IsTransitionStub(CORDB_ADDRESS address, OUT BOOL * pResult);

    // Get the "type" of address.
    HRESULT STDMETHODCALLTYPE GetAddressType(CORDB_ADDRESS address, OUT AddressType * pRetVal);


    // Enumerate the assemblies in the appdomain.
    HRESULT STDMETHODCALLTYPE EnumerateAssembliesInAppDomain(VMPTR_AppDomain vmAppDomain, FP_ASSEMBLY_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData);

    // Enumerate the moduels in the given assembly.
    HRESULT STDMETHODCALLTYPE EnumerateModulesInAssembly(VMPTR_Assembly vmAssembly, FP_MODULE_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData);

    // When stopped at an event, request a synchronization.
    HRESULT STDMETHODCALLTYPE RequestSyncAtEvent();

    //sets flag Debugger::m_sendExceptionsOutsideOfJMC on the LS
    HRESULT STDMETHODCALLTYPE SetSendExceptionsOutsideOfJMC(BOOL sendExceptionsOutsideOfJMC);

    // Notify the debuggee that a debugger attach is pending.
    HRESULT STDMETHODCALLTYPE MarkDebuggerAttachPending();

    // Notify the debuggee that a debugger is attached.
    HRESULT STDMETHODCALLTYPE MarkDebuggerAttached(BOOL fAttached);

    // Enumerate connections in the process.
    void EnumerateConnections(FP_CONNECTION_CALLBACK fpCallback, void * pUserData);

    HRESULT STDMETHODCALLTYPE EnumerateThreads(FP_THREAD_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData);

    HRESULT STDMETHODCALLTYPE IsThreadMarkedDead(VMPTR_Thread vmThread, OUT BOOL * pResult);

    // Return the handle of the specified thread.
    HRESULT STDMETHODCALLTYPE GetThreadHandle(VMPTR_Thread vmThread, OUT HANDLE * pRetVal);

    // Return the object handle for the managed Thread object corresponding to the specified thread.
    HRESULT STDMETHODCALLTYPE GetThreadObject(VMPTR_Thread vmThread, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Get the alocated bytes for this thread.
    HRESULT STDMETHODCALLTYPE GetThreadAllocInfo(VMPTR_Thread vmThread, DacThreadAllocInfo* threadAllocInfo);

    // Set and reset the TSNC_DebuggerUserSuspend bit on the state of the specified thread
    // according to the CorDebugThreadState.
    HRESULT STDMETHODCALLTYPE SetDebugState(VMPTR_Thread vmThread, CorDebugThreadState debugState);

    // Returns TRUE if there is a current exception which is unhandled
    HRESULT STDMETHODCALLTYPE HasUnhandledException(VMPTR_Thread vmThread, OUT BOOL * pResult);

    // Return the user state of the specified thread.
    HRESULT STDMETHODCALLTYPE GetUserState(VMPTR_Thread vmThread, OUT CorDebugUserState * pRetVal);

    // Returns the user state of the specified thread except for USER_UNSAFE_POINT.
    HRESULT STDMETHODCALLTYPE GetPartialUserState(VMPTR_Thread vmThread, OUT CorDebugUserState * pRetVal);

    // Return the connection ID of the specified thread.
    HRESULT STDMETHODCALLTYPE GetConnectionID(VMPTR_Thread vmThread, OUT CONNID * pRetVal);

    // Return the task ID of the specified thread.
    HRESULT STDMETHODCALLTYPE GetTaskID(VMPTR_Thread vmThread, OUT TASKID * pRetVal);

    // Return the OS thread ID of the specified thread
    HRESULT STDMETHODCALLTYPE TryGetVolatileOSThreadID(VMPTR_Thread vmThread, OUT DWORD * pRetVal);

    // Return the unique thread ID of the specified thread.
    HRESULT STDMETHODCALLTYPE GetUniqueThreadID(VMPTR_Thread vmThread, OUT DWORD * pRetVal);

    // Return the object handle to the managed Exception object of the current exception
    // on the specified thread.  The return value could be NULL if there is no current exception.
    HRESULT STDMETHODCALLTYPE GetCurrentException(VMPTR_Thread vmThread, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Return the object handle to the managed object for a given CCW pointer.
    HRESULT STDMETHODCALLTYPE GetObjectForCCW(CORDB_ADDRESS ccwPtr, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Return the object handle to the managed CustomNotification object of the current notification
    // on the specified thread.  The return value could be NULL if there is no current notification.
    // This will return non-null if and only if we are currently inside a CustomNotification Callback
    // (or a dump was generated while in this callback)
    HRESULT STDMETHODCALLTYPE GetCurrentCustomDebuggerNotification(VMPTR_Thread vmThread, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Return the current appdomain
    HRESULT STDMETHODCALLTYPE GetCurrentAppDomain(OUT VMPTR_AppDomain * pRetVal);

    // Given an assembly ref token and metadata scope (via the Assembly), resolve the assembly.
    HRESULT STDMETHODCALLTYPE ResolveAssembly(VMPTR_Assembly vmScope, mdToken tkAssemblyRef, OUT VMPTR_Assembly * pRetVal);

    // Hijack the thread
    HRESULT STDMETHODCALLTYPE Hijack(VMPTR_Thread vmThread, ULONG32 dwThreadId, const EXCEPTION_RECORD * pRecord, T_CONTEXT * pOriginalContext, ULONG32 cbSizeContext, EHijackReason::EHijackReason reason, void * pUserData, CORDB_ADDRESS * pRemoteContextAddr);

    // Return the filter CONTEXT on the LS.
    HRESULT STDMETHODCALLTYPE GetManagedStoppedContext(VMPTR_Thread vmThread, OUT VMPTR_CONTEXT * pRetVal);

    // Create and return a stackwalker on the specified thread.
    HRESULT STDMETHODCALLTYPE CreateStackWalk(VMPTR_Thread vmThread, DT_CONTEXT * pInternalContextBuffer, OUT StackWalkHandle * ppSFIHandle);

    // Delete the stackwalk object
    HRESULT STDMETHODCALLTYPE DeleteStackWalk(StackWalkHandle ppSFIHandle);

    // Get the CONTEXT of the current frame at which the stackwalker is stopped.
    HRESULT STDMETHODCALLTYPE GetStackWalkCurrentContext(StackWalkHandle pSFIHandle, DT_CONTEXT * pContext);

    void GetStackWalkCurrentContext(StackFrameIterator * pIter, DT_CONTEXT * pContext);

    // Set the stackwalker to the specified CONTEXT.
    HRESULT STDMETHODCALLTYPE SetStackWalkCurrentContext(VMPTR_Thread vmThread, StackWalkHandle pSFIHandle, CorDebugSetContextFlag flag, DT_CONTEXT * pContext);

    // Unwind the stackwalker to the next frame.
    HRESULT STDMETHODCALLTYPE UnwindStackWalkFrame(StackWalkHandle pSFIHandle, OUT BOOL * pResult);

    HRESULT STDMETHODCALLTYPE CheckContext(VMPTR_Thread       vmThread,
                         const DT_CONTEXT * pContext);

    // Retrieve information about the current frame from the stackwalker.
    HRESULT STDMETHODCALLTYPE GetStackWalkCurrentFrameInfo(StackWalkHandle pSFIHandle, OPTIONAL DebuggerIPCE_STRData * pFrameData, OUT FrameType * pRetVal);

    // Return the number of internal frames on the specified thread.
    HRESULT STDMETHODCALLTYPE GetCountOfInternalFrames(VMPTR_Thread vmThread, OUT ULONG32 * pRetVal);

    // Enumerate the internal frames on the specified thread and invoke the provided callback on each of them.
    HRESULT STDMETHODCALLTYPE EnumerateInternalFrames(VMPTR_Thread vmThread, FP_INTERNAL_FRAME_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData);

    // Given the FramePointer of the parent frame and the FramePointer of the current frame,
    // check if the current frame is the parent frame.
    HRESULT STDMETHODCALLTYPE IsMatchingParentFrame(FramePointer fpToCheck, FramePointer fpParent, OUT BOOL * pResult);

    // Return the stack parameter size of the given method.
    HRESULT STDMETHODCALLTYPE GetStackParameterSize(CORDB_ADDRESS controlPC, OUT ULONG32 * pRetVal);

    // Return the stack parameter size of the given method.
    ULONG32 GetStackParameterSize(EECodeInfo * pCodeInfo);

    // Return the FramePointer of the current frame at which the stackwalker is stopped.
    HRESULT STDMETHODCALLTYPE GetFramePointer(StackWalkHandle pSFIHandle, OUT FramePointer * pRetVal);

    FramePointer GetFramePointerWorker(StackFrameIterator * pIter);

    // Return TRUE if the specified CONTEXT is the CONTEXT of the leaf frame.
    // @dbgtodo  filter CONTEXT - Currently we check for the filter CONTEXT first.
    HRESULT STDMETHODCALLTYPE IsLeafFrame(VMPTR_Thread vmThread, const DT_CONTEXT * pContext, OUT BOOL * pResult);

    // DacDbi API: Get the context for a particular thread of the target process
    HRESULT STDMETHODCALLTYPE GetContext(VMPTR_Thread vmThread, DT_CONTEXT * pContextBuffer);

    // This is a simple helper function to convert a CONTEXT to a DebuggerREGDISPLAY.  We need to do this
    // inside DDI because the RS has no notion of REGDISPLAY.
    HRESULT STDMETHODCALLTYPE ConvertContextToDebuggerRegDisplay(const DT_CONTEXT * pInContext, DebuggerREGDISPLAY * pOutDRD, BOOL fActive);

    // Check if the given method is a DiagnosticHidden or an LCG method.
    HRESULT STDMETHODCALLTYPE IsDiagnosticsHiddenOrLCGMethod(VMPTR_MethodDesc vmMethodDesc, OUT DynamicMethodType * pRetVal);

    // Return a TargetBuffer for the raw vararg signature.
    HRESULT STDMETHODCALLTYPE GetVarArgSig(CORDB_ADDRESS VASigCookieAddr, OUT CORDB_ADDRESS * pArgBase, OUT TargetBuffer * pRetVal);

    // returns TRUE if the type requires 8-byte alignment
    HRESULT STDMETHODCALLTYPE RequiresAlign8(VMPTR_TypeHandle thExact, OUT BOOL * pResult);

    // Resolve the raw generics token to the real generics type token.  The resolution is based on the
    // given index.
    HRESULT STDMETHODCALLTYPE ResolveExactGenericArgsToken(DWORD dwExactGenericArgsTokenIndex, GENERICS_TYPE_TOKEN rawToken, OUT GENERICS_TYPE_TOKEN * pRetVal);

    // Returns a bitfield reflecting the managed debugging state at the time of
    // the jit attach.
    HRESULT STDMETHODCALLTYPE GetAttachStateFlags(OUT CLR_DEBUGGING_PROCESS_FLAGS * pRetVal);

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


    // Metadata lookups is just a property on the PEAssembly in the normal builds,
    // and so VM code tends to access the same metadata importer many times in a row.
    // Cache the most-recently used to avoid excessive redundant lookups.

    // PEAssembly of Cached Importer. Invalidated between Flush calls. If this is Non-null,
    // then the importer is m_pCachedImporter, and we can avoid using IMetaDataLookup
    VMPTR_PEAssembly m_pCachedPEAssembly;

    // Value of cached importer, corresponds with m_pCachedPEAssembly.
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
    HRESULT STDMETHODCALLTYPE GetDebuggerControlBlockAddress(OUT CORDB_ADDRESS * pRetVal);

    // Creates a VMPTR of an Object from a target address
    HRESULT STDMETHODCALLTYPE GetObject(CORDB_ADDRESS ptr, OUT VMPTR_Object * pRetVal);

    // Creates a VMPTR of an Object from a target address pointing to an OBJECTREF
    HRESULT STDMETHODCALLTYPE GetObjectFromRefPtr(CORDB_ADDRESS ptr, OUT VMPTR_Object * pRetVal);

    // Get the target address from a VMPTR_OBJECTHANDLE, i.e., the handle address
    HRESULT STDMETHODCALLTYPE GetHandleAddressFromVmHandle(VMPTR_OBJECTHANDLE vmHandle, OUT CORDB_ADDRESS * pRetVal);

    // Gets the target address of an VMPTR of an Object
    HRESULT STDMETHODCALLTYPE GetObjectContents(VMPTR_Object obj, OUT TargetBuffer * pRetVal);

    // Create a VMPTR_OBJECTHANDLE from a CORDB_ADDRESS pointing to an object handle
    HRESULT STDMETHODCALLTYPE GetVmObjectHandle(CORDB_ADDRESS handleAddress, OUT VMPTR_OBJECTHANDLE * pRetVal);

    // Validate that the VMPTR_OBJECTHANDLE refers to a legitimate managed object
    HRESULT STDMETHODCALLTYPE IsVmObjectHandleValid(VMPTR_OBJECTHANDLE vmHandle, OUT BOOL * pResult);

    // if the specified module is a WinRT module then isWinRT will equal TRUE
    HRESULT STDMETHODCALLTYPE IsWinRTModule(VMPTR_Module vmModule, BOOL * pIsWinRT);

private:
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
    // API for picking up the info needed for a debugger to look up an image from its search path.
    HRESULT STDMETHODCALLTYPE GetMetaDataFileInfoFromPEFile(VMPTR_PEAssembly vmPEAssembly, DWORD * pTimeStamp, DWORD * pImageSize, IStringHolder* pStrFilename, OUT BOOL * pResult);
};


// Global allocator for DD. Access is protected under the g_dacMutex lock.
extern "C" IDacDbiInterface::IAllocator * g_pAllocator;


class DDHolder
{
public:
    DDHolder(DacDbiInterfaceImpl* pContainer, bool fAllowReentrant)
    {
        minipal_mutex_enter(&g_dacMutex);

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

        minipal_mutex_leave(&g_dacMutex);
    }

protected:
    DacDbiInterfaceImpl * m_pOldContainer;
    IDacDbiInterface::IAllocator * m_pOldAllocator;
};


// Use this macro at the start of each DD function.
// "MAY_THROW" refers to the code within the function body (inside EX_TRY blocks) that may throw;
// the methods themselves catch all exceptions via EX_CATCH_HRESULT and return HRESULT to callers.
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
    DacRefWalker(ClrDataAccess *dac, BOOL walkStacks, BOOL walkFQ, UINT32 handleMask, BOOL resolvePointers);
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
    BOOL mResolvePointers;

    // Handles
    DacHandleWalker *mHandleWalker;

    // FQ
    PTR_PTR_Object mFQStart;
    PTR_PTR_Object mFQEnd;
    PTR_PTR_Object mFQCurr;
};

#endif // _DACDBI_IMPL_H_
