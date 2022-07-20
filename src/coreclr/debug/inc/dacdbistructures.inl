// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: DacDbiInterface.inl
//
// Inline functions for DacDbiStructures.h
//
//*****************************************************************************

#ifndef DACDBISTRUCTURES_INL_
#define DACDBISTRUCTURES_INL_

//-----------------------------------------------------------------------------------
// DacDbiArrayList member function implementations
//-----------------------------------------------------------------------------------

// constructor--sets list to empty state
// Arguments: none
// Notes: this allocates no memory, so the list will not be ready to use
template<class T>
inline
DacDbiArrayList<T>::DacDbiArrayList():
  m_pList(NULL),
  m_nEntries(0)
  {
  }

// conversion constructor--takes a list of type T and a count and converts to a
// DacDbiArrayList
// Arguments:
//      input: list  - a consecutive list (array) of elements of type T
//             count - the number of elements in list
// Notes: - Allocates memory and copies the elements of list into "this"
//        - It is assumed that the list does NOT already have memory allocated; if it does,
//          calling Init will cause a leak.
//        - the element copy relies on the assignment operator for T
//        - may throw OOM
template<class T>
inline
DacDbiArrayList<T>::DacDbiArrayList(const T * pList, int count):
    m_pList(NULL),
    m_nEntries(0)
{
    Init(pList, count);
}

// destructor: deallocates memory and sets list back to empty state
// Arguments: none
template<class T>
inline
DacDbiArrayList<T>::~DacDbiArrayList()
{
    Dealloc();
}

// explicitly deallocate the list and set it back to the empty state
// Arguments: none
// Notes: - Dealloc can be called multiple times without danger, since it
//          checks first that memory has been allocated
template<class T>
inline
void DacDbiArrayList<T>::Dealloc()
{
    CONTRACT_VOID
    {
        NOTHROW;
    }
    CONTRACT_END;

    if (m_pList != NULL)
    {
        DeleteDbiArrayMemory(m_pList, m_nEntries);
        m_pList = NULL;
    }
    m_nEntries = 0;
    RETURN;
}

// Alloc and Init are very similar.  Both preallocate the array; but Alloc leaves the
// contents uninitialized while Init provides initial values. The array contents are always
// mutable.

// allocate space for the list--in some instances, we'll know the count first, and then
// we'll compute the elements one at a time. This (along with the array access operator
// overload) allows us to handle that situation
// Arguments:
//     input: nElements -  number of elements of type T for which we need space
// Notes:
//     - Alloc can be called multiple times and will free previous arrays.
//     - May throw OOM
//     - The array is not expandable, so you must allocate for all the elements at once.
//     - requesting an allocation of 0 or fewer bytes will not cause an error, but no memory is
//       allocated
template<class T>
inline
void DacDbiArrayList<T>::Alloc(int nElements)
{
    Dealloc();
    if (nElements > 0)
    {
        m_pList = new(forDbi) T[(size_t)nElements];
        m_nEntries = nElements;
    }
}

// allocate and initialize a DacDbiArrayList from a list of type T and a count
// Arguments:
//     input: list  - consecutive list (array) of elements of type T to be copied into
//                    "this"
//            count - number of elements in list
// Notes:
//     - May throw OOM
//     - Can be called multiple times with different lists, since this will deallocate
//       previous arrays.
template<class T>
inline
void DacDbiArrayList<T>::Init(const T * pList, int count)
{
    _ASSERTE((m_pList == NULL) && (m_nEntries == 0));
    if (count > 0)
    {
        Alloc(count);
        m_nEntries = count;
        for (int index = 0; index < count; ++index)
        {
            m_pList[index] = pList[index];
        }
    }
}

// read-only list element access
template<class T>
inline
const T & DacDbiArrayList<T>::operator [](int i) const
{
     _ASSERTE(m_pList != NULL);
     _ASSERTE((i >= 0) && (i < m_nEntries));
     return m_pList[i];
}

// writeable list element access
template<class T>
inline
T & DacDbiArrayList<T>::operator [](int i)
{
     _ASSERTE(m_pList != NULL);
     _ASSERTE((i >= 0) && (i < m_nEntries));
     return m_pList[i];
}

// get the number of elements in the list
template<class T>
inline
unsigned int DacDbiArrayList<T>::Count() const
{
    return m_nEntries;
}

//-----------------------------------------------------------------------------
// Target Buffer functions
//-----------------------------------------------------------------------------

// Default ctor
inline
TargetBuffer::TargetBuffer()
{
    this->pAddress = NULL;
    this->cbSize = 0;
}

// Convenience Ctor to initialize around an (Address, size).
inline
TargetBuffer::TargetBuffer(CORDB_ADDRESS pBuffer, ULONG cbSizeInput)
{
    this->pAddress = pBuffer;
    this->cbSize   = cbSizeInput;
}

// Convenience Ctor to initialize around an (Address, size).
inline
TargetBuffer::TargetBuffer(void * pBuffer, ULONG cbSizeInput)
{
    this->pAddress = PTR_TO_CORDB_ADDRESS(pBuffer);
    this->cbSize   = cbSizeInput;
}

// Return a sub-buffer that's starts at byteOffset within this buffer and runs to the end.
//
// Arguments:
//    byteOffset - offset in bytes within this buffer that the new buffer starts at.
//
// Returns:
//    A new buffer that's a subset of the existing buffer.
inline
TargetBuffer TargetBuffer::SubBuffer(ULONG byteOffset) const
{
    _ASSERTE(byteOffset <= cbSize);
    return TargetBuffer(pAddress + byteOffset, cbSize - byteOffset);
}

// Return a sub-buffer that starts at byteOffset within this buffer and is byteLength long.
//
// Arguments:
//    byteOffset - offset in bytes within this buffer that the new buffer starts at.
//    byteLength - length in bytes of the new buffer.
//
// Returns:
//    A new buffer that's a subset of the existing buffer.
inline
TargetBuffer TargetBuffer::SubBuffer(ULONG byteOffset, ULONG byteLength) const
{
    _ASSERTE(byteOffset + byteLength <= cbSize);
    return TargetBuffer(pAddress + byteOffset, byteLength);
}

// Sets address to NULL and size to 0
inline
void TargetBuffer::Clear()
{
    pAddress = NULL;
    cbSize = 0;
}

// Initialize fields
inline
void TargetBuffer::Init(CORDB_ADDRESS address, ULONG size)
{
    pAddress = address;
    cbSize = size;
}


// Returns true iff the buffer is empty.
inline
bool TargetBuffer::IsEmpty() const
{
    return (this->cbSize == 0);
}

//-----------------------------------------------------------------------------
// NativeVarData member function implementations
//-----------------------------------------------------------------------------

// Initialize a new instance of NativeVarData
inline NativeVarData::NativeVarData() :
                  m_allArgsCount(0),
                  m_fInitialized(false)
{
}

// destructor
inline NativeVarData::~NativeVarData()
{
        m_fInitialized = false;
    }

// initialize the list of native var information structures, including the starting address of the list, the number of
// entries and the number of fixed args.
inline void NativeVarData::InitVarDataList(ICorDebugInfo::NativeVarInfo * pListStart,
                                           int                            fixedArgCount,
                                           int                            entryCount)
{
    m_offsetInfo.Init(pListStart, entryCount);
    m_fixedArgsCount = fixedArgCount;
    m_fInitialized = true;
}

//-----------------------------------------------------------------------------
// SequencePoints member function implementations
//-----------------------------------------------------------------------------

// initializing constructor
inline SequencePoints::SequencePoints() :
               m_mapCount(0),
               m_lastILOffset(0),
               m_fInitialized(false)
{
}

// destructor
inline SequencePoints::~SequencePoints()
{
        m_fInitialized = false;
    }

// Initialize the m_pMap data member to the address of an allocated chunk
// of memory (or to NULL if the count is zero). Set m_count as the
// number of entries in the map.
inline void SequencePoints::InitSequencePoints(ULONG32 count)
{
    m_map.Alloc(count),
    m_fInitialized = true;
}

//
// Map the given native offset to IL offset and return the mapping type.
//
// Arguments:
//    dwNativeOffset - the native offset to be mapped
//    pMapType       - out parameter; return the mapping type
//
// Return Value:
//    Return the IL offset corresponding to the given native offset.
//    For a prolog, return 0.
//    For an epilog, return the IL offset of the last sequence point before the epilog.
//    If we can't map to an IL offset, then return 0, with a mapping type of MAPPING_NO_INFO.
//
// Assumptions:
//    The sequence points are sorted.
//

inline
DWORD SequencePoints::MapNativeOffsetToIL(DWORD                  dwNativeOffset,
                                          CorDebugMappingResult *pMapType)
{
    //_ASSERTE(IsInitialized());
    if (!IsInitialized())
    {
        (*pMapType) = MAPPING_NO_INFO;
        return 0;
    }

    _ASSERTE(pMapType != NULL);

    int i;

    for (i = 0; i < (int)m_mapCount; ++i)
    {
        // Check to determine if dwNativeOffset is within this sequence point. Checking the lower bound is trivial--
        // we just make sure that dwNativeOffset >= m_map[i].nativeStartOffset.
        // Checking to be sure it's before the end of the range is a little trickier. We can have
        // m_map[i].nativeEndOffset = 0 for two reasons:
        // 1. We use an end offset of 0 to signify that this end offset is also the end of the method.
        // 2. We could also have an end offset of 0 if the IL prologue doesn't translate to any native
        // instructions. Thus, the first native instruction (which will not be in the prologue) is at an offset
        // of 0. The end offset is always set to the start offset of the next sequence point, so this means
        // that both the start and end offsets of the (non-existent) native instruction range for the
        // prologue is also 0.
        // If the end offset is 0, we want to check if we're in the prologue before concluding that the
        // value of dwNativeOffset is out of range.
        if ((dwNativeOffset >= m_map[i].nativeStartOffset) &&
            (((m_map[i].nativeEndOffset == 0) && (m_map[i].ilOffset != (ULONG)ICorDebugInfo::PROLOG)) ||
             (dwNativeOffset < m_map[i].nativeEndOffset)))
        {
            ULONG uILOffset = m_map[i].ilOffset;

            if (m_map[i].ilOffset == (ULONG)ICorDebugInfo::PROLOG)
            {
                uILOffset = 0;
                (*pMapType) = MAPPING_PROLOG;
            }
            else if (m_map[i].ilOffset == (ULONG)ICorDebugInfo::NO_MAPPING)
            {
                uILOffset = 0;
                (*pMapType) = MAPPING_UNMAPPED_ADDRESS;
            }
            else if (m_map[i].ilOffset == (ULONG)ICorDebugInfo::EPILOG)
            {
                uILOffset = m_lastILOffset;
                (*pMapType) = MAPPING_EPILOG;
            }
            else if (dwNativeOffset == m_map[i].nativeStartOffset)
            {
                (*pMapType) = MAPPING_EXACT;
            }
            else
            {
                (*pMapType) = MAPPING_APPROXIMATE;
            }
            return uILOffset;
        }
    }

    (*pMapType) = MAPPING_NO_INFO;
    return 0;
}

//
// Copy data from the VM map data to our own map structure and sort. The
// information comes to us in a data structure that differs slightly from the
// one we use out of process, so we have to copy it to the right-side struct.
// Arguments
//    input
//        mapCopy       sequence points
//    output
//        pSeqPoints.m_map is initialized with the correct right side representation of sequence points

inline
void SequencePoints::CopyAndSortSequencePoints(const ICorDebugInfo::OffsetMapping  mapCopy[])
{
    // copy information to pSeqPoint and set end offsets
    unsigned int i;

    ULONG32 lastILOffset = 0;

    const DWORD call_inst = (DWORD)ICorDebugInfo::CALL_INSTRUCTION;
    for (i = 0; i < m_map.Count(); i++)
    {
        m_map[i].ilOffset = mapCopy[i].ilOffset;
        m_map[i].nativeStartOffset = mapCopy[i].nativeOffset;

        if (i < m_map.Count() - 1)
        {
            // We need to not use CALL_INSTRUCTION's IL start offset.
            unsigned int j = i + 1;
            while ((mapCopy[j].source & call_inst) == call_inst && j < m_map.Count()-1)
                j++;

            m_map[i].nativeEndOffset = mapCopy[j].nativeOffset;
        }

        m_map[i].source = mapCopy[i].source;

        // need to cast the offsets to signed values first because we do actually use
        // special negative offsets such as ICorDebugInfo::PROLOG
        if ((m_map[i].source & call_inst) != call_inst)
            lastILOffset = max((int)lastILOffset, (int)m_map[i].ilOffset);
    }

    if (m_map.Count() >= 1)
    {
        m_map[i - 1].nativeEndOffset = 0;
        m_map[i - 1].source =
            (ICorDebugInfo::SourceTypes)(m_map[i - 1].source | ICorDebugInfo::NATIVE_END_OFFSET_UNKNOWN);
    }

    // sort the map
    MapSortILMap mapSorter(&m_map[0], m_map.Count());
    mapSorter.Sort();


    m_mapCount = m_map.Count();
    while (m_mapCount > 0 && (m_map[m_mapCount-1].source & (call_inst)) == call_inst)
        m_mapCount--;

    SetLastILOffset(lastILOffset);
} // CopyAndSortSequencePoints

//-----------------------------------------------------------------------------
// member function implementations for MapSortILMap class to sort sequence points
// by IL offset
//-----------------------------------------------------------------------------

// secondary key comparison--if two IL offsets are the same,
// we determine order based on native offset

inline
int SequencePoints::MapSortILMap::CompareInternal(DebuggerILToNativeMap *first,
                                  DebuggerILToNativeMap *second)
{
    LIMITED_METHOD_CONTRACT;

    if (first->nativeStartOffset == second->nativeStartOffset)
        return 0;
    else if (first->nativeStartOffset < second->nativeStartOffset)
        return -1;
    else
        return 1;
}

//Comparison operator
inline
int SequencePoints::MapSortILMap::Compare(DebuggerILToNativeMap * first,
                          DebuggerILToNativeMap * second)
{
    LIMITED_METHOD_CONTRACT;
    const DWORD call_inst = (DWORD)ICorDebugInfo::CALL_INSTRUCTION;

    //PROLOGs go first
    if (first->ilOffset == (ULONG) ICorDebugInfo::PROLOG &&
        second->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
    {
        return CompareInternal(first, second);
    }
    else if (first->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
    {
        return -1;
    }
    else if (second->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
    {
        return 1;
    }
    // call_instruction goes at the very very end of the table.
    else if ((first->source & call_inst) == call_inst
        && (second->source & call_inst) == call_inst)
    {
        return CompareInternal(first, second);
    } else if ((first->source & call_inst) == call_inst)
    {
        return 1;
    } else if ((second->source & call_inst) == call_inst)
    {
        return -1;
    }
    //NO_MAPPING go last
    else if (first->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING &&
             second->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
    {
        return CompareInternal(first, second);
    }
    else if (first->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
    {
        return 1;
    }
    else if (second->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
    {
        return -1;
    }
    //EPILOGs go next-to-last
    else if (first->ilOffset == (ULONG) ICorDebugInfo::EPILOG &&
             second->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
    {
        return CompareInternal(first, second);
    }
    else if (first->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
    {
        return 1;
    }
    else if (second->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
    {
        return -1;
    }
    //normal offsets compared otherwise
    else if (first->ilOffset < second->ilOffset)
    {
        return -1;
    }
    else if (first->ilOffset == second->ilOffset)
    {
        return CompareInternal(first, second);
    }
    else
    {
        return 1;
    }
}

//-----------------------------------------------------------------------------
// NativeCodeFunctionData member function implementations
// (for getting native code regions)
//-----------------------------------------------------------------------------

inline
CodeBlobRegion & operator++(CodeBlobRegion & rs)
{
     return rs = CodeBlobRegion(rs + 1);
}

// Convert the data in an instance of DebuggerIPCE_JITFUncData to an instance of NativeCodeFunctionData.
// We need to have this latter type to look up or create a new CordbNativeCode object, but the stack walker is
// using the former type to gather information.
// Arguments:
//     Input:
//            source - an initialized instance of DebuggerIPCE_JITFuncData containing the information to
//                     be copied into this instance of NativeCodeFunctionData
// @dbgtodo dlaw: Once CordbThread::RefreshStack is fully DAC-ized, we can change the data structure that it uses
// to have a member of type NativeCodeFunctionData which we can pass without copying. At that point,
// this method can disappear.
inline
NativeCodeFunctionData::NativeCodeFunctionData(DebuggerIPCE_JITFuncData * source)
{
    // copy the code region information
    m_rgCodeRegions[kHot].Init(CORDB_ADDRESS(source->nativeStartAddressPtr), (ULONG)source->nativeHotSize);
    m_rgCodeRegions[kCold].Init(CORDB_ADDRESS(source->nativeStartAddressColdPtr), (ULONG)source->nativeColdSize);

    // copy the other function information
    isInstantiatedGeneric = source->isInstantiatedGeneric;
    vmNativeCodeMethodDescToken = source->vmNativeCodeMethodDescToken;
    encVersion = source->enCVersion;
}


// set all fields to default values (NULL, FALSE, or zero as appropriate)
inline
NativeCodeFunctionData::NativeCodeFunctionData()
{
    Clear();
}

inline
void NativeCodeFunctionData::Clear()
{
    isInstantiatedGeneric =  FALSE;
    encVersion = CorDB_DEFAULT_ENC_FUNCTION_VERSION;
    for (CodeBlobRegion region = kHot; region < MAX_REGIONS; ++region)
    {
        m_rgCodeRegions[region].Clear();
    }
}

//-----------------------------------------------------------------------------------
// ClassInfo member functions
//-----------------------------------------------------------------------------------

inline
ClassInfo::ClassInfo():
    m_objectSize(0)
    {}

// clear all fields
inline
void ClassInfo::Clear()
{
    m_objectSize = 0;
    m_fieldList.Dealloc();
}

inline
ClassInfo::~ClassInfo()
{
    m_fieldList.Dealloc();
}

//-----------------------------------------------------------------------------------
// FieldData member functions
//-----------------------------------------------------------------------------------
#ifndef RIGHT_SIDE_COMPILE

// initialize various fields of an instance of FieldData from information retrieved from a FieldDesc
inline
void FieldData::Initialize(BOOL fIsStatic, BOOL fIsPrimitive, mdFieldDef mdToken)
{
    ClearFields();
    m_fFldIsStatic = (fIsStatic == TRUE);
    m_fFldIsPrimitive = (fIsPrimitive == TRUE);
    // This is what  tells the right side the field is unavailable due to EnC.
    m_fldMetadataToken = mdToken;
}
#endif

// clear various fields for a new instance of FieldData
inline
void FieldData::ClearFields()
{
    m_fldSignatureCache = NULL;
    m_fldSignatureCacheSize = 0;
    m_fldInstanceOffset = 0;
    m_pFldStaticAddress = NULL;
}

typedef ULONG_PTR SIZE_T;

inline
BOOL FieldData::OkToGetOrSetInstanceOffset()
{
    return (!m_fFldIsStatic && !m_fFldIsRVA && !m_fFldIsTLS &&
            m_fFldStorageAvailable  && (m_pFldStaticAddress == NULL));
}

// If this is an instance field, store its offset
inline
void FieldData::SetInstanceOffset(SIZE_T offset)
{
    _ASSERTE(!m_fFldIsStatic);
    _ASSERTE(!m_fFldIsRVA);
    _ASSERTE(!m_fFldIsTLS);
    _ASSERTE(m_fFldStorageAvailable);
    _ASSERTE(m_pFldStaticAddress == NULL);
    m_fldInstanceOffset = offset;
}

inline
BOOL FieldData::OkToGetOrSetStaticAddress()
{
    return (m_fFldIsStatic && !m_fFldIsTLS &&
            m_fFldStorageAvailable && (m_fldInstanceOffset == 0));
}

// If this is a "normal" static, store its absolute address
inline
void FieldData::SetStaticAddress(TADDR addr)
{
    _ASSERTE(m_fFldIsStatic);
    _ASSERTE(!m_fFldIsTLS);
    _ASSERTE(m_fFldStorageAvailable);
    _ASSERTE(m_fldInstanceOffset == 0);
    m_pFldStaticAddress = TADDR(addr);
}

// Get the offset of a field
inline
SIZE_T FieldData::GetInstanceOffset()
{
    _ASSERTE(!m_fFldIsStatic);
    _ASSERTE(!m_fFldIsRVA);
    _ASSERTE(!m_fFldIsTLS);
    _ASSERTE(m_fFldStorageAvailable);
    _ASSERTE(m_pFldStaticAddress == NULL);
    return m_fldInstanceOffset;
}

// Get the static address for a field
inline
TADDR FieldData::GetStaticAddress()
{
    _ASSERTE(m_fFldIsStatic);
    _ASSERTE(!m_fFldIsTLS);
    _ASSERTE(m_fFldStorageAvailable || (m_pFldStaticAddress == NULL));
    _ASSERTE(m_fldInstanceOffset == 0);
    return m_pFldStaticAddress;
}

//-----------------------------------------------------------------------------------
// EnCHangingFieldInfo member functions
//-----------------------------------------------------------------------------------

inline
void EnCHangingFieldInfo::Init(VMPTR_Object     pObject,
                               SIZE_T           offset,
                               mdFieldDef       fieldToken,
                               CorElementType   elementType,
                               mdTypeDef        metadataToken,
                               VMPTR_DomainAssembly vmDomainAssembly)
    {
        m_vmObject = pObject;
        m_offsetToVars = offset;
        m_fldToken = fieldToken;
        m_objectTypeData.elementType = elementType;
        m_objectTypeData.metadataToken = metadataToken;
        m_objectTypeData.vmDomainAssembly = vmDomainAssembly;
    }



#endif // DACDBISTRUCTURES_INL_
