// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _SHASH_INL_
#define _SHASH_INL_

// Many SHash functions do not throw on their own, but may propagate an exception
// from Hash, Equals, or GetKey.
#define NOTHROW_UNLESS_TRAITS_THROWS     if (TRAITS::s_NoThrow) NOTHROW; else THROWS

void DECLSPEC_NORETURN ThrowOutOfMemory();

template <typename TRAITS>
SHash<TRAITS>::SHash()
  : m_table(nullptr),
    m_tableSize(0),
    m_tableCount(0),
    m_tableOccupied(0),
    m_tableMax(0)
{
    LIMITED_METHOD_CONTRACT;

#ifndef __GNUC__ // these crash GCC
    static_assert_no_msg(SHash<TRAITS>::s_growth_factor_numerator > SHash<TRAITS>::s_growth_factor_denominator);
    static_assert_no_msg(SHash<TRAITS>::s_density_factor_numerator < SHash<TRAITS>::s_density_factor_denominator);
#endif
}

template <typename TRAITS>
SHash<TRAITS>::~SHash()
{
    LIMITED_METHOD_CONTRACT;

    if (TRAITS::s_DestructPerEntryCleanupAction)
    {
        for (Iterator i = Begin(); i != End(); i++)
        {
            TRAITS::OnDestructPerEntryCleanupAction(*i);
        }
    }

    delete [] m_table;
}

template <typename TRAITS>
typename SHash<TRAITS>::count_t SHash<TRAITS>::GetCount() const 
{
    LIMITED_METHOD_CONTRACT;

    return m_tableCount;
}

template <typename TRAITS>
typename SHash<TRAITS>::element_t SHash<TRAITS>::Lookup(key_t key) const
{
    CONTRACT(element_t)
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        POSTCONDITION(TRAITS::IsNull(RETVAL) || TRAITS::Equals(key, TRAITS::GetKey(RETVAL)));
        SUPPORTS_DAC_WRAPPER;
    }
    CONTRACT_END;

    const element_t *pRet = Lookup(m_table, m_tableSize, key);
    RETURN ((pRet != NULL) ? (*pRet) : TRAITS::Null());
}

template <typename TRAITS>
const typename SHash<TRAITS>::element_t * SHash<TRAITS>::LookupPtr(key_t key) const
{
    CONTRACT(const element_t *)
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        POSTCONDITION(RETVAL == NULL || TRAITS::Equals(key, TRAITS::GetKey(*RETVAL)));
    }
    CONTRACT_END;

    RETURN Lookup(m_table, m_tableSize, key);
}

template <typename TRAITS>
void SHash<TRAITS>::Add(const element_t & element)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        POSTCONDITION(TRAITS::Equals(TRAITS::GetKey(element), TRAITS::GetKey(*LookupPtr(TRAITS::GetKey(element)))));
    }
    CONTRACT_END;
    
    CheckGrowth();
    
    Add_GrowthChecked(element);
    
    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::Add_GrowthChecked(const element_t & element)
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        POSTCONDITION(TRAITS::Equals(TRAITS::GetKey(element), TRAITS::GetKey(*LookupPtr(TRAITS::GetKey(element)))));
    }
    CONTRACT_END;
    
    if (Add(m_table, m_tableSize, element))
        m_tableOccupied++;
    m_tableCount++;
    
    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::AddOrReplace(const element_t &element)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        static_assert(!TRAITS::s_supports_remove, "SHash::AddOrReplace is not implemented for SHash with support for remove operations.");
        POSTCONDITION(TRAITS::Equals(TRAITS::GetKey(element), TRAITS::GetKey(*LookupPtr(TRAITS::GetKey(element)))));
    }
    CONTRACT_END;

    CheckGrowth();

    AddOrReplace(m_table, m_tableSize, element);

    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(key_t key)
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(!(TRAITS::IsNull(Lookup(key))));
    }
    CONTRACT_END;

    Remove(m_table, m_tableSize, key);

    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(Iterator& i)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(!(TRAITS::IsNull(*i)));
        PRECONDITION(!(TRAITS::IsDeleted(*i)));
    }
    CONTRACT_END;

    RemoveElement(m_table, m_tableSize, (element_t*)&(*i));

    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(KeyIterator& i)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(!(TRAITS::IsNull(*i)));
        PRECONDITION(!(TRAITS::IsDeleted(*i)));
    }
    CONTRACT_END;

    RemoveElement(m_table, m_tableSize, (element_t*)&(*i));

    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::RemovePtr(element_t * p)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(!(TRAITS::IsNull(*p)));
        PRECONDITION(!(TRAITS::IsDeleted(*p)));
    }
    CONTRACT_END;

    RemoveElement(m_table, m_tableSize, p);

    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::RemoveAll()
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;

    delete [] m_table;

    m_table = NULL;
    m_tableSize = 0;
    m_tableCount = 0;
    m_tableOccupied = 0;
    m_tableMax = 0;

    RETURN;
}

template <typename TRAITS>
typename SHash<TRAITS>::Iterator SHash<TRAITS>::Begin() const
{
    LIMITED_METHOD_CONTRACT;

    Iterator i(this, TRUE);
    i.First();
    return i;
}

template <typename TRAITS>
typename SHash<TRAITS>::Iterator SHash<TRAITS>::End() const
{
    LIMITED_METHOD_CONTRACT;

    return Iterator(this, FALSE);
}

template <typename TRAITS>
typename SHash<TRAITS>::KeyIterator SHash<TRAITS>::Begin(key_t key) const
{
    LIMITED_METHOD_CONTRACT;

    KeyIterator k(this, TRUE);
    k.SetKey(key);
    return k;
}

template <typename TRAITS>
typename SHash<TRAITS>::KeyIterator SHash<TRAITS>::End(key_t key) const
{
    LIMITED_METHOD_CONTRACT;

    return KeyIterator(this, FALSE);
}

template <typename TRAITS>
BOOL SHash<TRAITS>::CheckGrowth()
{
    CONTRACT(BOOL)
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;

    if (m_tableOccupied == m_tableMax)
    {
        Grow();
        RETURN TRUE;
    }
        
    RETURN FALSE;
}

template <typename TRAITS>
typename SHash<TRAITS>::element_t * 
SHash<TRAITS>::CheckGrowth_OnlyAllocateNewTable(count_t * pcNewSize)
{
    CONTRACT(element_t *)
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;
    
    if (m_tableOccupied == m_tableMax)
    {
        RETURN Grow_OnlyAllocateNewTable(pcNewSize);
    }
    
    RETURN NULL;
}

template <typename TRAITS>
void SHash<TRAITS>::Grow()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;
    
    count_t     newSize;
    element_t * newTable = Grow_OnlyAllocateNewTable(&newSize);
    element_t * oldTable = ReplaceTable(newTable, newSize);
    DeleteOldTable(oldTable);
    
    RETURN;
}

template <typename TRAITS>
typename SHash<TRAITS>::element_t * 
SHash<TRAITS>::Grow_OnlyAllocateNewTable(count_t * pcNewSize)
{
    CONTRACT(element_t *)
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;
    
    count_t newSize = (count_t) (m_tableCount 
                                 * TRAITS::s_growth_factor_numerator / TRAITS::s_growth_factor_denominator
                                 * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator);
    if (newSize < TRAITS::s_minimum_allocation)
        newSize = TRAITS::s_minimum_allocation;
    
    // handle potential overflow
    if (newSize < m_tableCount)
        ThrowOutOfMemory();
    
    RETURN AllocateNewTable(newSize, pcNewSize);
}

template <typename TRAITS>
void SHash<TRAITS>::Reallocate(count_t requestedSize)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;
    
    count_t newTableSize;
    element_t * newTable = AllocateNewTable(requestedSize, &newTableSize);
    element_t * oldTable = ReplaceTable(newTable, newTableSize);
    DeleteOldTable(oldTable);
    
    RETURN;
}

template <typename TRAITS>
template <typename Functor>
void SHash<TRAITS>::ForEach(Functor &functor)
{
    WRAPPER_NO_CONTRACT;  // LIMITED_METHOD_CONTRACT + Functor

    for (count_t i = 0; i < m_tableSize; i++)
    {
        element_t element = m_table[i];
        if (!TRAITS::IsNull(element) && !TRAITS::IsDeleted(element))
        {
            functor(element);
        }
    }
}

template <typename TRAITS>
typename SHash<TRAITS>::element_t * 
SHash<TRAITS>::AllocateNewTable(count_t requestedSize, count_t * pcNewTableSize)
{
    CONTRACT(element_t *)
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(requestedSize >= 
                     (count_t) (GetCount() * s_density_factor_denominator / s_density_factor_numerator));
    }
    CONTRACT_END;

    // Allocation size must be a prime number.  This is necessary so that hashes uniformly
    // distribute to all indices, and so that chaining will visit all indices in the hash table.
    *pcNewTableSize = NextPrime(requestedSize);

    element_t * newTable = new element_t [*pcNewTableSize];

    element_t * p = newTable;
    element_t * pEnd = newTable + *pcNewTableSize;
    while (p < pEnd)
    {
        *p = TRAITS::Null();
        p++;
    }
    
    RETURN newTable;
}

template <typename TRAITS>
typename SHash<TRAITS>::element_t * 
SHash<TRAITS>::ReplaceTable(element_t * newTable, count_t newTableSize)
{
    CONTRACT(element_t *)
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(newTableSize >= 
                     (count_t) (GetCount() * s_density_factor_denominator / s_density_factor_numerator));
    }
    CONTRACT_END;
    
    element_t * oldTable = m_table;
    
    // Move all entries over to new table.
    for (Iterator i = Begin(), end = End(); i != end; i++)
    {
        const element_t & cur = (*i);
        if (!TRAITS::IsNull(cur) && !TRAITS::IsDeleted(cur))
            Add(newTable, newTableSize, cur);
    }
    
    m_table = PTR_element_t(newTable);
    m_tableSize = newTableSize;
    m_tableMax = (count_t) (newTableSize * TRAITS::s_density_factor_numerator / TRAITS::s_density_factor_denominator);
    m_tableOccupied = m_tableCount;

    RETURN oldTable;
}

template <typename TRAITS>
void 
SHash<TRAITS>::DeleteOldTable(element_t * oldTable)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    
    // @todo:
    // We might want to try to delay this cleanup to allow asynchronous readers
    if (oldTable != NULL)
        delete [] oldTable;
    
    RETURN;
}

template <typename TRAITS>
const typename SHash<TRAITS>::element_t * SHash<TRAITS>::Lookup(PTR_element_t table, count_t tableSize, key_t key)
{
    CONTRACT(const element_t *)
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(RETVAL == NULL || TRAITS::Equals(key, TRAITS::GetKey(*RETVAL)));
        SUPPORTS_DAC_WRAPPER;   // supports DAC only if the traits class does
    }
    CONTRACT_END;

    if (tableSize == 0)
        RETURN NULL;

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize; 
    count_t increment = 0; // delay computation

    while (TRUE)
    {
        element_t& current = table[index];
            
        if (TRAITS::IsNull(current))
            RETURN NULL;

        if (!TRAITS::IsDeleted(current)
            && TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            RETURN &current;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1; 

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
BOOL SHash<TRAITS>::Add(element_t * table, count_t tableSize, const element_t & element)
{
    CONTRACT(BOOL)
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(TRAITS::Equals(TRAITS::GetKey(element), TRAITS::GetKey(*Lookup(table, tableSize, TRAITS::GetKey(element)))));
    }
    CONTRACT_END;

    key_t key = TRAITS::GetKey(element);

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize; 
    count_t increment = 0; // delay computation

    while (TRUE)
    {
        element_t & current = table[index];
        
        if (TRAITS::IsNull(current))
        {
            table[index] = element;
            RETURN TRUE;
        }
        
        if (TRAITS::IsDeleted(current))
        {
            table[index] = element;
            RETURN FALSE;
        }
        
        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1; 
        
        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
void SHash<TRAITS>::AddOrReplace(element_t *table, count_t tableSize, const element_t &element)
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        static_assert(!TRAITS::s_supports_remove, "SHash::AddOrReplace is not implemented for SHash with support for remove operations.");
        POSTCONDITION(TRAITS::Equals(TRAITS::GetKey(element), TRAITS::GetKey(*Lookup(table, tableSize, TRAITS::GetKey(element)))));
    }
    CONTRACT_END;

    key_t key = TRAITS::GetKey(element);

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize; 
    count_t increment = 0; // delay computation

    while (TRUE)
    {
        element_t& current = table[index];
        _ASSERTE(!TRAITS::IsDeleted(current));    
 
        if (TRAITS::IsNull(current))
        {
            table[index] = element;
            m_tableCount++;
            m_tableOccupied++;
            RETURN;
        }
        else if (TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            table[index] = element;
            RETURN;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1; 

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(element_t *table, count_t tableSize, key_t key)
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(Lookup(table, tableSize, key) != NULL);
    }
    CONTRACT_END;

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize; 
    count_t increment = 0; // delay computation

    while (TRUE)
    {
        element_t& current = table[index];
            
        if (TRAITS::IsNull(current))
            RETURN;

        if (!TRAITS::IsDeleted(current)
            && TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            table[index] = TRAITS::Deleted();
            m_tableCount--;
            RETURN;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1; 

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
void SHash<TRAITS>::RemoveElement(element_t *table, count_t tableSize, element_t *element)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        static_assert(TRAITS::s_supports_remove, "This SHash does not support remove operations.");
        PRECONDITION(table <= element && element < table + tableSize);
        PRECONDITION(!TRAITS::IsNull(*element) && !TRAITS::IsDeleted(*element));
    }
    CONTRACT_END;

    *element = TRAITS::Deleted();
    m_tableCount--;
    RETURN;
}

template <typename TRAITS>
BOOL SHash<TRAITS>::IsPrime(COUNT_T number)
{
    CONTRACT(BOOL)
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // This is a very low-tech check for primality, which doesn't scale very well.  
    // There are more efficient tests if this proves to be burdensome for larger
    // tables.

    if ((number & 1) == 0)
        RETURN FALSE;

    COUNT_T factor = 3;
    while (factor * factor <= number)
    {
        if ((number % factor) == 0)
            RETURN FALSE;
        factor += 2;
    }

    RETURN TRUE;
}

// allow coexistence with simplerhash.inl
#ifndef _HASH_PRIMES_DEFINED
#define _HASH_PRIMES_DEFINED

namespace
{
    const COUNT_T g_shash_primes[] = {
        11,17,23,29,37,47,59,71,89,107,131,163,197,239,293,353,431,521,631,761,919,
        1103,1327,1597,1931,2333,2801,3371,4049,4861,5839,7013,8419,10103,12143,14591,
        17519,21023,25229,30293,36353,43627,52361,62851,75431,90523, 108631, 130363, 
        156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
        968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 
        4999559, 5999471, 7199369 };
}

#endif //_HASH_PRIMES_DEFINED

template <typename TRAITS>
COUNT_T SHash<TRAITS>::NextPrime(COUNT_T number)
{
    CONTRACT(COUNT_T)
    {
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(IsPrime(RETVAL));
    }
    CONTRACT_END;

    for (int i = 0; i < (int) (sizeof(g_shash_primes) / sizeof(g_shash_primes[0])); i++) {
        if (g_shash_primes[i] >= number)
            RETURN g_shash_primes[i];
    }

    if ((number&1) == 0)
        number++;

    while (number != 1) {
        if (IsPrime(number))
            RETURN number;
        number +=2;
    }

    // overflow
    ThrowOutOfMemory();
}

template <typename TRAITS>
SHash<TRAITS>::AddPhases::AddPhases()
{
    LIMITED_METHOD_CONTRACT;
            
    m_pHash = NULL;
    m_newTable = NULL;
    m_newTableSize = 0;
    m_oldTable = NULL;
    
    INDEBUG(dbg_m_fAddCalled = FALSE;)
}

template <typename TRAITS>
SHash<TRAITS>::AddPhases::~AddPhases()
{
    LIMITED_METHOD_CONTRACT;
    
    if (m_newTable != NULL)
    {   // The new table was not applied to the hash yet
        _ASSERTE((m_pHash != NULL) && (m_newTableSize != 0) && (m_oldTable == NULL));
                
        delete [] m_newTable;
    }
    DeleteOldTable();
}

template <typename TRAITS>
void SHash<TRAITS>::AddPhases::PreallocateForAdd(SHash * pHash)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    
    _ASSERTE((m_pHash == NULL) && (m_newTable == NULL) && (m_newTableSize == 0) && (m_oldTable == NULL));
    
    m_pHash = pHash;
    // May return NULL if the allocation was not needed
    m_newTable = m_pHash->CheckGrowth_OnlyAllocateNewTable(&m_newTableSize);
    
#ifdef _DEBUG
    dbg_m_table = pHash->m_table;
    dbg_m_tableSize = pHash->m_tableSize;
    dbg_m_tableCount = pHash->m_tableCount;
    dbg_m_tableOccupied = pHash->m_tableOccupied;
    dbg_m_tableMax = pHash->m_tableMax;
#endif //_DEBUG
    
    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::AddPhases::Add(const element_t & element)
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    
    _ASSERTE((m_pHash != NULL) && (m_oldTable == NULL));
    // Add can be called only once on this object
    _ASSERTE(!dbg_m_fAddCalled);
    
    // Check that the hash table didn't change since call to code:PreallocateForAdd
    _ASSERTE(dbg_m_table == m_pHash->m_table);
    _ASSERTE(dbg_m_tableSize == m_pHash->m_tableSize);
    _ASSERTE(dbg_m_tableCount >= m_pHash->m_tableCount); // Remove operation might have removed elements
    _ASSERTE(dbg_m_tableOccupied == m_pHash->m_tableOccupied);
    _ASSERTE(dbg_m_tableMax == m_pHash->m_tableMax);
    
    if (m_newTable != NULL)
    {   // We have pre-allocated table from code:PreallocateForAdd, use it.
        _ASSERTE(m_newTableSize != 0);
        
        // May return NULL if there was not table allocated yet
        m_oldTable = m_pHash->ReplaceTable(m_newTable, m_newTableSize);
        
        m_newTable = NULL;
        m_newTableSize = 0;
    }
    // We know that we have enough space, direcly add the element
    m_pHash->Add_GrowthChecked(element);
    
    INDEBUG(dbg_m_fAddCalled = TRUE;)
    
    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::AddPhases::AddNothing_PublishPreallocatedTable()
{
    CONTRACT_VOID
    {
        NOTHROW_UNLESS_TRAITS_THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    
    _ASSERTE((m_pHash != NULL) && (m_oldTable == NULL));
    // Add can be called only once on this object
    _ASSERTE(!dbg_m_fAddCalled);
    
    // Check that the hash table didn't change since call to code:PreallocateForAdd
    _ASSERTE(dbg_m_table == m_pHash->m_table);
    _ASSERTE(dbg_m_tableSize == m_pHash->m_tableSize);
    _ASSERTE(dbg_m_tableCount >= m_pHash->m_tableCount); // Remove operation might have removed elements
    _ASSERTE(dbg_m_tableOccupied == m_pHash->m_tableOccupied);
    _ASSERTE(dbg_m_tableMax == m_pHash->m_tableMax);
    
    if (m_newTable != NULL)
    {   // We have pre-allocated table from code:PreallocateForAdd, use it.
        _ASSERTE(m_newTableSize != 0);
        
        // May return NULL if there was not table allocated yet
        m_oldTable = m_pHash->ReplaceTable(m_newTable, m_newTableSize);
        
        m_newTable = NULL;
        m_newTableSize = 0;
    }
    
    INDEBUG(dbg_m_fAddCalled = TRUE;)
    
    RETURN;
}

template <typename TRAITS>
void SHash<TRAITS>::AddPhases::DeleteOldTable()
{
    LIMITED_METHOD_CONTRACT;
    
    if (m_oldTable != NULL)
    {
        _ASSERTE((m_pHash != NULL) && (m_newTable == NULL) && (m_newTableSize == 0));
        
        delete [] m_oldTable;
        m_oldTable = NULL;
    }
}

template <typename TRAITS>
template <typename LockHolderT,
          typename AddLockHolderT,
          typename LockT,
          typename AddLockT>
bool SHash<TRAITS>::CheckAddInPhases(
    element_t const & elem,
    LockT & lock,
    AddLockT & addLock,
    IUnknown * addRefObject)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    AddLockHolderT hAddLock(&addLock);
    AddPhases addCall;

    // 1. Preallocate one element
    addCall.PreallocateForAdd(this);
    {
        // 2. Take the reader lock. Host callouts now forbidden.
        LockHolderT hLock(&lock);

        element_t const * pEntry = LookupPtr(TRAITS::GetKey(elem));
        if (pEntry != nullptr)
        {
            // 3a. Use the newly allocated table (if any) to avoid later redundant allocation.
            addCall.AddNothing_PublishPreallocatedTable();
            return false;
        }
        else
        {
            // 3b. Add the element to the hash table.
            addCall.Add(elem);

            if (addRefObject != nullptr)
            {
                clr::SafeAddRef(addRefObject);
            }
            
            return true;
        }
    }

    // 4. addCall's destructor will take care of any required cleanup.
}


#endif // _SHASH_INL_
