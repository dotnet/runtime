// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************
 **                                                                         **
 ** Corhlpr.h -                                                                 **
 **                                                                         **
 *****************************************************************************/


#ifndef __CORHLPR_H__
#define __CORHLPR_H__

#if defined(_MSC_VER) && defined(_X86_) && !defined(FPO_ON)
#pragma optimize("y", on)		// Small critical routines, don't put in EBP frame 
#define FPO_ON 1
#define CORHLPR_TURNED_FPO_ON 1
#endif

#include "cor.h"
#include "corhdr.h"
#include "corerror.h"

// This header is consumed both within the runtime and externally. In the former
// case we need to wrap memory allocations, in the latter there is no
// infrastructure to support this. Detect which way we're building and provide a
// very simple abstraction layer (handles allocating bytes only).
#ifdef _BLD_CLR
#include "new.hpp"


#define NEW_NOTHROW(_bytes) new (nothrow) BYTE[_bytes]
#define NEW_THROWS(_bytes) new BYTE[_bytes]
void DECLSPEC_NORETURN ThrowOutOfMemory();
inline void DECLSPEC_NORETURN THROW_OUT_OF_MEMORY()
{
    ThrowOutOfMemory();
}
#else
#define NEW_NOTHROW(_bytes) new BYTE[_bytes]
#define NEW_THROWS(_bytes) __CorHlprNewThrows(_bytes)
static inline void DECLSPEC_NORETURN __CorHlprThrowOOM()
{
    RaiseException(STATUS_NO_MEMORY, 0, 0, NULL);
}
static inline BYTE *__CorHlprNewThrows(size_t bytes)
{
    BYTE *pbMemory = new BYTE[bytes];
    if (pbMemory == NULL)
        __CorHlprThrowOOM();
    return pbMemory;
}
inline void DECLSPEC_NORETURN THROW_OUT_OF_MEMORY()
{
    __CorHlprThrowOOM();
}
#endif


//*****************************************************************************
// There are a set of macros commonly used in the helpers which you will want
// to override to get richer behavior.  The following defines what is needed
// if you chose not to do the extra work.
//*****************************************************************************
#ifndef IfFailGoto
#define IfFailGoto(EXPR, LABEL) \
do { hr = (EXPR); if(FAILED(hr)) { goto LABEL; } } while (0)
#endif

#ifndef IfFailGo
#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#endif

#ifndef IfFailRet
#define IfFailRet(EXPR) do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)
#endif

#ifndef IfNullRet
#define IfNullRet(EXPR) do { if ((EXPR) == NULL){ return (E_OUTOFMEMORY); } } while (0)
#endif


#ifndef _ASSERTE
#define _ASSERTE(expr)
#endif

#ifndef COUNTOF
#define COUNTOF(a) (sizeof(a) / sizeof(*a))
#endif

#if !BIGENDIAN
#define VAL16(x) x
#define VAL32(x) x
#endif


//*****************************************************************************
//
//***** Macro to assist with cleaning up local static variables
//
//*****************************************************************************

#define CHECK_LOCAL_STATIC_VAR(x)   \
    x                                \

//*****************************************************************************
//
//***** Utility helpers
//
//*****************************************************************************


#define MAX_CLASSNAME_LENGTH 1024

//*****************************************************************************
//
//***** Signature helpers
//
//*****************************************************************************

inline bool isCallConv(unsigned sigByte, CorCallingConvention conv)
{
    return ((sigByte & IMAGE_CEE_CS_CALLCONV_MASK) == (unsigned) conv);
}

//*****************************************************************************
//
//***** File format helper classes
//
//*****************************************************************************



//*****************************************************************************
typedef struct tagCOR_ILMETHOD_SECT_SMALL : IMAGE_COR_ILMETHOD_SECT_SMALL {
        //Data follows
    const BYTE* Data() const 
    { 
        return(((const BYTE*) this) + sizeof(struct tagCOR_ILMETHOD_SECT_SMALL)); 
    }

    bool IsSmall() const
    { 
        return (Kind & CorILMethod_Sect_FatFormat) == 0;
    }

    bool More() const
    {
        return (Kind & CorILMethod_Sect_MoreSects) != 0;
    }
} COR_ILMETHOD_SECT_SMALL;


/************************************/
/* NOTE this structure must be DWORD aligned!! */
typedef struct tagCOR_ILMETHOD_SECT_FAT : IMAGE_COR_ILMETHOD_SECT_FAT {
        //Data follows
    const BYTE* Data() const 
    { 
        return(((const BYTE*) this) + sizeof(struct tagCOR_ILMETHOD_SECT_FAT)); 
    }

        //Endian-safe wrappers
    unsigned GetKind() const {
        /* return Kind; */
        return *(BYTE*)this;
    }
    void SetKind(unsigned kind) {
        /* Kind = kind; */
        *(BYTE*)this = (BYTE)kind;
    }

    unsigned GetDataSize() const {
        /* return DataSize; */
        BYTE* p = (BYTE*)this;
        return ((unsigned)*(p+1)) |
            (((unsigned)*(p+2)) << 8) |
            (((unsigned)*(p+3)) << 16);
    }
    void SetDataSize(unsigned datasize) {
        /* DataSize = dataSize; */
        BYTE* p = (BYTE*)this;
        *(p+1) = (BYTE)(datasize);
        *(p+2) = (BYTE)(datasize >> 8);
        *(p+3) = (BYTE)(datasize >> 16);
    }
} COR_ILMETHOD_SECT_FAT;

typedef struct tagCOR_ILMETHOD_SECT_EH_CLAUSE_FAT : public IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT {
    //Endian-safe wrappers
    CorExceptionFlag GetFlags() const {
        return (CorExceptionFlag)VAL32((unsigned)Flags);
    }
    void SetFlags(CorExceptionFlag flags) {
        Flags = (CorExceptionFlag)VAL32((unsigned)flags);
    }

    DWORD GetTryOffset() const {
        return VAL32(TryOffset);
    }
    void SetTryOffset(DWORD Offset) {
        TryOffset = VAL32(Offset);
    }

    DWORD GetTryLength() const {
        return VAL32(TryLength);
    }
    void SetTryLength(DWORD Length) {
        TryLength = VAL32(Length);
    }

    DWORD GetHandlerOffset() const {
        return VAL32(HandlerOffset);
    }
    void SetHandlerOffset(DWORD Offset) {
        HandlerOffset = VAL32(Offset);
    }

    DWORD GetHandlerLength() const {
        return VAL32(HandlerLength);
    }
    void SetHandlerLength(DWORD Length) {
        HandlerLength = VAL32(Length);
    }

    DWORD GetClassToken() const {
        return VAL32(ClassToken);
    }
    void SetClassToken(DWORD tok) {
        ClassToken = VAL32(tok);
    }

    DWORD GetFilterOffset() const {
        return VAL32(FilterOffset);
    }
    void SetFilterOffset(DWORD offset) {
        FilterOffset = VAL32(offset);
    }

} COR_ILMETHOD_SECT_EH_CLAUSE_FAT;

//*****************************************************************************
struct COR_ILMETHOD_SECT_EH_FAT : public COR_ILMETHOD_SECT_FAT {
    static unsigned Size(unsigned ehCount) {
        return (sizeof(COR_ILMETHOD_SECT_EH_FAT) +
                sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * (ehCount-1));
        }

    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT Clauses[1];     // actually variable size
};

typedef struct tagCOR_ILMETHOD_SECT_EH_CLAUSE_SMALL : public IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL {
    //Endian-safe wrappers
    CorExceptionFlag GetFlags() const {
        return (CorExceptionFlag)VAL16((SHORT)Flags);
    }
    void SetFlags(CorExceptionFlag flags) {
        Flags = (CorExceptionFlag)VAL16((SHORT)flags);
    }

    DWORD GetTryOffset() const {
        return VAL16(TryOffset);
    }
    void SetTryOffset(DWORD Offset) {
        _ASSERTE((Offset & ~0xffff) == 0);
        TryOffset = VAL16(Offset);
    }

    DWORD GetTryLength() const {
        return TryLength;
    }
    void SetTryLength(DWORD Length) {
        _ASSERTE((Length & ~0xff) == 0);
        TryLength = Length;
    }

    DWORD GetHandlerOffset() const {
        return VAL16(HandlerOffset);
    }
    void SetHandlerOffset(DWORD Offset) {
        _ASSERTE((Offset & ~0xffff) == 0);
        HandlerOffset = VAL16(Offset);
    }

    DWORD GetHandlerLength() const {
        return HandlerLength;
    }
    void SetHandlerLength(DWORD Length) {
        _ASSERTE((Length & ~0xff) == 0);
        HandlerLength = Length;
    }

    DWORD GetClassToken() const {
        return VAL32(ClassToken);
    }
    void SetClassToken(DWORD tok) {
        ClassToken = VAL32(tok);
    }

    DWORD GetFilterOffset() const {
        return VAL32(FilterOffset);
    }
    void SetFilterOffset(DWORD offset) {
        FilterOffset = VAL32(offset);
    }
} COR_ILMETHOD_SECT_EH_CLAUSE_SMALL;

//*****************************************************************************
struct COR_ILMETHOD_SECT_EH_SMALL : public COR_ILMETHOD_SECT_SMALL {
    static unsigned Size(unsigned ehCount) {
        return (sizeof(COR_ILMETHOD_SECT_EH_SMALL) +
                sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL) * (ehCount-1));
        }

    WORD Reserved;                                  // alignment padding
    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL Clauses[1];   // actually variable size
};


/************************************/
/* NOTE this structure must be DWORD aligned!! */
struct COR_ILMETHOD_SECT
{
    bool More() const           
    { 
        return((AsSmall()->Kind & CorILMethod_Sect_MoreSects) != 0); 
    }

    CorILMethodSect Kind() const
    { 
        return((CorILMethodSect) (AsSmall()->Kind & CorILMethod_Sect_KindMask)); 
    }

    const COR_ILMETHOD_SECT* Next() const   
    {
        if (!More()) return(0);
        return ((COR_ILMETHOD_SECT*)(((BYTE *)this) + DataSize()))->Align();
    }

    const BYTE* Data() const 
    {
        if (IsFat()) return(AsFat()->Data());
        return(AsSmall()->Data());
    }

    unsigned DataSize() const
    {
        if (Kind() == CorILMethod_Sect_EHTable) 
        {
            // VB and MC++ shipped with bug where they have not accounted for size of COR_ILMETHOD_SECT_EH_XXX
            // in DataSize. To avoid breaking these images, we will align the size of EH sections up. This works
            // because IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_XXX is bigger than COR_ILMETHOD_SECT_EH_XXX 
            // (see VSWhidbey #99031 and related bugs for details).

            if (IsFat())
                return Fat.Size(Fat.GetDataSize() / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
            else
                return Small.Size(Small.DataSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        }
        else
    {
        if (IsFat()) return(AsFat()->GetDataSize());
        return(AsSmall()->DataSize);
    }
    }

    friend struct COR_ILMETHOD;
    friend struct tagCOR_ILMETHOD_FAT;
    friend struct tagCOR_ILMETHOD_TINY;
    bool IsFat() const                            
    { 
        return((AsSmall()->Kind & CorILMethod_Sect_FatFormat) != 0); 
    }

    const COR_ILMETHOD_SECT* Align() const        
    { 
        return((COR_ILMETHOD_SECT*) ((((UINT_PTR) this) + 3) & ~3));  
    }

protected:
    const COR_ILMETHOD_SECT_FAT*   AsFat() const  
    { 
        return((COR_ILMETHOD_SECT_FAT*) this); 
    }

    const COR_ILMETHOD_SECT_SMALL* AsSmall() const
    { 
        return((COR_ILMETHOD_SECT_SMALL*) this); 
    }

public:
    // The body is either a COR_ILMETHOD_SECT_SMALL or COR_ILMETHOD_SECT_FAT
    // (as indicated by the CorILMethod_Sect_FatFormat bit
    union {
        COR_ILMETHOD_SECT_EH_SMALL Small;
        COR_ILMETHOD_SECT_EH_FAT Fat;
        };
};


/***********************************/
// exported functions (implementation in Format\Format.cpp:
extern "C" {
IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* __stdcall SectEH_EHClause(void *pSectEH, unsigned idx, IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* buff);
        // compute the size of the section (best format)
        // codeSize is the size of the method
    // deprecated
unsigned __stdcall SectEH_SizeWithCode(unsigned ehCount, unsigned codeSize);

    // will return worse-case size and then Emit will return actual size
unsigned __stdcall SectEH_SizeWorst(unsigned ehCount);

    // will return exact size which will match the size returned by Emit
unsigned __stdcall SectEH_SizeExact(unsigned ehCount, IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* clauses);

        // emit the section (best format);
unsigned __stdcall SectEH_Emit(unsigned size, unsigned ehCount,
                  IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* clauses,
                  BOOL moreSections, BYTE* outBuff,
                  ULONG* ehTypeOffsets = 0);
} // extern "C"


struct COR_ILMETHOD_SECT_EH : public COR_ILMETHOD_SECT
{
    unsigned EHCount() const 
    {
        return (unsigned)(IsFat() ? (Fat.GetDataSize() / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT)) :
                        (Small.DataSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL)));
    }

        // return one clause in its fat form.  Use 'buff' if needed
    const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* EHClause(unsigned idx, IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* buff) const
    { 
        return SectEH_EHClause((void *)this, idx, buff); 
    };
        // compute the size of the section (best format)
        // codeSize is the size of the method
    // deprecated
    unsigned static Size(unsigned ehCount, unsigned codeSize)
    { 
        return SectEH_SizeWithCode(ehCount, codeSize); 
    };

    // will return worse-case size and then Emit will return actual size
    unsigned static Size(unsigned ehCount)
    { 
        return SectEH_SizeWorst(ehCount); 
    };

    // will return exact size which will match the size returned by Emit
    unsigned static Size(unsigned ehCount, const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* clauses)
    { 
        return SectEH_SizeExact(ehCount, (IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)clauses);  
    };

        // emit the section (best format);
    unsigned static Emit(unsigned size, unsigned ehCount,
                  const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* clauses,
                  bool moreSections, BYTE* outBuff,
                  ULONG* ehTypeOffsets = 0)
    { 
        return SectEH_Emit(size, ehCount,
                           (IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)clauses,
                           moreSections, outBuff, ehTypeOffsets); 
    };
};


/***************************************************************************/
/* Used when the method is tiny (< 64 bytes), and there are no local vars */
typedef struct tagCOR_ILMETHOD_TINY : IMAGE_COR_ILMETHOD_TINY
{
    bool     IsTiny() const         
    { 
        return((Flags_CodeSize & (CorILMethod_FormatMask >> 1)) == CorILMethod_TinyFormat); 
    }

    unsigned GetCodeSize() const    
    { 
        return(((unsigned) Flags_CodeSize) >> (CorILMethod_FormatShift-1)); 
    }

    unsigned GetMaxStack() const    
    { 
        return(8); 
    }

    BYTE*    GetCode() const        
    { 
        return(((BYTE*) this) + sizeof(struct tagCOR_ILMETHOD_TINY)); 
    }

    DWORD    GetLocalVarSigTok() const  
    { 
        return(0); 
    }

    COR_ILMETHOD_SECT* GetSect() const 
    { 
        return(0); 
    }
} COR_ILMETHOD_TINY;


/************************************/
// This strucuture is the 'fat' layout, where no compression is attempted.
// Note that this structure can be added on at the end, thus making it extensible
typedef struct tagCOR_ILMETHOD_FAT : IMAGE_COR_ILMETHOD_FAT
{
        //Endian-safe wrappers
    unsigned GetSize() const {
        /* return Size; */
        BYTE* p = (BYTE*)this;
        return *(p+1) >> 4;
    }
    void SetSize(unsigned size) {
        /* Size = size; */
        BYTE* p = (BYTE*)this;
        *(p+1) = (BYTE)((*(p+1) & 0x0F) | (size << 4));
    }

    unsigned GetFlags() const {
        /* return Flags; */
        BYTE* p = (BYTE*)this;
        return ((unsigned)*(p+0)) | (( ((unsigned)*(p+1)) & 0x0F) << 8);
    }
    void SetFlags(unsigned flags) {
        /* flags = Flags; */
        BYTE* p = (BYTE*)this;
        *p = (BYTE)flags;
        *(p+1) = (BYTE)((*(p+1) & 0xF0) | ((flags >> 8) & 0x0F));
    }

    bool IsFat() const {
        /* return((IMAGE_COR_ILMETHOD_FAT::GetFlags() & CorILMethod_FormatMask) == CorILMethod_FatFormat); */
        return (*(BYTE*)this & CorILMethod_FormatMask) == CorILMethod_FatFormat;
    }

    unsigned GetMaxStack() const {
        /* return MaxStack; */
        return VAL16(*(USHORT*)((BYTE*)this+2));
    }
    void SetMaxStack(unsigned maxStack) {
        /* MaxStack = maxStack; */
        *(USHORT*)((BYTE*)this+2) = VAL16((USHORT)maxStack);
    }

    unsigned GetCodeSize() const        
    { 
        return VAL32(CodeSize); 
    }

    void SetCodeSize(DWORD Size)        
    { 
        CodeSize = VAL32(Size); 
    }

    mdToken  GetLocalVarSigTok() const      
    { 
        return VAL32(LocalVarSigTok); 
    }

    void SetLocalVarSigTok(mdSignature tok) 
    { 
        LocalVarSigTok = VAL32(tok); 
    }

    BYTE* GetCode() const {
        return(((BYTE*) this) + 4*GetSize());
    }

    bool More() const {
        // return (GetFlags() & CorILMethod_MoreSects) != 0;
        return (*(BYTE*)this & CorILMethod_MoreSects) != 0;
    }

    const COR_ILMETHOD_SECT* GetSect() const {
        if (!More()) return (0);
        return(((COR_ILMETHOD_SECT*) (GetCode() + GetCodeSize()))->Align());
    }
} COR_ILMETHOD_FAT;


extern "C" {
/************************************/
// exported functions (impl. Format\Format.cpp)
unsigned __stdcall IlmethodSize(COR_ILMETHOD_FAT* header, BOOL MoreSections);
        // emit the header (bestFormat) return amount emitted
unsigned __stdcall IlmethodEmit(unsigned size, COR_ILMETHOD_FAT* header,
                  BOOL moreSections, BYTE* outBuff);
}

struct COR_ILMETHOD
{
        // a COR_ILMETHOD header should not be decoded by hand.  Instead us
        // COR_ILMETHOD_DECODER to decode it.
    friend class COR_ILMETHOD_DECODER;

        // compute the size of the header (best format)
    unsigned static Size(const COR_ILMETHOD_FAT* header, bool MoreSections)
    { 
        return IlmethodSize((COR_ILMETHOD_FAT*)header,MoreSections); 
    };
        // emit the header (bestFormat) return amount emitted
    unsigned static Emit(unsigned size, const COR_ILMETHOD_FAT* header,
                  bool moreSections, BYTE* outBuff)
    { 
        return IlmethodEmit(size, (COR_ILMETHOD_FAT*)header, moreSections, outBuff); 
    };

//private:
    union
    {
        COR_ILMETHOD_TINY       Tiny;
        COR_ILMETHOD_FAT        Fat;
    };
        // Code follows the Header, then immedately after the code comes
        // any sections (COR_ILMETHOD_SECT).
};

extern "C" {
/***************************************************************************/
/* COR_ILMETHOD_DECODER is the only way functions internal to the EE should
   fetch data from a COR_ILMETHOD.  This way any dependancy on the file format
   (and the multiple ways of encoding the header) is centralized to the
   COR_ILMETHOD_DECODER constructor) */
    void __stdcall DecoderInit(void * pThis, COR_ILMETHOD* header);
    int  __stdcall DecoderGetOnDiskSize(void * pThis, COR_ILMETHOD* header);
} // extern "C"

class COR_ILMETHOD_DECODER : public COR_ILMETHOD_FAT
{
public:
    // This returns an uninitialized decoder, suitable for placement new but nothing
    // else. Use with caution.
    COR_ILMETHOD_DECODER() {}

    // Typically the ONLY way you should access COR_ILMETHOD is through
    // this constructor so format changes are easier.
    COR_ILMETHOD_DECODER(const COR_ILMETHOD* header) 
    { 
        DecoderInit(this,(COR_ILMETHOD*)header); 
    };

    // The above variant of the constructor can not do a 'complete' job, because
    // it can not look up the local variable signature meta-data token.
    // This method should be used when you have access to the Meta data API
    // If the construction fails, the 'Code' field is set to 0

    enum DecoderStatus {SUCCESS, FORMAT_ERROR, VERIFICATION_ERROR};

    // If we want the decoder to verify the that local signature is OK we
    // will pass a non-NULL value for wbStatus
    //
    // When using LazyInit we want ask that the local signature be verified
    // But if we fail verification we still need access to the 'Code' field
    // Because we may be able to demand SkipVerification and thus it was OK
    // to have had a verification error.

    COR_ILMETHOD_DECODER(COR_ILMETHOD* header, 
                         void *pInternalImport,
                         DecoderStatus* wbStatus);

    unsigned EHCount() const 
    {
        return (EH != 0) ? EH->EHCount() : 0;
    }

    unsigned GetHeaderSize() const
    {
        return GetCodeSize() + ((EH != 0) ? EH->DataSize() : 0);
    }

    // returns total size of method for use in copying
    int GetOnDiskSize(const COR_ILMETHOD* header) 
    { 
        return DecoderGetOnDiskSize(this,(COR_ILMETHOD*)header); 
    }

    // Flags        these are available because we inherit COR_ILMETHOD_FAT
    // MaxStack
    // CodeSize
    const BYTE *    Code;
    PCCOR_SIGNATURE LocalVarSig;        // pointer to signature blob, or 0 if none
    DWORD           cbLocalVarSig;      // size of dignature blob, or 0 if none
    const COR_ILMETHOD_SECT_EH * EH;    // eh table if any  0 if none
    const COR_ILMETHOD_SECT *    Sect;  // additional sections  0 if none
};  // class COR_ILMETHOD_DECODER

#if defined(CORHLPR_TURNED_FPO_ON)
#pragma optimize("", on)		// Go back to command line default optimizations
#undef CORHLPR_TURNED_FPO_ON
#undef FPO_ON
#endif

#endif // __CORHLPR_H__
