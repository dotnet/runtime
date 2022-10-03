// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: DacDbiStructures.h
//

//
// Declarations and inline functions for data structures shared between by the
// DAC/DBI interface functions and the right side.
//
// Note that for MAC these structures are marshalled between Windows and Mac
// and so their layout and size must be identical in both builds.  Use the
// MSLAYOUT macro on every structure to avoid compiler packing differences.
//
//*****************************************************************************

#ifndef DACDBISTRUCTURES_H_
#define DACDBISTRUCTURES_H_

#include "./common.h"

//-------------------------------------------------------------------------------
// classes shared by the DAC/DBI interface functions and the right side
//-------------------------------------------------------------------------------

// DacDbiArrayList encapsulates an array and the number of elements in the array.
// Notes:
// - storage is always on the DacDbi heap
// - this class owns the memory. Its dtor will free.
// - Operations that initialize list elements use the assignment
//   operator defined for type T.  If T is a pointer type or has pointer
//   type components and no assignment operator override, this will make a shallow copy of
//   the element. If T has an assignment operator override that makes a deep copy of pointer
//   types, T must also have a destructor that will deallocate any memory allocated.
// - this is NOT thread safe!!!
// - the array elements are always mutable, but the number of elements is fixed between allocations
// - you can gain access to the array using &(list[0]) but this is NOT safe if the array is empty. You
//   can call IsEmpty to determine if it is safe to access the array portion
//   This list is not designed to have unused elements at the end of the array (extra space) nor to be growable

// usage examples:
// typedef DacDbiArrayList<Bar> BarList;       // handy typedef
// void GetAListOfBars(BarList * pBarList)
// {
//     DacDbiArrayList<Foo> fooList;   // fooList is an empty array of objects of type Foo
//     int elementCount = GetNumberOfFoos();
//     Bar * pBars = new Bar[elementCount];
//
//     fooList.Alloc(elementCount);            // get space for the list of Foo instances
//     for (int i = 0; i < fooList.Count(); ++i)
//     {
//        fooList[i] = GetNextFoo();           // copy elements into the list
//     }
//     ConvertFoosToBars(pBars, &fooList);     // always pass by reference
//     pBarList->Init(pBars, fooList.Count()); // initialize  a list
// }
//
// void ConvertFoosToBars(Bar * pBars, DacDbiArrayList<Foo> * pFooList)
// {
//    for (int i = 0; i < pFooList->Count(); ++i)
//    {
//        if ((*pFooList)[i].IsBaz())
//            pBars [i] = ConvertBazToBar(&(*pFooList)[i]);
//        else pBars [i] = (*pFooList)[i].barPart;
//    }
// }
//
template<class T>
class MSLAYOUT DacDbiArrayList
{
public:
    // construct an empty list
    DacDbiArrayList();

    // deep copy constructor
    DacDbiArrayList(const T * list, int count);

    // destructor--sets list to empty state
    ~DacDbiArrayList();

    // explicitly deallocate the list and set it back to the empty state
    void Dealloc();

    // allocate a list with space for nElements items
    void Alloc(int nElements);

    // allocate and initialize a DacDbiArrayList from an array of type T and a count
    void Init(const T * list, int count);

    // predicate to indicate if the list is empty
    bool IsEmpty() { return m_nEntries == 0; }

    // read-only element accessor
    const T & operator [](int index) const;

    // writeable element accessor
    T & operator [](int index);


    // returns the number of elements in the list
    unsigned int Count() const;

    // @dbgtodo  Mac - cleaner way to expose this for serialization?
    void PrepareForDeserialize()
    {
        m_pList = NULL;
    }
private:
    // because these are private (and unimplemented), calls will generate a compiler (or linker) error.
    // This prevents accidentally invoking the default (shallow) copy ctor or assignment operator.
    // This prevents having multiple instances point to the same list memory (eg. due to passing by value),
    // which would result in memory corruption when the first copy is destroyed and the list memory is deallocated.
    DacDbiArrayList(const DacDbiArrayList<T> & sourceList);
    T & operator = (const DacDbiArrayList<T> & rhs);

// data members
protected:
    T *  m_pList;           // the list

    // - the count is managed by the member functions and is not settable, so (m_pList == NULL) == (m_nEntries == 0)
    //   is always true.
    int  m_nEntries;        // the number of items in the list

};


// Describes a buffer in the target
struct MSLAYOUT TargetBuffer
{
    TargetBuffer();
    TargetBuffer(CORDB_ADDRESS pBuffer, ULONG cbSizeInput);

    // @dbgtodo : This ctor form confuses target and host address spaces.  This should probably be PTR_VOID instead of void*
    TargetBuffer(void * pBuffer, ULONG cbSizeInput);

    //
    // Helper methods
    //

    // Return a sub-buffer that's starts at byteOffset within this buffer and runs to the end.
    TargetBuffer SubBuffer(ULONG byteOffset) const;

    // Return a sub-buffer that starts at byteOffset within this buffer and is byteLength long.
    TargetBuffer SubBuffer(ULONG byteOffset, ULONG byteLength) const;

    // Returns true if the buffer length is 0.
    bool IsEmpty() const;

    // Sets address to NULL and size to 0
    // IsEmpty() will be true after this.
    void Clear();

    // Initialize fields
    void Init(CORDB_ADDRESS address, ULONG size);

    // Target address of buffer
    CORDB_ADDRESS pAddress;

    // Size of buffer in bytes
    ULONG         cbSize;
};

//===================================================================================
// Module properties, retrieved by DAC.
// Describes a VMPTR_DomainAssembly representing a module.
// In the VM, a raw Module may be domain neutral and shared by many appdomains.
// Whereas a DomainAssembly is like a { AppDomain, Module} pair. DomainAssembly corresponds
// much more to ICorDebugModule (which also has appdomain affinity).
//===================================================================================
struct MSLAYOUT DomainAssemblyInfo
{
    // The appdomain that the DomainAssembly is associated with.
    // Although VMPTR_Module may be shared across multiple domains, a DomainAssembly has appdomain affinity.
    VMPTR_AppDomain vmAppDomain;

    // The assembly this module belongs to. All modules live in an assembly.
    VMPTR_DomainAssembly vmDomainAssembly;
};

struct MSLAYOUT ModuleInfo
{
    // The non-domain specific assembly which this module resides in.
    VMPTR_Assembly vmAssembly;

    // The PE Base address and size of the module. These may be 0 if there is no image
    // (such as for a dynamic module that's not persisted to disk).
    CORDB_ADDRESS pPEBaseAddress;

    // The PEAssembly associated with the module. Every module (even non-file-based ones) has a PEAssembly.
    // This is critical because DAC may ask for a metadata importer via PE-file.
    // a PEAssembly may have 1 or more PEImage child objects (1 for IL, 1 for native image, etc)
    VMPTR_PEAssembly vmPEAssembly;

    // The PE Base address and size of the module. These may be 0 if there is no image
    // (such as for a dynamic module that's not persisted to disk).
    ULONG nPESize;

    // Is this a dynamic (reflection.emit) module?
    // This means that new classes can be added to the module; and so
    // the module's metadata and symbols can be updated. Debugger will have to do extra work
    // to keep up with the updates.
    // Dynamic modules may be transient (entirely in-memory) or persisted to disk (have a file associated with them).
    BOOL  fIsDynamic;

    // Is this an inmemory module?
    // Assemblies can be instantiated purely in-memory from just a Byte[].
    // This means the module (and pdb) are not in files, and thus the debugger
    // needs to do extra work to retrieve them from the Target's memory.
    BOOL  fInMemory;
};

// the following two classes track native offsets for local variables and sequence
// points. This information is initialized on demand.


//===================================================================================
// NativeVarData holds a list of structs that provide the following information for
// each local variable and fixed argument in a function: the offsets between which the
// variable or argument lives in a particular location, the location itself, and the
// variable number (ID). This allows us to determine where a value is at any given IP.

// Lifetime management of the list is the responsibility of the NativeVarData class.
// Callers that allocate memory for a new list should NOT maintain a separate pointer
// to the list.

// The arguments we track are the "fixed" arguments, specifically, the explicit arguments
// that appear in the source code and the "this" pointer for non-static methods.
// Varargs and other implicit arguments, such as the generic handle are counted in
// CordbJITILFrame::m_allArgsCount.

// Although logically, we really don't differentiate between arguments and locals when
// all we want to know is where to find a value, we need to have two
// separate counts. The full explanation is in the comment in rsthread.cpp in
// CordbJITILFrame::ILVariableToNative, but the short version is that it allows us to
// compute the correct ID for a value.

// m_fixedArgsCount, accessed through GetFixedArgCount, is the actual number of fixed
// arguments.
// m_allArgsCount, accessed through GetAllArgsCount, is the number of fixed args plus the
// number of varargs.

// The number of entries in m_offsetInfo, accessed through Count(), is NOT the
// number of locals, nor the number of locals plus the number of arguments. It is the
// number of entries in the list. Any particular value may have an arbitrary number of
// entries, depending on how many different places it is stored during the execution of
// the method. The list is not sorted, so searches for data within it must be linear.
//===================================================================================
class MSLAYOUT NativeVarData
{
public:
    // constructor
    NativeVarData();
    // destructor
    ~NativeVarData();


    // initialize the list of native var information structures, including the starting address of the list
    // (m_pOffsetInfo, the number of entries (m_count) and the number of fixed args (m_fixedArgsCount).
    // NativeVarData will manage the lifetime of the allocated memory for the list, so the caller should not
    // hold on to its address.
    void InitVarDataList(ICorDebugInfo::NativeVarInfo * plistStart, int fixedArgCount, int entryCount);

private:
    // non-existent copy constructor to disable the (shallow) compiler-generated
    // one. If you attempt to use this, you will get a compiler or linker error.
    NativeVarData(const NativeVarData & rhs) {};

    // non-existent assignment operator to disable the (shallow) compiler-generated
    // one. If you attempt to use this, you will get a compiler or linker error.
    NativeVarData & operator=(const NativeVarData & rhs);

//----------------------------------------------------------------------------------
// Accessor Functions
//----------------------------------------------------------------------------------
public:

    // get the list of native offset info
    const DacDbiArrayList<ICorDebugInfo::NativeVarInfo> * GetOffsetInfoList() const
    {
        _ASSERTE(m_fInitialized);
        return &m_offsetInfo;
    }

    // get the number of explicit arguments for this function--this
    // includes the fixed arguments for vararg methods, but not the variable ones
    ULONG32 GetFixedArgCount()
    {
        _ASSERTE(IsInitialized());
        // this count includes explicit arguments plus one for the "this" pointer
        // but doesn't count varargs
        return m_fixedArgsCount;
    }

    // get the number of all arguments, including varargs
    ULONG32 GetAllArgsCount()
    {
        _ASSERTE(IsInitialized());
        return m_allArgsCount;
    }

    // set the number of all arguments, including varargs
    void SetAllArgsCount(ULONG32 count)
    {
        m_allArgsCount = count;
    }

    // determine whether we have successfully initialized this
    BOOL IsInitialized()
    {
        return m_fInitialized == true;
    }


//----------------------------------------------------------------------------------
// Data Members
//----------------------------------------------------------------------------------

// @dbgtodo  Mac - making this public for serializing for remote DAC on mac. Need to make this private again.
public:
    // contains a list of structs providing information about the location of a local
    // variable or argument between a pair of offsets and the number of entries in the list
    DacDbiArrayList<ICorDebugInfo::NativeVarInfo> m_offsetInfo;

    // number of fixed arguments to the function i.e., the explicit arguments and "this" pointer
    ULONG32                                     m_fixedArgsCount;

    // number of fixed arguments plus number of varargs
    ULONG32                                     m_allArgsCount;

    // indicates whether an attempt has been made to initialize the var data already
    bool                                        m_fInitialized;
}; // class NativeVarData

//===================================================================================
// SequencePoints holds a list of sequence points that map IL offsets to native offsets. In addition,
// it keeps track of the number of entries in the list and whether the list is sorted.
//===================================================================================
class MSLAYOUT SequencePoints
{
public:
    SequencePoints();

    ~SequencePoints();

    // Initialize the m_pMap data member to the address of an allocated chunk
    // of memory (or to NULL if the count is zero). Set m_count as the
    // number of entries in the map.
    void InitSequencePoints(ULONG32 count);

private:
    // non-existent copy constructor to disable the (shallow) compiler-generated
    // one. If you attempt to use this, you will get a compiler or linker error.
    SequencePoints(const SequencePoints & rhs) {};

    // non-existent assignment operator to disable the (shallow) compiler-generated
    // one. If you attempt to use this, you will get a compiler or linker error.
    SequencePoints & operator=(const SequencePoints & rhs);

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

//----------------------------------------------------------------------------------
// Accessor Functions
//----------------------------------------------------------------------------------
public:
    // @dbgtodo Microsoft inspection: It would be very nice not to need this at all. Ideally,
    // it would be better to make ExportILToNativeMap expect a DacDbiArrayList instead of the
    // array and size. At present, there's a call to ExportILToNativeMap in debugger.cpp where
    // DacDbiArrayLists aren't available, so at present, we need to pass the array and size.
    // We should be able to eliminate the debugger.cpp call when we get rid of in-proc
    // inspection. At that point, we can delete this function too, as well as GetEntryCount.
    // In the meantime, it would be great if no one else took a dependency on this.

    // get value of m_pMap
    DebuggerILToNativeMap * GetMapAddr()
    {
        // Please don't call this function
       _ASSERTE(m_fInitialized);
       return &(m_map[0]);
    }

    // get value of m_count
    ULONG32 GetEntryCount()
    {
        _ASSERTE(m_fInitialized);
        return m_mapCount;
    }

    ULONG32 GetCallsiteEntryCount()
    {
        _ASSERTE(m_fInitialized);
        return m_map.Count() - m_mapCount; //m_map.Count();
    }

    DebuggerILToNativeMap * GetCallsiteMapAddr()
    {
        // Please don't call this function
       _ASSERTE(m_fInitialized);

       if (m_map.Count() == m_mapCount)
          return NULL;

       return &(m_map[m_mapCount]);
    }



    // determine whether we have initialized this
    BOOL IsInitialized()
    {
        return m_fInitialized == true;
    }

    // Copy data from the VM map data to our own map structure and sort. The
    // information comes to us in a data structure that differs slightly from the
    // one we use out of process, so we have to copy it to the right-side struct.
    void CopyAndSortSequencePoints(const ICorDebugInfo::OffsetMapping  mapCopy[]);


    // Set the IL offset of the last sequence point before the epilog.
    // If a native offset maps to the epilog, we will return the this IL offset.
    void SetLastILOffset(ULONG32 lastILOffset)
    {
        _ASSERTE(m_fInitialized);
        m_lastILOffset = lastILOffset;
    }

    // Map the given native offset to IL offset.  Also return the mapping type.
    DWORD MapNativeOffsetToIL(DWORD dwNativeOffset,
                              CorDebugMappingResult *pMapType);

//----------------------------------------------------------------------------------
// Data Members
//----------------------------------------------------------------------------------

    // @dbgtodo  Mac - making this public for serializing for remote DAC on mac. Need to make this private again.
public:

    // map of IL to native offsets for sequence points
    DacDbiArrayList<DebuggerILToNativeMap> m_map;

    //
    ULONG32 m_mapCount;

    // the IL offset of the last sequence point before the epilog
    ULONG32                                m_lastILOffset;
    // indicates whether an attempt has been made to initialize the sequence points already
    bool                                   m_fInitialized;
}; // class SequencePoints

//----------------------------------------------------------------------------------
// declarations needed for getting native code regions
//----------------------------------------------------------------------------------

// Code may be split into Hot & Cold regions, so we need an extra address & size.
// The jitter doesn't do this optimization w/ debuggable code, so we'll
// rarely see the cold region information as non-null values.

// This enumeration provides symbolic indices into m_rgCodeRegions.
typedef enum {kHot = 0, kCold, MAX_REGIONS} CodeBlobRegion;

// This contains the information we need to initialize a CordbNativeCode object
class MSLAYOUT NativeCodeFunctionData
{
public:
    // set all fields to default values (NULL, FALSE, or zero as appropriate)
    NativeCodeFunctionData();

    // conversion constructor to convert from an instance of DebuggerIPCE_JITFUncData to an instance of
    // NativeCodeFunctionData.
    NativeCodeFunctionData(DebuggerIPCE_JITFuncData * source);

    // The hot region start address could be NULL in the following circumstances:
    // 1. We haven't yet tried to get the information
    // 2. We tried to get the information, but the function hasn't been jitted yet
    // 3. We tried to get the information, but the MethodDesc wasn't available yet (very early in
    //    module initialization), which implies that the code isn't available either.
    // 4. We tried to get the information, but a method edit has reset the MethodDesc, but the
    //    method hasn't been jitted yet.
    // In all cases, we can check the hot region start address to determine whether the rest of the
    // the information is valid.
    BOOL IsValid() { return (m_rgCodeRegions[kHot].pAddress != NULL); }
    void Clear();

    // data members
    // start addresses and sizes of hot & cold regions
    TargetBuffer     m_rgCodeRegions[MAX_REGIONS];

    // indicates whether the function is a generic function, or a method inside a generic class (or both).
    BOOL             isInstantiatedGeneric;

    // MethodDesc for the function
    VMPTR_MethodDesc vmNativeCodeMethodDescToken;

    // EnC version number of the function
    SIZE_T           encVersion;
};

//----------------------------------------------------------------------------------
// declarations needed for getting type information
//----------------------------------------------------------------------------------

// FieldData holds data for each field within a class or type. This data
// is passed from the DAC to the DI in response to a request for class info.
// This type is also used by CordbClass and CordbType to hold the list of fields for the
// class.
class MSLAYOUT FieldData
{
public:
#ifndef RIGHT_SIDE_COMPILE
    // initialize various fields of an instance of FieldData from information in a FieldDesc
    void Initialize(BOOL fIsStatic, BOOL fIsPrimitive, mdFieldDef mdToken);
#else
    HRESULT GetFieldSignature(class CordbModule * pModule, /*OUT*/ SigParser * pSigParser);
#endif

    // clear various fields for a new instance of FieldData
    void ClearFields();

    // Make sure it's okay to get or set an instance field offset.
    BOOL OkToGetOrSetInstanceOffset();

    // Make sure it's okay to get or set a static field address.
    BOOL OkToGetOrSetStaticAddress();

    // If this is an instance field, store its offset
    void SetInstanceOffset( SIZE_T offset );

    // If this is a "normal" static, store its absolute address
    void SetStaticAddress( TADDR addr );

    // If this is an instance field, return its offset
    // Note that this offset is always a real offset (possibly larger than 22 bits), which isn't
    // necessarily the same as the overloaded FieldDesc.dwOffset field which can have
    // some special FIELD_OFFSET tokens.
    SIZE_T  GetInstanceOffset();

    // If this is a "normal" static, get its absolute address
    // TLS and context-specific statics are "special".
    TADDR GetStaticAddress();

//
// Data members
//
    mdFieldDef      m_fldMetadataToken;
    // m_fFldStorageAvailable is true whenever the storage for this field is available.
    // If this is a field that is newly added with EnC and hasn't had any storage
    // allocated yet, then fldEnCAvailable will be false.
    BOOL            m_fFldStorageAvailable;

    // Bits that specify what type of field this is
    bool            m_fFldIsStatic;           // true if static field, false if instance field
    bool            m_fFldIsRVA;              // true if static relative to module address
    bool            m_fFldIsTLS;              // true if thread-specific static
    bool            m_fFldIsPrimitive;        // Only true if this is a value type masquerading as a primitive.
    bool            m_fFldIsCollectibleStatic; // true if this is a static field on a collectible type

private:
    // The m_fldInstanceOffset and m_pFldStaticAddress are mutually exclusive. Only one is ever set at a time.
    SIZE_T          m_fldInstanceOffset;      // The offset of a field within an object instance
                                              // For EnC fields, this isn't actually within the object instance,
                                              // but has been cooked to still be relative to the beginning of
                                              // the object.
    TADDR           m_pFldStaticAddress;      // The absolute target address of a static field

    PCCOR_SIGNATURE m_fldSignatureCache;      // This is passed across as null. It is a RS-only cache, and SHOULD
                                              // NEVER BE ACCESSED DIRECTLY!
    ULONG           m_fldSignatureCacheSize;  // This is passed across as 0. It is a RS-only cache, and SHOULD
                                              // NEVER BE ACCESSED DIRECTLY!
public:
    VMPTR_FieldDesc m_vmFieldDesc;

}; // class FieldData


// ClassInfo holds information about a type (class or other structured type), including a list of its fields
class MSLAYOUT ClassInfo
{
public:
    ClassInfo();

    ~ClassInfo();

    void Clear();

    // Size of object in bytes, for non-generic types. Note: this is NOT valid for constructed value types,
    // e.g. value type Pair<DateTime,int>.  Use CordbType::m_objectSize instead.
    SIZE_T                     m_objectSize;

    // list of structs containing information about all the fields in this Class, along with the number of entries
    // in the list. Does not include inherited fields. DON'T KEEP POINTERS TO ELEMENTS OF m_fieldList AROUND!!
                                                // This may be deleted if the class gets EnC'd.
    DacDbiArrayList<FieldData> m_fieldList;
}; // class ClassInfo

// EnCHangingFieldInfo holds information describing a field added with Edit And Continue. This data
// is passed from the DAC to the DI in response to a request for EnC field info.
class MSLAYOUT EnCHangingFieldInfo
{
public:
    // Init will initialize fields, taking into account whether the field is static or not.
    void Init(VMPTR_Object     pObject,
              SIZE_T           offset,
              mdFieldDef       fieldToken,
              CorElementType   elementType,
              mdTypeDef        metadataToken,
              VMPTR_DomainAssembly vmDomainAssembly);

    DebuggerIPCE_BasicTypeData GetObjectTypeData() const { return m_objectTypeData; };
    mdFieldDef GetFieldToken() const { return m_fldToken; };
    VMPTR_Object GetVmObject() const { return m_vmObject; };
    SIZE_T GetOffsetToVars() const { return m_offsetToVars; };

private:
    DebuggerIPCE_BasicTypeData m_objectTypeData; // type data for the EnC field
    VMPTR_Object               m_vmObject;        // object instance to which the field has been added--if the field is
                                                 // static, this will be NULL instead of pointing to an instance
    SIZE_T                     m_offsetToVars;   // offset to the beginning of variable storage in the object
    mdFieldDef                 m_fldToken;       // metadata token for the added field

}; // EnCHangingFieldInfo

// TypeHandleToExpandedTypeInfo returns different DebuggerIPCE_ExpandedTypeData objects
// depending on whether the object value that the TypeData corresponds to is
// boxed or not.  Different parts of the API transfer objects in slightly different ways.
// AllBoxed:
//    For GetAndSendObjectData all values are boxed,
//
// OnlyPrimitivesUnboxed:
//     When returning results from FuncEval only "true" structs
//     get boxed, i.e. primitives are unboxed.
//
// NoValueTypeBoxing:
//     TypeHandleToExpandedTypeInfo is also used to report type parameters,
//      and in this case none of the types are considered boxed (
enum AreValueTypesBoxed { NoValueTypeBoxing, OnlyPrimitivesUnboxed, AllBoxed };

// TypeRefData is used for resolving a type reference (see code:CordbModule::ResolveTypeRef and
// code:DacDbiInterfaceImpl::ResolveTypeReference) to store relevant information about the type
typedef struct MSLAYOUT
{
    // domain file for the type
    VMPTR_DomainAssembly vmDomainAssembly;
    // metadata token for the type. This may be a typeRef (for requests) or a typeDef (for responses).
    mdToken          typeToken;
} TypeRefData;

// @dbgtodo Microsoft inspection: get rid of IPCE type.
// TypeInfoList encapsulates a list of type data instances and the length of the list.
typedef DacDbiArrayList<DebuggerIPCE_TypeArgData> TypeInfoList;

// ArgInfoList encapsulates a list of type data instances for arguments for a top-level
// type and the length of the list.
typedef DacDbiArrayList<DebuggerIPCE_BasicTypeData> ArgInfoList;

// TypeParamsList encapsulate a list of type parameters and the length of the list
typedef DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> TypeParamsList;

// A struct for passing version information from DBI to DAC.
// See code:CordbProcess::CordbProcess#DBIVersionChecking for more information.
const DWORD kCurrentDacDbiProtocolBreakingChangeCounter = 1;

struct DbiVersion
{
    DWORD m_dwFormat;               // the format of this DbiVersion instance
    DWORD m_dwDbiVersionMS;         // version of the DBI DLL, in the convention used by VS_FIXEDFILEINFO
    DWORD m_dwDbiVersionLS;
    DWORD m_dwProtocolBreakingChangeCounter;  // initially this was reserved and always set to 0
	                                          // Now we use it as a counter to explicitly introduce breaking changes
	                                          // between DBI and DAC when we have our IPC transport in the middle
	                                          // If DBI and DAC don't agree on the same value CheckDbiVersion will return CORDBG_E_INCOMPATIBLE_PROTOCOL
	                                          // Please document every time this value changes
	                                          // 0 - initial value
	                                          // 1 - Indicates that the protocol now supports the GetRemoteInterfaceHashAndTimestamp message
	                                          //     The message must have ID 2, with signature:
	                                          //     OUT DWORD & hash1, OUT DWORD & hash2, OUT DWORD & hash3, OUT DWORD & hash4, OUT DWORD & timestamp1, OUT DWORD & timestamp2
	                                          //     The hash can be used as an indicator of many other breaking changes providing
	                                          //     easier automated enforcement during development. It is NOT recommended to use
	                                          //     the hash as a release versioning mechanism however.
    DWORD m_dwReservedMustBeZero1;  // reserved for future use
};

// The way in which a thread is blocking on an object
enum DacBlockingReason
{
    DacBlockReason_MonitorCriticalSection,
    DacBlockReason_MonitorEvent
};

// Information about an object which is blocking a managed thread
struct DacBlockingObject
{
    VMPTR_Object      vmBlockingObject;
    VMPTR_AppDomain   vmAppDomain;
    DWORD             dwTimeout;
    DacBlockingReason blockingReason;
};

// Opaque user defined data used in callbacks
typedef void* CALLBACK_DATA;

struct MonitorLockInfo
{
    VMPTR_Thread lockOwner;
    DWORD acquisitionCount;
};

struct MSLAYOUT DacGcReference
{
    VMPTR_AppDomain vmDomain;   // The AppDomain of the handle/object, may be null.
    union
    {
        CORDB_ADDRESS pObject;     // A managed object, with the low bit set.
        VMPTR_OBJECTHANDLE objHnd;  // A reference to the object, valid if (pAddress & 1) == 0
    };
    DWORD dwType;           // Where the root came from.

    /*
        DependentSource - for HandleDependent
        RefCount - for HandleStrongRefCount
        Size - for HandleSizedByref
    */
    UINT64 i64ExtraData;
}; // struct DacGcReference

struct MSLAYOUT DacExceptionCallStackData
{
    VMPTR_AppDomain vmAppDomain;
    VMPTR_DomainAssembly vmDomainAssembly;
    CORDB_ADDRESS ip;
    mdMethodDef methodDef;
    BOOL isLastForeignExceptionFrame;
};

// These represent the various states a SharedReJitInfo can be in.
enum DacSharedReJitInfoState
{
    // The profiler has requested a ReJit, so we've allocated stuff, but we haven't
    // called back to the profiler to get any info or indicate that the ReJit has
    // started. (This Info can be 'reused' for a new ReJit if the
    // profiler calls RequestReJit again before we transition to the next state.)
    kStateRequested = 0x00000000,

    // We have asked the profiler about this method via ICorProfilerFunctionControl,
    // and have thus stored the IL and codegen flags the profiler specified. Can only
    // transition to kStateReverted from this state.
    kStateActive = 0x00000001,

    // The methoddef has been reverted, but not freed yet. It (or its instantiations
    // for generics) *MAY* still be active on the stack someplace or have outstanding
    // memory references.
    kStateReverted = 0x00000002,


    kStateMask = 0x0000000F,
};

struct MSLAYOUT DacSharedReJitInfo
{
    DWORD          m_state;
    CORDB_ADDRESS  m_pbIL;
    DWORD          m_dwCodegenFlags;
    ULONG          m_cInstrumentedMapEntries;
    CORDB_ADDRESS  m_rgInstrumentedMapEntries;
};

// These represent the allocated bytes so far on the thread.
struct MSLAYOUT DacThreadAllocInfo
{
    ULONG64 m_allocBytesSOH;
    ULONG64 m_allocBytesUOH;
};

#include "dacdbistructures.inl"
#endif // DACDBISTRUCTURES_H_
