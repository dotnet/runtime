// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Appdomainstack.h -
//


//


#ifndef __appdomainstack_h__
#define __appdomainstack_h__

#include "vars.hpp"
#include "util.hpp"


// Stack of AppDomains executing on the current thread. Used in security optimization to avoid stackwalks
#define ADSTACK_BLOCK_SIZE        16
#define INVALID_APPDOMAIN_ID ((DWORD)-1)
#define CURRENT_APPDOMAIN_ID ((ADID)(DWORD)0)
#define __GetADID(index)   ((index)<ADSTACK_BLOCK_SIZE?m_pStack[(index)].m_domainID:m_pExtraStack[((index)-ADSTACK_BLOCK_SIZE)].m_domainID)
#define __GetEntryPtr(index) ((index)<ADSTACK_BLOCK_SIZE?&(m_pStack[(index)]):&(m_pExtraStack[((index)-ADSTACK_BLOCK_SIZE)]))

struct AppDomainStackEntry
{
    ADID  m_domainID;
    DWORD m_dwOverridesCount;
    DWORD m_dwAsserts;
    DWORD m_dwPreviousThreadWideSpecialFlags;

    FORCEINLINE bool operator==(const AppDomainStackEntry& entry) const
    {
        return (m_domainID == entry.m_domainID &&
                m_dwOverridesCount == entry.m_dwOverridesCount &&
                m_dwAsserts == entry.m_dwAsserts);
    }
    FORCEINLINE bool operator!=(const AppDomainStackEntry& entry) const
    {
        return (m_domainID != entry.m_domainID ||
                m_dwOverridesCount != entry.m_dwOverridesCount ||
                m_dwAsserts != entry.m_dwAsserts);

    }
    BOOL IsFullyTrustedWithNoStackModifiers(void);
    BOOL IsHomogeneousWithNoStackModifiers(void);
    BOOL HasFlagsOrFullyTrustedWithNoStackModifiers(DWORD flags);
};

class AppDomainStack
{
public:
    AppDomainStack() : m_numEntries(0), m_pExtraStack(NULL), m_ExtraStackSize(0), m_dwOverridesCount(0), m_dwAsserts(0), m_dwThreadWideSpecialFlags(0xFFFFFFFF)
    {
        LIMITED_METHOD_CONTRACT;
        FillEntries(m_pStack, ADSTACK_BLOCK_SIZE);
    }

    AppDomainStack(const AppDomainStack& stack):m_numEntries(0), m_pExtraStack(NULL), m_ExtraStackSize(0), m_dwOverridesCount(0), m_dwAsserts(0)
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_dwThreadWideSpecialFlags = stack.m_dwThreadWideSpecialFlags;
        m_numEntries = stack.m_numEntries;
        m_dwOverridesCount = stack.m_dwOverridesCount;
        m_dwAsserts = stack.m_dwAsserts;
        LOG((LF_APPDOMAIN, LL_INFO100, "copy ctor: m_dwAsserts:%d stack.m_dwAsserts:%d\n",m_dwAsserts, stack.m_dwAsserts));
        memcpy(m_pStack, stack.m_pStack, sizeof( AppDomainStackEntry) * ADSTACK_BLOCK_SIZE);
        // If there is anything stored in the extra allocated space, copy that over
        if (m_numEntries > ADSTACK_BLOCK_SIZE)
        {
            // #blocks to allocate = ceil(numDomains/blocksize) - 1 = ceil ((numdomains - blocksize)/blocksize) = numdomains/blocksize
            DWORD numBlocks = m_numEntries/ADSTACK_BLOCK_SIZE; 
            m_ExtraStackSize = numBlocks*ADSTACK_BLOCK_SIZE;
            m_pExtraStack = new AppDomainStackEntry[m_ExtraStackSize];
            memcpy(m_pExtraStack, stack.m_pExtraStack, sizeof(AppDomainStackEntry)*(m_numEntries-ADSTACK_BLOCK_SIZE));
            FillEntries((m_pExtraStack+m_numEntries-ADSTACK_BLOCK_SIZE), (m_ExtraStackSize -(m_numEntries-ADSTACK_BLOCK_SIZE)));
        }
    }

    ~AppDomainStack()
    {
        CONTRACTL
        {
            MODE_ANY;
            GC_NOTRIGGER;
            NOTHROW;
        } CONTRACTL_END;
        if (m_pExtraStack != NULL)
            delete[] m_pExtraStack;
        m_pExtraStack = NULL;
        m_ExtraStackSize = 0;
    }

    bool operator!= (const AppDomainStack& stack) const
    {
        return !(*this == stack);
    }

    bool operator== (const AppDomainStack& stack) const
    {
        LIMITED_METHOD_CONTRACT;
        if (this == &stack) // degenerate case: comparing with self
            return true;
        if (this->m_numEntries != stack.m_numEntries || 
            this->m_dwAsserts != stack.m_dwAsserts || 
            this->m_dwOverridesCount != stack.m_dwOverridesCount)
            return false;
        for (unsigned i =0; i < stack.m_numEntries; i++)
        {
            if (i < ADSTACK_BLOCK_SIZE)
            {
                if (this->m_pStack[i] != stack.m_pStack[i])
                    return false;
            }
            else
            {
                if (this->m_pExtraStack[i-ADSTACK_BLOCK_SIZE] != stack.m_pExtraStack[i-ADSTACK_BLOCK_SIZE])
                    return false;
            }
        }
        return true;
    }
    inline AppDomainStack& operator =(const AppDomainStack& stack)
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        // Degenerate case (assigning x = x)
        if (this == &stack)
            return *this;

        m_dwThreadWideSpecialFlags = stack.m_dwThreadWideSpecialFlags;
        m_numEntries = stack.m_numEntries;
        m_dwOverridesCount = stack.m_dwOverridesCount;
        m_dwAsserts = stack.m_dwAsserts;
        LOG((LF_APPDOMAIN, LL_INFO100, "= operator : m_dwAsserts:%d stack.m_dwAsserts:%d\n",m_dwAsserts, stack.m_dwAsserts));
        memcpy(m_pStack, stack.m_pStack, sizeof( AppDomainStackEntry) * ADSTACK_BLOCK_SIZE);
        // If there is anything stored in the extra allocated space, copy that over
        if (m_numEntries > ADSTACK_BLOCK_SIZE)
        {
            // #blocks to allocate = ceil(numDomains/blocksize) - 1 = ceil ((numdomains - blocksize)/blocksize) = numdomains/blocksize
            DWORD numBlocks = m_numEntries/ADSTACK_BLOCK_SIZE; 
            if (m_ExtraStackSize < numBlocks*ADSTACK_BLOCK_SIZE)
            {
                // free ptr if it exists
                if (m_pExtraStack != NULL)
                    delete[] m_pExtraStack;
                m_pExtraStack = NULL;

                m_ExtraStackSize = numBlocks*ADSTACK_BLOCK_SIZE;
                m_pExtraStack = new AppDomainStackEntry[m_ExtraStackSize];
            }

            memset(m_pExtraStack, 0xFF, sizeof(ADID) * numBlocks);
            memcpy(m_pExtraStack, stack.m_pExtraStack, sizeof(AppDomainStackEntry)*(m_numEntries-ADSTACK_BLOCK_SIZE));
            FillEntries((m_pExtraStack+m_numEntries-ADSTACK_BLOCK_SIZE), (m_ExtraStackSize -(m_numEntries-ADSTACK_BLOCK_SIZE)));
        }
    
        return *this;
    }

    inline void PushDomain(ADID pDomain);
    inline ADID PopDomain();

    inline void InitDomainIteration(DWORD *pIndex) const;
    // Gets the next AD on the stack
    inline ADID GetNextDomainOnStack(DWORD *pIndex, DWORD *pOverrides, DWORD *pAsserts) const;
    inline AppDomainStackEntry* GetNextDomainEntryOnStack(DWORD *pIndex);
    inline AppDomainStackEntry* GetCurrentDomainEntryOnStack(DWORD pIndex);
    // Updates the asserts/overrides on the next AD on the stack
    inline void UpdateDomainOnStack(DWORD pIndex, DWORD asserts, DWORD overrides);
    inline DWORD   GetNumDomains() const;
    inline void ClearDomainStack();
    inline DWORD GetThreadWideSpecialFlag() const;
    inline DWORD IncrementOverridesCount();
    inline DWORD DecrementOverridesCount();
    inline DWORD GetOverridesCount();
    inline DWORD GetInnerAppDomainOverridesCount();
    inline DWORD IncrementAssertCount();
    inline DWORD DecrementAssertCount();
    inline DWORD GetAssertCount();
    inline DWORD GetInnerAppDomainAssertCount();
    bool IsDefaultSecurityInfo() const;
    BOOL AllDomainsHomogeneousWithNoStackModifiers();

private:
    inline void AddMoreDomains(void);
    inline AppDomainStackEntry* ReadTopOfStack();
    void UpdateStackFromEntries();
    static void FillEntries(AppDomainStackEntry ptr[], DWORD size)
    {
        CONTRACTL 
        {
            MODE_ANY;
            GC_NOTRIGGER;
            NOTHROW;
        }CONTRACTL_END;
        _ASSERTE(ptr != NULL);
        DWORD i;
        const AppDomainStackEntry tmp_entry = {ADID(INVALID_APPDOMAIN_ID), 0, 0};
        for(i=0;i<size;i++)
            ptr[i]=tmp_entry;
    }

#ifdef _DEBUG
    inline void LogADStackUpdate(void);
    void CheckOverridesAssertCounts(); // Debug only code to check that assert count/overrides count are always in sync across adstack
#endif

    DWORD       m_numEntries;
    AppDomainStackEntry m_pStack[ADSTACK_BLOCK_SIZE];
    AppDomainStackEntry *m_pExtraStack;
    DWORD       m_ExtraStackSize;
    DWORD m_dwOverridesCount; // across all entries
    DWORD m_dwAsserts; // across all entries
    DWORD       m_dwThreadWideSpecialFlags; // this flag records the last evaluated thread wide security state
};
#endif
