// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: contractimpl.h
//
// Keeps track of contract implementations, used primarily in stub dispatch.
//


//

//
// ============================================================================

#ifndef CONTRACTIMPL_H_
#define CONTRACTIMPL_H_

#include "hash.h"
#include "decodemd.h"

class Module;
class MethodDesc;
class StackingAllocator;

// ===========================================================================
struct DispatchSlot
{
protected:
    PCODE m_slot;

public:
    //------------------------------------------------------------------------
    inline DispatchSlot(PCODE slot) : m_slot(slot)
    { LIMITED_METHOD_CONTRACT; }

    //------------------------------------------------------------------------
    inline DispatchSlot(const DispatchSlot &slot) : m_slot(slot.m_slot)
    { LIMITED_METHOD_CONTRACT; }

    //------------------------------------------------------------------------
    inline DispatchSlot& operator=(PCODE slot)
    { LIMITED_METHOD_CONTRACT; m_slot = slot; return *this; }

    //------------------------------------------------------------------------
    inline DispatchSlot& operator=(const DispatchSlot &slot)
    { LIMITED_METHOD_CONTRACT; m_slot = slot.m_slot; return *this; }

    //------------------------------------------------------------------------
    inline BOOL IsNull()
    { LIMITED_METHOD_CONTRACT; return (m_slot == NULL); }

    //------------------------------------------------------------------------
    inline void SetNull()
    { LIMITED_METHOD_CONTRACT; m_slot = NULL; }

    //------------------------------------------------------------------------
    inline PCODE GetTarget()
    { LIMITED_METHOD_CONTRACT; return m_slot; }

    //------------------------------------------------------------------------
    MethodDesc *GetMethodDesc();
};  // struct DispatchSlot

// ===========================================================================
// This value indicates that a slot number is in reference to the
// current class. Thus, no TypeID can have a value of 0. This is stored
// inside a DispatchToken as the TypeID for such cases.
static const UINT32 TYPE_ID_THIS_CLASS = 0;


// ===========================================================================
// The type IDs used in the dispatch map are relative to the implementing
// type, and are a discriminated union between:
//   - a special value to indicate "this" class
//   - a special value to indicate that an interface is not implemented by the type
//   - an index into the InterfaceMap
class DispatchMapTypeID
{
private:
    static const UINT32 const_nFirstInterfaceIndex = 1;

    UINT32 m_typeIDVal;
    DispatchMapTypeID(UINT32 id) { LIMITED_METHOD_DAC_CONTRACT; m_typeIDVal = id; }
public:
    // Constructors
    static DispatchMapTypeID ThisClassID() { LIMITED_METHOD_CONTRACT; return DispatchMapTypeID(TYPE_ID_THIS_CLASS); }
    static DispatchMapTypeID InterfaceClassID(UINT32 inum)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(inum + const_nFirstInterfaceIndex > inum);
        return DispatchMapTypeID(inum + const_nFirstInterfaceIndex);
    }
    DispatchMapTypeID() { LIMITED_METHOD_DAC_CONTRACT; m_typeIDVal = TYPE_ID_THIS_CLASS; }

    // Accessors
    BOOL IsThisClass() const { LIMITED_METHOD_DAC_CONTRACT; return (m_typeIDVal == TYPE_ID_THIS_CLASS); }
    BOOL IsImplementedInterface() const { LIMITED_METHOD_CONTRACT; return (m_typeIDVal >= const_nFirstInterfaceIndex); }
    UINT32 GetInterfaceNum() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsImplementedInterface());
        return (m_typeIDVal - const_nFirstInterfaceIndex);
    }

    // Ordering/equality
    BOOL operator ==(const DispatchMapTypeID &that) const { LIMITED_METHOD_CONTRACT; return m_typeIDVal == that.m_typeIDVal; }
    BOOL operator !=(const DispatchMapTypeID &that) const { LIMITED_METHOD_CONTRACT; return m_typeIDVal != that.m_typeIDVal; }
    BOOL operator <(const DispatchMapTypeID &that) const { LIMITED_METHOD_CONTRACT; return m_typeIDVal < that.m_typeIDVal; }

    // To/from UINT32, for encoding/decoding etc.
    UINT32 ToUINT32() const { LIMITED_METHOD_DAC_CONTRACT; return m_typeIDVal; }
    static DispatchMapTypeID FromUINT32(UINT32 x) { LIMITED_METHOD_DAC_CONTRACT; return DispatchMapTypeID(x); }
};  // class DispatchMapTypeID

#ifdef FAT_DISPATCH_TOKENS
// ===========================================================================
// This is the structure that is used when typeId becomes too be to be
// contained in a regular DispatchToken. DispatchToken is able to encapsulate
// a DispatchTokenFat*, somewhat like TypeHandle may encapsulate a TypeDesc*.
struct DispatchTokenFat
{
    friend struct DispatchToken;
    friend class BaseDomain;

  private:
    UINT32 m_typeId;
    UINT32 m_slotNum;

  public:
    DispatchTokenFat(UINT32 typeID, UINT32 slotNumber)
        : m_typeId(typeID), m_slotNum(slotNumber)
        {}

    // Equality comparison, used in SHash set.
    bool operator==(const DispatchTokenFat &other) const
        { return m_typeId == other.m_typeId && m_slotNum == other.m_slotNum; }

    // Hashing operator, using in SHash set.
    operator size_t() const
        { return (size_t)m_typeId ^ (size_t)m_slotNum; }
};  // struct DispatchTokenFat

typedef DPTR(DispatchTokenFat) PTR_DispatchTokenFat;
#endif

// ===========================================================================
// This represents the contract used for code lookups throughout the
// virtual stub dispatch mechanism. It is important to know that
// sizeof(DispatchToken) is UINT_PTR, which means it can be thrown around
// by value without a problem.

struct DispatchToken
{
private:
    // IMPORTANT: This is the ONLY member of this class.
    UINT_PTR     m_token;

#ifndef TARGET_64BIT
    // NOTE: On 32-bit, we use the uppermost bit to indicate that the
    // token is really a DispatchTokenFat*, and to recover the pointer
    // we just shift left by 1; correspondingly, when storing a
    // DispatchTokenFat* in a DispatchToken, we shift right by 1.
    static const UINT_PTR MASK_TYPE_ID       = 0x00007FFF;
    static const UINT_PTR MASK_SLOT_NUMBER   = 0x0000FFFF;

    static const UINT_PTR SHIFT_TYPE_ID      = 0x10;
    static const UINT_PTR SHIFT_SLOT_NUMBER  = 0x0;

#ifdef FAT_DISPATCH_TOKENS
    static const UINT_PTR FAT_TOKEN_FLAG     = 0x80000000;
#endif // FAT_DISPATCH_TOKENS

    static const UINT_PTR INVALID_TOKEN      = 0x7FFFFFFF;
#else //TARGET_64BIT
    static const UINT_PTR MASK_SLOT_NUMBER   = UI64(0x000000000000FFFF);

    static const UINT_PTR SHIFT_TYPE_ID      = 0x20;
    static const UINT_PTR SHIFT_SLOT_NUMBER  = 0x0;

#ifdef FAT_DISPATCH_TOKENS
    static const UINT_PTR MASK_TYPE_ID       = UI64(0x000000007FFFFFFF);
    static const UINT_PTR FAT_TOKEN_FLAG     = UI64(0x8000000000000000);
#else
    static const UINT_PTR MASK_TYPE_ID       = UI64(0x00000000FFFFFFFF);
#endif // FAT_DISPATCH_TOKENS

    static const UINT_PTR INVALID_TOKEN      = 0x7FFFFFFFFFFFFFFF;
#endif //TARGET_64BIT

#ifdef FAT_DISPATCH_TOKENS
    //------------------------------------------------------------------------
    static inline BOOL IsFat(UINT_PTR token)
    {
        return (token & FAT_TOKEN_FLAG) != 0;
    }

    //------------------------------------------------------------------------
    static inline DispatchTokenFat* ToFat(UINT_PTR token)
    {
        return PTR_DispatchTokenFat(token << 1);
    }
#endif

    //------------------------------------------------------------------------
    // Combines the two values into a single 32-bit number.
    static UINT_PTR CreateToken(UINT32 typeID, UINT32 slotNumber)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(((UINT_PTR)typeID & MASK_TYPE_ID) == (UINT_PTR)typeID);
        CONSISTENCY_CHECK(((UINT_PTR)slotNumber & MASK_SLOT_NUMBER) == (UINT_PTR)slotNumber);
        return ((((UINT_PTR)typeID & MASK_TYPE_ID) << SHIFT_TYPE_ID) |
                (((UINT_PTR)slotNumber & MASK_SLOT_NUMBER) << SHIFT_SLOT_NUMBER));
    }

    //------------------------------------------------------------------------
    // Extracts the type ID from a token created by CreateToken
    static UINT32 DecodeTypeID(UINT_PTR token)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(token != INVALID_TOKEN);
#ifdef FAT_DISPATCH_TOKENS
        if (IsFat(token))
            return ToFat(token)->m_typeId;
        else
#endif
            return ((token >> SHIFT_TYPE_ID) & MASK_TYPE_ID);
    }

    //------------------------------------------------------------------------
    // Extracts the slot number from a token created by CreateToken
    static UINT32 DecodeSlotNumber(UINT_PTR token)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(token != INVALID_TOKEN);
#ifdef FAT_DISPATCH_TOKENS
        if (IsFat(token))
            return ToFat(token)->m_slotNum;
        else
#endif
            return ((token >> SHIFT_SLOT_NUMBER) & MASK_SLOT_NUMBER);
    }

public:

#ifdef FAT_DISPATCH_TOKENS
#if !defined(TARGET_64BIT)
    static const UINT32   MAX_TYPE_ID_SMALL  = 0x00007FFF;
#else
    static const UINT32   MAX_TYPE_ID_SMALL  = 0x7FFFFFFF;
#endif
#endif // FAT_DISPATCH_TOKENS

    //------------------------------------------------------------------------
    DispatchToken()
    {
        LIMITED_METHOD_CONTRACT;
        m_token = INVALID_TOKEN;
    }

    explicit DispatchToken(UINT_PTR token)
    {
        CONSISTENCY_CHECK(token != INVALID_TOKEN);
        m_token = token;
    }

#ifdef FAT_DISPATCH_TOKENS
    //------------------------------------------------------------------------
    DispatchToken(DispatchTokenFat *pFat)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK((((UINT_PTR)pFat) & 0x1) == 0);
        m_token = (UINT_PTR(pFat) >> 1) | FAT_TOKEN_FLAG;
    }

    //------------------------------------------------------------------------
    static bool RequiresDispatchTokenFat(UINT32 typeID, UINT32 slotNumber)
    {
        LIMITED_METHOD_CONTRACT;
        return typeID > MAX_TYPE_ID_SMALL
#ifdef _DEBUG
            // Stress the overflow mechanism in debug builds.
            || ((typeID != TYPE_ID_THIS_CLASS) && ((typeID % 7) < 4))
#endif
        ;
    }
#endif //FAT_DISPATCH_TOKENS

    //------------------------------------------------------------------------
    inline bool operator==(const DispatchToken &tok) const
    {
        LIMITED_METHOD_CONTRACT;
        return m_token == tok.m_token;
    }

    //------------------------------------------------------------------------
    // Creates a "this" type dispatch token. This means that the type for the
    // token is implied by the type on which one wishes to invoke. In other
    // words, the value returned by GetTypeID is TYPE_ID_THIS_CLASS.
    static DispatchToken CreateDispatchToken(UINT32 slotNumber)
    {
        WRAPPER_NO_CONTRACT;
        return DispatchToken(CreateToken(TYPE_ID_THIS_CLASS, slotNumber));
    }

    //------------------------------------------------------------------------
    // Creates a fully qualified type dispatch token. This means that the ID
    // for the type is encoded directly in the token.
    static DispatchToken CreateDispatchToken(UINT32 typeID, UINT32 slotNumber)
    {
        WRAPPER_NO_CONTRACT;
        return DispatchToken(CreateToken(typeID, slotNumber));
    }

    //------------------------------------------------------------------------
    // Returns the type ID for this dispatch contract
    inline UINT32 GetTypeID() const
    {
        WRAPPER_NO_CONTRACT;
        return DecodeTypeID(m_token);
    }

    //------------------------------------------------------------------------
    // Returns the slot number for this dispatch contract
    inline UINT32 GetSlotNumber() const
    {
        WRAPPER_NO_CONTRACT;
        return DecodeSlotNumber(m_token);
    }

    //------------------------------------------------------------------------
    inline bool IsThisToken() const
    {
        WRAPPER_NO_CONTRACT;
        return (GetTypeID() == TYPE_ID_THIS_CLASS);
    }

    //------------------------------------------------------------------------
    inline bool IsTypedToken() const
    {
        WRAPPER_NO_CONTRACT;
        return (!IsThisToken());
    }

    //------------------------------------------------------------------------
    static DispatchToken From_SIZE_T(SIZE_T token)
    {
        WRAPPER_NO_CONTRACT;
        return DispatchToken((UINT_PTR)token);
    }

    //------------------------------------------------------------------------
    SIZE_T To_SIZE_T() const
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(sizeof(SIZE_T) == sizeof(UINT_PTR));
        return (SIZE_T) m_token;
    }

    //------------------------------------------------------------------------
    inline BOOL IsValid() const
    {
        LIMITED_METHOD_CONTRACT;
        return !(m_token == INVALID_TOKEN);
    }
};  // struct DispatchToken

// DispatchToken.m_token should be the only field of DispatchToken.
static_assert_no_msg(sizeof(DispatchToken) == sizeof(UINT_PTR));

// ===========================================================================
class TypeIDProvider
{
protected:
    UINT32 m_nextID;
#ifdef FAT_DISPATCH_TOKENS
    UINT32 m_nextFatID;
#endif

public:
    // This is used for an invalid type ID.
    static const UINT32 INVALID_TYPE_ID = ~0;

    // If we can have more than 2^32-1 types, we'll need to revisit this.
    static const UINT32 MAX_TYPE_ID = INVALID_TYPE_ID - 1;

    //------------------------------------------------------------------------
    // Ctor
    TypeIDProvider()
        : m_nextID(2)
#ifdef FAT_DISPATCH_TOKENS
        , m_nextFatID(DispatchToken::MAX_TYPE_ID_SMALL + 1)
#endif
    { LIMITED_METHOD_CONTRACT; }

    //------------------------------------------------------------------------
    // Returns the next available ID
    inline UINT32 GetNextID()
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
            PRECONDITION(m_nextID != 0);
        } CONTRACTL_END;
        UINT32 id = m_nextID;

#ifdef FAT_DISPATCH_TOKENS
        if (id > DispatchToken::MAX_TYPE_ID_SMALL)
        {
            return GetNextFatID();
        }
#endif // FAT_DISPATCH_TOKENS

        if (!ClrSafeInt<UINT32>::addition(m_nextID, 1, m_nextID) ||
            m_nextID == INVALID_TYPE_ID)
        {
            ThrowOutOfMemory();
        }
        return id;
    }

#ifdef FAT_DISPATCH_TOKENS
    //------------------------------------------------------------------------
    // Returns the next available ID
    inline UINT32 GetNextFatID()
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
            PRECONDITION(m_nextFatID != 0);
        } CONTRACTL_END;
        UINT32 id = m_nextFatID;
        if (!ClrSafeInt<UINT32>::addition(m_nextFatID, 1, m_nextFatID) ||
            m_nextID == INVALID_TYPE_ID)
        {
            ThrowOutOfMemory();
        }
        return id;
    }
#endif // FAT_DISPATCH_TOKENS
};  // class TypeIDProvider

// ===========================================================================
class TypeIDMap
{
protected:
    HashMap             m_idMap;
    HashMap             m_mtMap;
    Crst                m_lock;
    TypeIDProvider      m_idProvider;
    UINT32              m_entryCount;

    //------------------------------------------------------------------------
    // Returns the next available ID
    inline UINT32 GetNextID()
    {
        WRAPPER_NO_CONTRACT;
        CONSISTENCY_CHECK(m_lock.OwnedByCurrentThread());
        UINT32 id = m_idProvider.GetNextID();
        CONSISTENCY_CHECK(id != TYPE_ID_THIS_CLASS);
        return id;
    }

public:
    //------------------------------------------------------------------------
    void Init();

    //------------------------------------------------------------------------
    // Ctor
    TypeIDMap()
        : m_lock(CrstTypeIDMap, CrstFlags(CRST_REENTRANCY))
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(TypeIDProvider::INVALID_TYPE_ID == static_cast<UINT32>(INVALIDENTRY));
    }

    //------------------------------------------------------------------------
    // Dtor
    ~TypeIDMap()
    { WRAPPER_NO_CONTRACT; }

    //------------------------------------------------------------------------
    // Returns the ID of the type if found. If not found, returns INVALID_TYPE_ID
    UINT32 LookupTypeID(PTR_MethodTable pMT);

    //------------------------------------------------------------------------
    // Returns the ID of the type if found. If not found, returns NULL.
    PTR_MethodTable LookupType(UINT32 id);

    //------------------------------------------------------------------------
    // Returns the ID of the type if found. If not found, assigns the ID and
    // returns the new ID.
    UINT32 GetTypeID(PTR_MethodTable pMT);

#ifndef DACCESS_COMPILE
    //------------------------------------------------------------------------
    // Remove all types that belong to the passed in LoaderAllocator
    void RemoveTypes(LoaderAllocator* pLoaderAllocator);
#endif // DACCESS_COMPILE

    //------------------------------------------------------------------------
    inline UINT32 GetCount()
        { LIMITED_METHOD_CONTRACT; return m_entryCount; }

    //------------------------------------------------------------------------
    class Iterator
    {
        HashMap::Iterator m_it;

    public:
        //--------------------------------------------------------------------
        inline Iterator(TypeIDMap *map)
            : m_it(map->m_mtMap.begin())
        {
            WRAPPER_NO_CONTRACT;
        }

        //--------------------------------------------------------------------
        inline BOOL IsValid()
        {
            WRAPPER_NO_CONTRACT;
            return !m_it.end();
        }

        //--------------------------------------------------------------------
        inline BOOL Next()
        {
            // We want to skip the entries that are ID->Type, and just
            // enumerate the Type->ID entries to avoid duplicates.
            ++m_it;
            return IsValid();
        }

        //--------------------------------------------------------------------
        inline MethodTable *GetType()
        {
            WRAPPER_NO_CONTRACT;
            return (MethodTable *) m_it.GetKey();
        }

        //--------------------------------------------------------------------
        inline UINT32 GetID()
        {
            WRAPPER_NO_CONTRACT;
            return (UINT32) m_it.GetValue();
        }
    };
};  // class TypeIDMap


// ===========================================================================
struct DispatchMapEntry
{
private:
    DispatchMapTypeID m_typeID;
    UINT16            m_slotNumber;
    UINT16            m_targetSlotNumber;

    enum
    {
        e_IS_VALID = 0x1
    };
    UINT16 m_flags;

public:
    //------------------------------------------------------------------------
    // Initializes this structure.
    void InitVirtualMapping(
        DispatchMapTypeID typeID,
        UINT32 slotNumber,
        UINT32 targetSlotNumber)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_typeID = typeID;
        m_slotNumber = (UINT16)slotNumber;
        m_targetSlotNumber = (UINT16)targetSlotNumber;

        // Set the flags
        m_flags = e_IS_VALID;
    }

    //------------------------------------------------------------------------
    inline DispatchMapTypeID GetTypeID()
        { LIMITED_METHOD_CONTRACT; return m_typeID; }

    //------------------------------------------------------------------------
    inline UINT32 GetSlotNumber()
        { LIMITED_METHOD_CONTRACT; return (UINT32) m_slotNumber; }

    //------------------------------------------------------------------------
    inline UINT32 GetTargetSlotNumber()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        CONSISTENCY_CHECK(IsValid());
        return (UINT32)m_targetSlotNumber;
    }
    inline void SetTargetSlotNumber(UINT32 targetSlotNumber)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(IsValid());
        m_targetSlotNumber = (UINT16)targetSlotNumber;
    }

    //------------------------------------------------------------------------
    // Ctor - just blanks everything out - need to call Init*Mapping function.
    inline DispatchMapEntry() : m_flags(0)
        { LIMITED_METHOD_DAC_CONTRACT; }

    inline BOOL IsValid()
        { LIMITED_METHOD_CONTRACT; return (m_flags & e_IS_VALID); }
};  // struct DispatchMapEntry

// ===========================================================================
// This represents an entry in the dispatch mapping. Conceptually, there is a
// source to target mapping. There are additional housekeeping flags.
struct DispatchMapBuilderNode
{
    // This represents the type and slot for this mapping
    DispatchMapTypeID m_typeID;
    UINT32            m_slotNumber;

    // These represent the target, and type of mapping
    MethodDesc * m_pMDTarget;

    // Flags
    UINT32 m_flags;

    enum {
        e_ENTRY_IS_METHODIMPL = 1
    };

    // Next entry in the list
    DispatchMapBuilderNode *m_next;

    //------------------------------------------------------------------------
    void Init(
        DispatchMapTypeID typeID,
        UINT32            slotNumber,
        MethodDesc *      pMDTarget)
    {
        WRAPPER_NO_CONTRACT;
        CONSISTENCY_CHECK(CheckPointer(pMDTarget, NULL_OK));
        // Remember type and slot
        m_typeID = typeID;
        m_slotNumber = slotNumber;
        // Set the target MD
        m_pMDTarget = pMDTarget;
        // Initialize the flags
        m_flags = 0;
        // Default to null link
        m_next = NULL;
    }

    //------------------------------------------------------------------------
    inline BOOL IsMethodImpl()
    {
        WRAPPER_NO_CONTRACT;
        return (m_flags & e_ENTRY_IS_METHODIMPL);
    }

    //------------------------------------------------------------------------
    inline void SetIsMethodImpl()
    {
        WRAPPER_NO_CONTRACT;
        m_flags |= e_ENTRY_IS_METHODIMPL;
    }
};  // struct DispatchMapBuilderNode

// ===========================================================================
class DispatchMapBuilder
{
public:
    class Iterator;

    //------------------------------------------------------------------------
    DispatchMapBuilder(StackingAllocator *allocator)
        : m_pHead(NULL), m_cEntries(0), m_pAllocator(allocator)
    { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(CheckPointer(m_pAllocator)); }

    //------------------------------------------------------------------------
    inline StackingAllocator *GetAllocator()
    { LIMITED_METHOD_CONTRACT; return m_pAllocator; }

    //------------------------------------------------------------------------
    // If TRUE, it points to a matching entry.
    // If FALSE, it is at the insertion point.
    BOOL Find(DispatchMapTypeID typeID, UINT32 slotNumber, Iterator &it);

    //------------------------------------------------------------------------
    // If TRUE, contains such an entry.
    // If FALSE, no such entry exists.
    BOOL Contains(DispatchMapTypeID typeID, UINT32 slotNumber);

    //------------------------------------------------------------------------
    // This is used when building a MT, and things such as implementation
    // table index and chain delta can't be calculated until later on. That's
    // why we use an MD to get the information later.
    void InsertMDMapping(
        DispatchMapTypeID typeID,
        UINT32            slotNumber,
        MethodDesc *      pMDTarget,
        BOOL              fIsMethodImpl);

    //------------------------------------------------------------------------
    inline UINT32 Count()
    { LIMITED_METHOD_CONTRACT; return m_cEntries; }

    //------------------------------------------------------------------------
    class Iterator
    {
        friend class DispatchMapBuilder;

    protected:
        DispatchMapBuilderNode **m_cur;

        //--------------------------------------------------------------------
        inline DispatchMapBuilderNode **EntryNodePtr()
        { LIMITED_METHOD_CONTRACT; return m_cur; }

        //--------------------------------------------------------------------
        inline DispatchMapBuilderNode *EntryNode()
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsValid()); return *m_cur; }

public:
        //--------------------------------------------------------------------
        // Creates an iterator that is pointing to the first entry of the map.
        inline Iterator(DispatchMapBuilder *pMap)
            : m_cur(&pMap->m_pHead)
        { LIMITED_METHOD_CONTRACT; }

        //--------------------------------------------------------------------
        // Creates an iterator this is pointing to the same location as 'it'.
        inline Iterator(Iterator &it)
            : m_cur(it.m_cur)
        { LIMITED_METHOD_CONTRACT; }

        //--------------------------------------------------------------------
        inline BOOL IsValid()
        { LIMITED_METHOD_CONTRACT; return (*m_cur != NULL); }

        //--------------------------------------------------------------------
        inline BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            if (!IsValid()) {
                return FALSE;
            }
            m_cur = &((*m_cur)->m_next);
            return (IsValid());
        }

        //--------------------------------------------------------------------
        inline DispatchMapTypeID GetTypeID()
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            return EntryNode()->m_typeID;
        }

        //--------------------------------------------------------------------
        inline UINT32 GetSlotNumber()
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            return EntryNode()->m_slotNumber;
        }

        //--------------------------------------------------------------------
        inline MethodDesc *GetTargetMD()
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            return EntryNode()->m_pMDTarget;
        }

        //--------------------------------------------------------------------
        UINT32 GetTargetSlot();

        //--------------------------------------------------------------------
        inline void SetTarget(MethodDesc *pMDTarget)
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            CONSISTENCY_CHECK(CheckPointer(pMDTarget));
            EntryNode()->m_pMDTarget = pMDTarget;
        }

        //--------------------------------------------------------------------
        inline BOOL IsMethodImpl()
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            return EntryNode()->IsMethodImpl();
        }

        //--------------------------------------------------------------------
        inline void SetIsMethodImpl()
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(IsValid());
            EntryNode()->SetIsMethodImpl();
        }

        inline void SkipThisTypeEntries()
        {
            LIMITED_METHOD_CONTRACT;
            while (IsValid() && GetTypeID() == DispatchMapTypeID::ThisClassID())
            {
                Next();
            }
        }
    };  // class Iterator

protected:
    DispatchMapBuilderNode * m_pHead;
    UINT32                   m_cEntries;
    StackingAllocator *      m_pAllocator;

    //------------------------------------------------------------------------
    DispatchMapBuilderNode * NewEntry();

};  // class DispatchMapBuilder

typedef DPTR(class DispatchMap) PTR_DispatchMap;
// ===========================================================================
class DispatchMap
{
protected:
    BYTE m_rgMap[0];

    static const INT32 ENCODING_TYPE_DELTA = 1;
    static const INT32 ENCODING_SLOT_DELTA = 1;
    static const INT32 ENCODING_TARGET_SLOT_DELTA = 1;

public:
    //------------------------------------------------------------------------
    // Need to make sure that you allocate GetObjectSize(pMap) bytes for any
    // instance of DispatchMap, as this constructor assumes that m_rgMap is
    // large enough to store cbMap bytes, which GetObjectSize ensures.
    DispatchMap(
        BYTE * pMap,
        UINT32 cbMap);

    //------------------------------------------------------------------------
    static void CreateEncodedMapping(
        MethodTable *        pMT,
        DispatchMapBuilder * pMapBuilder,
        StackingAllocator *  pAllocator,
        BYTE **              ppbMap,
        UINT32 *             pcbMap);

    //------------------------------------------------------------------------
    static UINT32 GetObjectSize(UINT32 cbMap)
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT32)(sizeof(DispatchMap) + cbMap);
    }

    //------------------------------------------------------------------------
    UINT32 GetMapSize();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    //------------------------------------------------------------------------
    class EncodedMapIterator
    {
        friend class DispatchMap;
    protected:
        DispatchMapEntry m_e;

        // These fields are for decoding the implementation map
        Decoder          m_d;
        // Keep count of the number of types in the list
        INT32            m_numTypes;
        INT32            m_curType;
        DispatchMapTypeID           m_curTypeId;
        BOOL             m_fCurTypeHasNegativeEntries;

        // Keep count of the number of entries for the current type
        INT32            m_numEntries;
        INT32            m_curEntry;
        UINT32           m_curSlot;

        UINT32           m_curTargetSlot;

        //--------------------------------------------------------------------
        void Invalidate();

        //--------------------------------------------------------------------
        void Init(PTR_BYTE pbMap);

public:
        //--------------------------------------------------------------------
        EncodedMapIterator(MethodTable *pMT);

        //--------------------------------------------------------------------
        // This should be used only when a dispatch map needs to be used
        // separately from its MethodTable.
        EncodedMapIterator(DispatchMap *pMap);

        //--------------------------------------------------------------------
        EncodedMapIterator(PTR_BYTE pbMap);

        //--------------------------------------------------------------------
        inline BOOL IsValid()
        { LIMITED_METHOD_DAC_CONTRACT; return (m_curType < m_numTypes); }

        //--------------------------------------------------------------------
        BOOL Next();

        //--------------------------------------------------------------------
        inline DispatchMapEntry *Entry()
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsValid()); return &m_e; }
    };  // class EncodedMapIterator

public:
    //------------------------------------------------------------------------
    class Iterator
    {
    protected:
        // This is for generating entries from the encoded map
        EncodedMapIterator m_mapIt;

    public:
        //--------------------------------------------------------------------
        Iterator(MethodTable *pMT);

        //--------------------------------------------------------------------
        BOOL IsValid();

        //--------------------------------------------------------------------
        BOOL Next();

        //--------------------------------------------------------------------
        DispatchMapEntry *Entry();
    };  // class Iterator
};  // class DispatchMap

#ifdef LOGGING
struct StubDispatchStats
{
    // DispatchMap stats
    UINT32 m_cDispatchMap;              // Number of DispatchMaps created
    UINT32 m_cbDispatchMap;             // Total size of created maps
    UINT32 m_cNGENDispatchMap;
    UINT32 m_cbNGENDispatchMap;

    // Some comparative stats with the old world (simulated)
    UINT32 m_cVTables;                  // Number of vtables out there
    UINT32 m_cVTableSlots;              // Total number of slots.
    UINT32 m_cVTableDuplicateSlots;     // Total number of duplicated slots

    UINT32 m_cCacheLookups;
    UINT32 m_cCacheMisses;

    UINT32 m_cbComInteropData;
};  // struct StubDispatchStats

extern StubDispatchStats g_sdStats;
#endif // LOGGING

#endif // !CONTRACTIMPL_H_
