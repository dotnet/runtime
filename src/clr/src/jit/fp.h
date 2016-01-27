// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _JIT_FP

#define _JIT_FP

// Auxiliary structures.
#if FEATURE_STACK_FP_X87

enum dummyFPenum
{
    #define REGDEF(name, rnum, mask, sname)  dummmy_##name = rnum,
    #include "registerfp.h"

    FP_VIRTUALREGISTERS,
};


// FlatFPStateX87 holds the state of the virtual register file. For each
// virtual register we keep track to which physical register we're 
// mapping. We also keep track of the physical stack.

#define FP_PHYSICREGISTERS FP_VIRTUALREGISTERS
#define FP_VRNOTMAPPED     -1

struct FlatFPStateX87
{
public:  
    void                    Init                    (FlatFPStateX87* pFrom = 0);
    bool                    Mapped                  (unsigned uEntry); // Is virtual register mapped
    void                    Unmap                   (unsigned uEntry); // Unmaps a virtual register
    void                    Associate               (unsigned uEntry, unsigned uStack);
    unsigned                StackToST               (unsigned uEntry); // Maps the stack to a ST(x) entry
    unsigned                VirtualToST             (unsigned uEntry);
    unsigned                STToVirtual             (unsigned uST);
    unsigned                TopIndex                ();
    unsigned                TopVirtual              ();
    void                    Rename                  (unsigned uVirtualTo, unsigned uVirtualFrom);
    unsigned                Pop                     ();
    void                    Push                    (unsigned uEntry);
    bool                    IsEmpty                 ();
            
    // Debug/test methods
    static bool             AreEqual                (FlatFPStateX87* pSrc, FlatFPStateX87* pDst);
    #ifdef DEBUG    
    bool                    IsValidEntry            (unsigned uEntry);
    bool                    IsConsistent            ();
    void                    UpdateMappingFromStack  ();
    void                    Dump                    ();

    // In some optimizations the stack will be inconsistent in some transactions. We want to keep
    // the checks for everthing else, so if have the stack in an inconsistent state, you must
    // ignore it on purpose.
    bool                    m_bIgnoreConsistencyChecks;

    inline void IgnoreConsistencyChecks(bool bIgnore) 
    {
        m_bIgnoreConsistencyChecks = bIgnore;
    }
    #else
    inline void IgnoreConsistencyChecks(bool bIgnore) 
    {       
    }    
    #endif

    unsigned                m_uVirtualMap[FP_VIRTUALREGISTERS];
    unsigned                m_uStack[FP_PHYSICREGISTERS];
    unsigned                m_uStackSize;
};    
    
#endif // FEATURE_STACK_FP_X87
#endif
