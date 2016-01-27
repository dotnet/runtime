// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _ARRAY_H_
#define _ARRAY_H_

#define MAX_RANK 32        // If you make this bigger, you need to make MAX_CLASSNAME_LENGTH bigger too.
                           // if you have an 32 dim array with at least 2 elements in each dim that
                           // takes up 4Gig!!!  Thus this is a reasonable maximum.
// (Note: at the time of the above comment, the rank was 32, and
// MAX_CLASSNAME_LENGTH was 256.  I'm now changing MAX_CLASSNAME_LENGTH
// to 1024, but not changing MAX_RANK.)

class MethodTable;


#ifndef FEATURE_ARRAYSTUB_AS_IL

//======================================================================
// The following structures double as hash keys for the ArrayStubCache.
// Thus, it's imperative that there be no
// unused "pad" fields that contain unstable values.
#include <pshpack1.h>


// Specifies one index spec. This is used mostly to get the argument
// location done early when we still have a signature to work with.
struct ArrayOpIndexSpec
{
    UINT32  m_idxloc;         //if (m_fref) offset in ArgumentReg else base-frame offset into stack.
    UINT32  m_lboundofs;      //offset within array of lowerbound
    UINT32  m_lengthofs;      //offset within array of lengths
};


struct ArrayOpScript
{
    enum
    {
        LOAD     = 0,
        STORE    = 1,
        LOADADDR = 2,
    };


    // FLAGS
    enum
    {
        ISFPUTYPE            = 0x01,
        NEEDSWRITEBARRIER    = 0x02,
        HASRETVALBUFFER      = 0x04,
        NEEDSTYPECHECK       = 0x10,
    };

    //
    // these args have been reordered for better packing..
    //

    BYTE     m_rank;            // # of ArrayOpIndexSpec's
    BYTE     m_fHasLowerBounds; // if FALSE, all lowerbounds are 0
    BYTE     m_flags;
    BYTE     m_signed;          // whether to sign-extend or zero-extend (for short types)

    BYTE     m_op;              // STORE/LOAD/LOADADDR
    BYTE     m_pad1;

    UINT16   m_fRetBufLoc;      // if HASRETVALBUFFER, stack offset or argreg offset of retbuf ptr
    UINT16   m_fValLoc;         // for STORES, stack offset or argreg offset of value

    UINT16   m_cbretpop;        // how much to pop

    UINT32   m_elemsize;        // size in bytes of element.
    UINT     m_ofsoffirst;      // offset of first element
    INT      m_typeParamOffs;   // offset of type param
    CGCDesc* m_gcDesc;          // layout of GC stuff (0 if not needed)

    // Array of ArrayOpIndexSpec's follow (one for each dimension).

    const ArrayOpIndexSpec *GetArrayOpIndexSpecs() const
    {
        LIMITED_METHOD_CONTRACT;
        
        return (const ArrayOpIndexSpec *)(1+ this);
    }

    UINT Length() const
    {
        LIMITED_METHOD_CONTRACT;

        return sizeof(*this) + m_rank * sizeof(ArrayOpIndexSpec);
    }

};

#include <poppack.h>
//======================================================================

#endif // FEATURE_ARRAYSTUB_AS_IL


Stub *GenerateArrayOpStub(ArrayMethodDesc* pMD);



BOOL IsImplicitInterfaceOfSZArray(MethodTable *pIntfMT);
BOOL ArraySupportsBizarreInterface(ArrayTypeDesc *pArrayTypeDesc, MethodTable *pInterfaceMT);

MethodDesc* GetActualImplementationForArrayGenericIListOrIReadOnlyListMethod(MethodDesc *pItfcMeth, TypeHandle theT);

#endif// _ARRAY_H_

