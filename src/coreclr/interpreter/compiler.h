// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPILER_H_
#define _COMPILER_H_

#include "intops.h"
#include "datastructs.h"
#include "enum_class_flags.h"
#include <new>
#include "failures.h"
#include "simdhash.h"
#include "intrinsics.h"

struct InterpException
{
    InterpException(const char* message, CorJitResult result)
        : m_message(message), m_result(result)
    {
        assert(result != CORJIT_OK);
    }
    const char* const m_message;
    const CorJitResult m_result;
};

class InterpreterStackMap;
class InterpCompiler;

class MemPoolAllocator
{
    InterpCompiler* const m_compiler;
    public:
    MemPoolAllocator(InterpCompiler* compiler) : m_compiler(compiler) {}
    void* Alloc(size_t sz) const;
    void Free(void* ptr) const;
};

class InterpDataItemIndexMap
{
    struct VarSizedData
    {
        VarSizedData(size_t size) : size(size)
        {
        }

        const size_t size;
        uint32_t SizeOf()
        {
            return (uint32_t)(size * sizeof(void*));
        }
    };

    template<typename T>
    struct VarSizedDataWithPayload : public VarSizedData
    {
        VarSizedDataWithPayload() : VarSizedData(sizeof(VarSizedDataWithPayload<T>)/sizeof(void*))
        {
            assert(SizeOf() == sizeof(VarSizedDataWithPayload<T>));
        }
        T payload;
    };

    dn_simdhash_ght_t* _hash = nullptr;
    TArray<void*, MemPoolAllocator> *_dataItems = nullptr; // Actual data items stored here, indexed by the value in the hash table. This pointer is owned by the InterpCompiler class.
    InterpCompiler* _compiler = nullptr;

    static unsigned int HashVarSizedData(const void *voidKey)
    {
        VarSizedData* key = (VarSizedData*)voidKey;
        return MurmurHash3_32((const uint8_t*)key, key->SizeOf(), 0);
    }

    static int32_t KeyEqualVarSizeData(const void * aVoid, const void * bVoid)
    {
        VarSizedData* keyA = (VarSizedData*)aVoid;
        VarSizedData* keyB = (VarSizedData*)bVoid;

        if (keyA->size != keyB->size)
            return 0;

        if (memcmp(aVoid, bVoid, keyA->SizeOf()) == 0)
            return 1;
        else
            return 0;
    }

    dn_simdhash_ght_t* GetHash()
    {
        if (_hash == nullptr)
            _hash = dn_simdhash_ght_new(HashVarSizedData, KeyEqualVarSizeData, 0, NULL);
        if (_hash == nullptr)
            NOMEM();

        return _hash;
    }

public:
    InterpDataItemIndexMap() = default;
    InterpDataItemIndexMap(const InterpDataItemIndexMap&) = delete;
    InterpDataItemIndexMap& operator=(const InterpDataItemIndexMap&) = delete;

    void Init(TArray<void*, MemPoolAllocator> *dataItems, InterpCompiler* compiler)
    {
        _compiler = compiler;
        _dataItems = dataItems;
    }

    int32_t GetDataItemIndex(const InterpGenericLookup& lookup)
    {
        const size_t sizeOfFieldsConcatenated = sizeof(InterpGenericLookup::offsets) +
                                          sizeof(InterpGenericLookup::indirections) +
                                          sizeof(InterpGenericLookup::sizeOffset) +
                                          sizeof(InterpGenericLookup::lookupType) +
                                          sizeof(InterpGenericLookup::signature);

        const size_t sizeOfStruct = sizeof(InterpGenericLookup);

        static_assert(sizeOfFieldsConcatenated == sizeOfStruct); // Assert that there is no padding in the struct, so a fixed size hash unaware of padding is safe to use
        return GetDataItemIndexForT(lookup);
    }

    int32_t GetDataItemIndex(void* lookup)
    {
        // TODO: this is a bit more expensive than necessary size we are allocating a full varsized struct for a single pointer
        // Consider optimizing this to use a seperate hashtable like a dn_simdhash_ptr_ptr_t if it becomes a bottleneck
        return GetDataItemIndexForT(lookup);
    }

private:
    template<typename T>
    int32_t GetDataItemIndexForT(const T& lookup);
};

TArray<char, MallocAllocator> PrintMethodName(COMP_HANDLE comp,
                             CORINFO_CLASS_HANDLE  clsHnd,
                             CORINFO_METHOD_HANDLE methHnd,
                             CORINFO_SIG_INFO*     sig,
                             bool                  includeAssembly,
                             bool                  includeClass,
                             bool                  includeClassInstantiation,
                             bool                  includeMethodInstantiation,
                             bool                  includeSignature,
                             bool                  includeReturnType,
                             bool                  includeThisSpecifier);

// Types that can exist on the IL execution stack. They are used only during
// IL import compilation stage.
enum StackType {
    StackTypeI4 = 0,
    StackTypeI8,
    StackTypeR4,
    StackTypeR8,
    StackTypeO,
    StackTypeVT,
    StackTypeByRef,
    StackTypeF,
#ifdef TARGET_64BIT
    StackTypeI = StackTypeI8
#else
    StackTypeI = StackTypeI4
#endif
};

// Types relevant for interpreter vars and opcodes. They are used in the final
// stages of the codegen and can be used during execution.
enum InterpType {
    InterpTypeI1 = 0,
    InterpTypeU1,
    InterpTypeI2,
    InterpTypeU2,
    InterpTypeI4,
    InterpTypeI8,
    InterpTypeR4,
    InterpTypeR8,
    InterpTypeO,
    InterpTypeVT,
    InterpTypeByRef,
    InterpTypeVoid,
#ifdef TARGET_64BIT
    InterpTypeI = InterpTypeI8
#else
    InterpTypeI = InterpTypeI4
#endif
};

#ifdef DEBUG
extern thread_local bool t_interpDump;

class InterpDumpScope
{
    bool m_prev;
    public:
    InterpDumpScope(bool enable)
    {
        m_prev = t_interpDump;
        t_interpDump = enable;
    }
    ~InterpDumpScope()
    {
        t_interpDump = m_prev;
    }
};

#define INTERP_DUMP(...)            \
    {                               \
        if (t_interpDump)           \
            printf(__VA_ARGS__);    \
    }
#else
#define INTERP_DUMP(...)
#endif

struct InterpInst;
struct InterpBasicBlock;

struct InterpCallInfo
{
    // For call instructions, this represents an array of all call arg vars
    // in the order they are pushed to the stack. This makes it easy to find
    // all source vars for these types of opcodes. This is terminated with -1.
    int32_t *pCallArgs;
    int32_t callOffset;
    union {
        // Array of call dependencies that need to be resolved before
        TSList<InterpInst*> *callDeps;
        // Stack end offset of call arguments
        int32_t callEndOffset;
    };
};

enum InterpInstFlags
{
    INTERP_INST_FLAG_CALL               = 0x01,
    // Flag used internally by the var offset allocator
    INTERP_INST_FLAG_ACTIVE_CALL        = 0x02
};

struct InterpInst
{
    InterpInst *pNext, *pPrev;
    union
    {
        InterpBasicBlock *pTargetBB; // target basic block for branch instructions
        InterpBasicBlock **ppTargetBBTable; // basic block table for switch instruction
        InterpCallInfo *pCallInfo; // additional information for call instructions
    } info;

    int32_t opcode;
    int32_t ilOffset;
    int32_t nativeOffset;
    uint32_t flags;
    int32_t dVar;
    int32_t sVars[3]; // Currently all instructions have at most 3 sregs

    int32_t data[];

    void SetDVar(int32_t dv)
    {
        dVar = dv;
    }

    void SetSVar(int32_t sv1)
    {
        sVars[0] = sv1;
    }

    void SetSVars2(int32_t sv1, int32_t sv2)
    {
        sVars[0] = sv1;
        sVars[1] = sv2;
    }

    void SetSVars3(int32_t sv1, int32_t sv2, int32_t sv3)
    {
        sVars[0] = sv1;
        sVars[1] = sv2;
        sVars[2] = sv3;
    }
};

#define CALL_ARGS_SVAR  -2
#define CALL_ARGS_TERMINATOR -1

struct StackInfo;

enum InterpBBState
{
    BBStateNotEmitted,
    BBStateEmitting,
    BBStateEmitted
};

enum InterpBBClauseType
{
    BBClauseNone,
    BBClauseTry,
    BBClauseCatch,
    BBClauseFinally,
    BBClauseFilter,
};

struct InterpBasicBlock
{
    int32_t index;
    int32_t ilOffset, nativeOffset;
    int32_t nativeEndOffset;
    int32_t stackHeight;
    StackInfo *pStackState;

    InterpInst *pFirstIns, *pLastIns;
    InterpBasicBlock *pNextBB;

    // * If this basic block is a finally, this points to a finally call island that is located where the finally
    //   was before all funclets were moved to the end of the method.
    // * If this basic block is a call island, this points to the next finally call island basic block.
    // * Otherwise, this is NULL.
    InterpBasicBlock *pFinallyCallIslandBB;
    // Target of a leave instruction that is located in this basic block. NULL if there is none.
    InterpBasicBlock *pLeaveTargetBB;

    int inCount, outCount;
    InterpBasicBlock **ppInBBs;
    InterpBasicBlock **ppOutBBs;

    InterpBBState emitState;

    // Type of the innermost try block, catch, filter, or finally that contains this basic block.
    uint8_t clauseType;

    // True indicates that this basic block is the first block of a filter, catch or filtered handler funclet.
    bool isFilterOrCatchFuncletEntry;

    // If this basic block is a catch or filter funclet entry, this is the index of the variable
    // that holds the exception object.
    int clauseVarIndex;

    // Number of catch, filter or finally clauses that overlap with this basic block.
    int32_t overlappingEHClauseCount;

    InterpBasicBlock(int32_t index) : InterpBasicBlock(index, 0) { }

    InterpBasicBlock(int32_t index, int32_t ilOffset)
    {
        this->index = index;
        this->ilOffset = ilOffset;
        nativeOffset = -1;
        nativeEndOffset = -1;
        stackHeight = -1;

        pFirstIns = pLastIns = NULL;
        pNextBB = NULL;
        pFinallyCallIslandBB = NULL;
        pLeaveTargetBB = NULL;

        inCount = 0;
        outCount = 0;

        emitState = BBStateNotEmitted;

        clauseType = BBClauseNone;
        isFilterOrCatchFuncletEntry = false;
        clauseVarIndex = -1;
        overlappingEHClauseCount = 0;
    }
};

struct InterpVar
{
    CORINFO_CLASS_HANDLE clsHnd;
    InterpType interpType;
    int offset;
    int size;
    // live_start and live_end are used by the offset allocator
    InterpInst* liveStart;
    InterpInst* liveEnd;
    // index of first basic block where this var is used
    int bbIndex;
    // If var is callArgs, this is the call instruction using it.
    // Only used by the var offset allocator
    InterpInst *call;

    unsigned int callArgs : 1; // Var used as argument to a call
    unsigned int noCallArgs : 1; // Var can't be used as argument to a call, needs to be copied to temp
    unsigned int global : 1; // Dedicated stack offset throughout method execution
    unsigned int ILGlobal : 1; // Args and IL locals
    unsigned int alive : 1; // Used internally by the var offset allocator
    unsigned int pinned : 1; // Indicates that the var had the 'pinned' modifier in IL

    InterpVar(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int size)
    {
        this->interpType = interpType;
        this->clsHnd = clsHnd;
        this->size = size;
        offset = -1;
        liveStart = NULL;
        bbIndex = -1;

        callArgs = false;
        noCallArgs = false;
        global = false;
        ILGlobal = false;
        alive = false;
        pinned = false;
    }
};

struct StackInfo
{
    StackType type;
    CORINFO_CLASS_HANDLE clsHnd;

    // The var associated with the value of this stack entry. Every time we push on
    // the stack a new var is created.
    int var;

    StackInfo(StackType type, CORINFO_CLASS_HANDLE clsHnd, int var)
    {
        this->type = type;
        this->clsHnd = clsHnd;
        this->var = var;
    }
};

enum RelocType
{
    RelocLongBranch,
    RelocSwitch
};

struct Reloc
{
    RelocType type;
    // For branch relocation, how many sVar slots to skip
    int skip;
    // Base offset that the relative offset to be embedded in IR applies to
    int32_t offset;
    InterpBasicBlock *pTargetBB;

    Reloc(RelocType type, int32_t offset, InterpBasicBlock *pTargetBB, int skip)
    {
        this->type = type;
        this->offset = offset;
        this->pTargetBB = pTargetBB;
        this->skip = skip;
    }
};


class InterpIAllocator;

// Entry of the table where for each leave instruction we store the first finally call island
// to be executed when the leave instruction is executed.
struct LeavesTableEntry
{
    // offset of the CEE_LEAVE instruction
    int32_t ilOffset;
    // The BB of the call island BB that will be the first to call when the leave
    // instruction is executed.
    InterpBasicBlock *pFinallyCallIslandBB;
};

class InterpCompiler
{
    friend class InterpIAllocator;
    friend class InterpGcSlotAllocator;

private:
#ifdef DEBUG
    InterpDumpScope m_dumpScope;
#endif
    CORINFO_METHOD_HANDLE m_methodHnd;
    CORINFO_MODULE_HANDLE m_compScopeHnd;
    COMP_HANDLE m_compHnd;
    CORINFO_METHOD_INFO* m_methodInfo;

    void DeclarePointerIsClass(CORINFO_CLASS_HANDLE clsHnd)
    {
#ifdef DEBUG
        void *ptr = (void*)clsHnd;
        if (!PointerInNameMap(ptr))
        {
            AddPointerToNameMap(ptr, PointerIsClassHandle);
        }
#endif // DEBUG
    }

    void DeclarePointerIsMethod(CORINFO_METHOD_HANDLE methodHnd)
    {
#ifdef DEBUG
        void *ptr = (void*)methodHnd;
        if (!PointerInNameMap(ptr))
        {
            AddPointerToNameMap(ptr, PointerIsMethodHandle);
        }
#endif // DEBUG
    }

    void DeclarePointerIsString(void* stringLiteral)
    {
#ifdef DEBUG
        void *ptr = (void*)stringLiteral;
        if (!PointerInNameMap(ptr))
        {
            AddPointerToNameMap(ptr, PointerIsStringLiteral);
        }
#endif // DEBUG
    }

    CORINFO_CLASS_HANDLE m_classHnd;
#ifdef DEBUG
    TArray<char, MallocAllocator> m_methodName;

    const char* PointerIsClassHandle = (const char*)0x1;
    const char* PointerIsMethodHandle = (const char*)0x2;
    const char* PointerIsStringLiteral = (const char*)0x3;

    dn_simdhash_ptr_ptr_holder m_pointerToNameMap;
    bool PointerInNameMap(void* ptr)
    {
        return dn_simdhash_ptr_ptr_try_get_value(m_pointerToNameMap.GetValue(), ptr, NULL) != 0;
    }
    void AddPointerToNameMap(void* ptr, const char* name)
    {
        checkNoError(dn_simdhash_ptr_ptr_try_add(m_pointerToNameMap.GetValue(), ptr, (void*)name));
    }
    void PrintNameInPointerMap(void* ptr);
#endif // DEBUG

    dn_simdhash_ptr_ptr_holder m_stackmapsByClass;
    InterpreterStackMap* GetInterpreterStackMap(CORINFO_CLASS_HANDLE classHandle);

    static int32_t InterpGetMovForType(InterpType interpType, bool signExtend);

    uint8_t* m_ip;
    uint8_t* m_pILCode;
    int32_t m_ILCodeSize;
    int32_t m_currentILOffset;
    InterpInst* m_pInitLocalsIns;

    // If the method has a hidden argument, GenerateCode allocates a var to store it and
    //  populates the var at method entry
    int32_t m_hiddenArgumentVar;

    // Table of mappings of leave instructions to the first finally call island the leave
    // needs to execute.
    TArray<LeavesTableEntry, MemPoolAllocator> m_leavesTable;

    // This represents a mapping from indexes to pointer sized data. During compilation, an
    // instruction can request an index for some data (like a MethodDesc pointer), that it
    // will then embed in the instruction stream. The data item table will be referenced
    // from the interpreter code header during execution.
    TArray<void*, MemPoolAllocator> m_dataItems;

    InterpDataItemIndexMap m_genericLookupToDataItemIndex;
    int32_t GetDataItemIndex(void* data)
    {
        return m_genericLookupToDataItemIndex.GetDataItemIndex(data);
    }
    int32_t GetDataItemIndex(const InterpGenericLookup& data)
    {
        return m_genericLookupToDataItemIndex.GetDataItemIndex(data);
    }

    void* GetDataItemAtIndex(int32_t index);
    void* GetAddrOfDataItemAtIndex(int32_t index);
    int32_t GetMethodDataItemIndex(CORINFO_METHOD_HANDLE mHandle);
    int32_t GetDataForHelperFtn(CorInfoHelpFunc ftn);

    void GenerateCode(CORINFO_METHOD_INFO* methodInfo);
    InterpBasicBlock* GenerateCodeForFinallyCallIslands(InterpBasicBlock *pNewBB, InterpBasicBlock *pPrevBB);
    void PatchInitLocals(CORINFO_METHOD_INFO* methodInfo);

    void                    ResolveToken(uint32_t token, CorInfoTokenKind tokenKind, CORINFO_RESOLVED_TOKEN *pResolvedToken);
    CORINFO_METHOD_HANDLE   ResolveMethodToken(uint32_t token);
    CORINFO_CLASS_HANDLE    ResolveClassToken(uint32_t token);
    CORINFO_CLASS_HANDLE    getClassFromContext(CORINFO_CONTEXT_HANDLE context);
    int                     getParamArgIndex(); // Get the index into the m_pVars array of the Parameter argument. This is either the this pointer, a methoddesc or a class handle

    struct InterpEmbedGenericResult
    {
        // If var is != -1, then the var holds the result of the lookup
        int var = -1;
        // If var == -1, then the data item holds the result of the lookup
        int dataItemIndex = -1;
    };

    enum class GenericHandleEmbedOptions
    {
        support_use_as_flags = -1, // Magic value which in combination with enum_class_flags.h allows the use of bitwise operations and the HasFlag helper method

        None = 0,
        VarOnly = 1,
        EmbedParent = 2,
    };

    enum class HelperArgType
    {
        GenericResolution,
        Value
    };

    struct TokenArg
    {
        CORINFO_RESOLVED_TOKEN* token;
        InterpCompiler::GenericHandleEmbedOptions options;
    };

    struct GenericHandleData
    {
        GenericHandleData(int genericVar, int dataItemIndex)
            : argType(HelperArgType::GenericResolution), genericVar(genericVar), dataItemIndex(dataItemIndex) {}

        GenericHandleData(int dataItemIndex)
            : argType(HelperArgType::Value), genericVar(-1), dataItemIndex(dataItemIndex) {}

        GenericHandleData() = default;

        HelperArgType argType = HelperArgType::Value;
        int genericVar = -1; // This will be set to the var of the generic context argument if argType == HelperArgType::GenericResolution
        int dataItemIndex = 0;
    };

    GenericHandleData GenericHandleToGenericHandleData(const CORINFO_GENERICHANDLE_RESULT& embedInfo);
    InterpEmbedGenericResult EmitGenericHandle(CORINFO_RESOLVED_TOKEN* resolvedToken, GenericHandleEmbedOptions options);

    // Do a generic handle lookup and acquire the result as either a var or a data item.
    int EmitGenericHandleAsVar(const CORINFO_GENERICHANDLE_RESULT &embedInfo);

    // Emit a generic dictionary lookup and push the result onto the interpreter stack
    void CopyToInterpGenericLookup(InterpGenericLookup* dst, const CORINFO_RUNTIME_LOOKUP *src);
    void EmitPushCORINFO_LOOKUP(const CORINFO_LOOKUP& lookup);
    void EmitPushLdvirtftn(int thisVar, CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);
    void EmitPushHelperCall_2(const CorInfoHelpFunc ftn, const CORINFO_GENERICHANDLE_RESULT& arg1, int arg2, StackType resultStackType, CORINFO_CLASS_HANDLE clsHndStack);
    void EmitPushHelperCall_Addr2(const CorInfoHelpFunc ftn, const CORINFO_GENERICHANDLE_RESULT& arg1, int arg2, StackType resultStackType, CORINFO_CLASS_HANDLE clsHndStack);
    void EmitPushHelperCall(const CorInfoHelpFunc ftn, const CORINFO_GENERICHANDLE_RESULT& arg1, StackType resultStackType, CORINFO_CLASS_HANDLE clsHndStack);
    void EmitPushUnboxAny(const CORINFO_GENERICHANDLE_RESULT& arg1, int arg2, StackType resultStackType, CORINFO_CLASS_HANDLE clsHndStack);
    void EmitPushUnboxAnyNullable(const CORINFO_GENERICHANDLE_RESULT& arg1, int arg2, StackType resultStackType, CORINFO_CLASS_HANDLE clsHndStack);

    void* AllocMethodData(size_t numBytes);
public:
    // FIXME MemPool allocation currently leaks. We need to add an allocator and then
    // free all memory when method is finished compilling.
    void* AllocMemPool(size_t numBytes);
    void* AllocMemPool0(size_t numBytes);
    MemPoolAllocator GetMemPoolAllocator() { return MemPoolAllocator(this); }

private:
    void* AllocTemporary(size_t numBytes);
    void* AllocTemporary0(size_t numBytes);
    void* ReallocTemporary(void* ptr, size_t numBytes);
    void  FreeTemporary(void* ptr);

    // Instructions
    InterpBasicBlock *m_pCBB, *m_pEntryBB;
    InterpInst* m_pLastNewIns;

    int32_t     GetInsLength(InterpInst *pIns);
    bool        InsIsNop(InterpInst *pIns);
    InterpInst* AddIns(int opcode);
    InterpInst* NewIns(int opcode, int len);
    InterpInst* AddInsExplicit(int opcode, int dataLen);
    InterpInst* InsertInsBB(InterpBasicBlock *pBB, InterpInst *pPrevIns, int opcode);
    InterpInst* InsertIns(InterpInst *pPrevIns, int opcode);
    InterpInst* FirstRealIns(InterpBasicBlock *pBB);
    InterpInst* NextRealIns(InterpInst *pIns);
    InterpInst* PrevRealIns(InterpInst *pIns);
    void        ClearIns(InterpInst *pIns);

    void        ForEachInsSVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*));
    void        ForEachInsVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*));

    // Basic blocks
    int m_BBCount = 0;
    InterpBasicBlock**  m_ppOffsetToBB;

    ICorDebugInfo::OffsetMapping* m_pILToNativeMap = NULL;
#ifdef DEBUG
    int32_t* m_pNativeMapIndexToILOffset = NULL;
#endif
    int32_t m_ILToNativeMapSize = 0;

    InterpBasicBlock*   AllocBB(int32_t ilOffset);
    InterpBasicBlock*   GetBB(int32_t ilOffset);
    void                LinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);
    void                UnlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);

    void    EmitBranch(InterpOpcode opcode, int ilOffset);
    void    EmitOneArgBranch(InterpOpcode opcode, int ilOffset, int insSize);
    void    EmitTwoArgBranch(InterpOpcode opcode, int ilOffset, int insSize);
    void    EmitBranchToBB(InterpOpcode opcode, InterpBasicBlock *pTargetBB);

    void    EmitBBEndVarMoves(InterpBasicBlock *pTargetBB);
    void    InitBBStackState(InterpBasicBlock *pBB);
    void    UnlinkUnreachableBBlocks();

    // Vars
    InterpVar *m_pVars = NULL;
    int32_t m_varsSize = 0;
    int32_t m_varsCapacity = 0;
    int32_t m_numILVars = 0;
    int32_t m_paramArgIndex = 0; // Index of the type parameter argument in the m_pVars array.
    // For each catch or filter clause, we create a variable that holds the exception object.
    // This is the index of the first such variable.
    int32_t m_clauseVarsIndex = 0;
    bool m_shadowCopyOfThisPointerActuallyNeeded = false;
    bool m_shadowCopyOfThisPointerHasVar = false;

    int32_t CreateVarExplicit(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int size);

    int32_t m_totalVarsStackSize, m_globalVarsWithRefsStackTop;
    int32_t m_paramAreaOffset = 0;
    int32_t m_ILLocalsOffset, m_ILLocalsSize;
    void    AllocVarOffsetCB(int *pVar, void *pData);
    int32_t AllocVarOffset(int var, int32_t *pPos);
    int32_t GetLiveStartOffset(int var);
    int32_t GetLiveEndOffset(int var);

    int32_t GetInterpTypeStackSize(CORINFO_CLASS_HANDLE clsHnd, InterpType interpType, int32_t *pAlign);
    void    CreateILVars();

    void CreateNextLocalVar(int iArgToSet, CORINFO_CLASS_HANDLE argClass, InterpType interpType, int32_t *pOffset, bool pinned = false);

    // Stack
    StackInfo *m_pStackPointer, *m_pStackBase;
    int32_t m_stackCapacity;

    void CheckStackHelper(int n);
    void CheckStackExact(int n);
    void EnsureStack(int additional);
    void PushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size);
    void PushStackType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd);
    void PushInterpType(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd);
    void PushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size);
    void ConvertFloatingPointStackEntryToStackType(StackInfo* entry, StackType type);

    // Code emit
    void    EmitConv(StackInfo *sp, StackType type, InterpOpcode convOp);
    void    EmitLoadVar(int var);
    void    EmitStoreVar(int var);
    void    EmitBinaryArithmeticOp(int32_t opBase);
    void    EmitUnaryArithmeticOp(int32_t opBase);
    void    EmitShiftOp(int32_t opBase);
    void    EmitCompareOp(int32_t opBase);
    void    EmitCall(CORINFO_RESOLVED_TOKEN* pConstrainedToken, bool readonly, bool tailcall, bool newObj, bool isCalli);
    void    EmitCalli(bool isTailCall, void* calliCookie, int callIFunctionPointerVar, CORINFO_SIG_INFO* callSiteSig);
    bool    EmitNamedIntrinsicCall(NamedIntrinsic ni, bool nonVirtualCall, CORINFO_CLASS_HANDLE clsHnd, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO sig);
    void    EmitLdind(InterpType type, CORINFO_CLASS_HANDLE clsHnd, int32_t offset);
    void    EmitStind(InterpType type, CORINFO_CLASS_HANDLE clsHnd, int32_t offset, bool reverseSVarOrder);
    void    EmitLdelem(int32_t opcode, InterpType type);
    void    EmitStelem(InterpType type);
    void    EmitStaticFieldAddress(CORINFO_FIELD_INFO *pFieldInfo, CORINFO_RESOLVED_TOKEN *pResolvedToken);
    void    EmitStaticFieldAccess(InterpType interpFieldType, CORINFO_FIELD_INFO *pFieldInfo, CORINFO_RESOLVED_TOKEN *pResolvedToken, bool isLoad);
    void    EmitLdLocA(int32_t var);
    void    EmitBox(StackInfo* pStackInfo, const CORINFO_GENERICHANDLE_RESULT &boxType, bool argByRef);

    // Var Offset allocator
    TArray<InterpInst*, MemPoolAllocator> *m_pActiveCalls;
    TArray<int32_t, MemPoolAllocator> *m_pActiveVars;
    TSList<InterpInst*> *m_pDeferredCalls;

    int32_t AllocGlobalVarOffset(int var);
    void    SetVarLiveRange(int32_t var, InterpInst* ins);
    void    SetVarLiveRangeCB(int32_t *pVar, void *pData);
    void    InitializeGlobalVar(int32_t var, int bbIndex);
    void    InitializeGlobalVarCB(int32_t *pVar, void *pData);
    void    InitializeGlobalVars();
    void    EndActiveCall(InterpInst *call);
    void    CompactActiveVars(int32_t *current_offset);

    // Passes
    int32_t* m_pMethodCode;
    int32_t m_methodCodeSize; // code size measured in int32_t slots, instead of bytes

    void AllocOffsets();
    int32_t ComputeCodeSize();
    uint32_t ConvertOffset(int32_t offset);
    void EmitCode();
    int32_t* EmitBBCode(int32_t *ip, InterpBasicBlock *bb, TArray<Reloc*, MemPoolAllocator> *relocs);
    int32_t* EmitCodeIns(int32_t *ip, InterpInst *pIns, TArray<Reloc*, MemPoolAllocator> *relocs);
    void PatchRelocations(TArray<Reloc*, MemPoolAllocator> *relocs);
    InterpMethod* CreateInterpMethod();
    void CreateBasicBlocks(CORINFO_METHOD_INFO* methodInfo);
    void InitializeClauseBuildingBlocks(CORINFO_METHOD_INFO* methodInfo);
    void CreateFinallyCallIslandBasicBlocks(CORINFO_METHOD_INFO* methodInfo, int32_t leaveOffset, InterpBasicBlock* pLeaveTargetBB);
    void GetNativeRangeForClause(uint32_t startILOffset, uint32_t endILOffset, int32_t *nativeStartOffset, int32_t* nativeEndOffset);

    // Debug
    void PrintClassName(CORINFO_CLASS_HANDLE cls);
    void PrintMethodName(CORINFO_METHOD_HANDLE method);
    void PrintCode();
    void PrintBBCode(InterpBasicBlock *pBB);
    void PrintIns(InterpInst *ins);
    void PrintPointer(void* pointer);
    void PrintHelperFtn(int32_t _data);
    void PrintInsData(InterpInst *ins, int32_t offset, const int32_t *pData, int32_t opcode);
    void PrintCompiledCode();
    void PrintCompiledIns(const int32_t *ip, const int32_t *start);
public:

    InterpCompiler(COMP_HANDLE compHnd, CORINFO_METHOD_INFO* methodInfo);

    InterpMethod* CompileMethod();
    void BuildGCInfo(InterpMethod *pInterpMethod);
    void BuildEHInfo();

    int32_t* GetCode(int32_t *pCodeSize);
};

/*****************************************************************************
 *  operator new
 *
 *  Uses the compiler's AllocMemPool0, which will eventually free automatically at the end of compilation (doesn't yet).
 */

inline void* operator new(size_t sz, InterpCompiler* compiler)
{
    return compiler->AllocMemPool0(sz);
}

inline void* operator new[](size_t sz, InterpCompiler* compiler)
{
    return compiler->AllocMemPool0(sz);
}

inline void operator delete(void* ptr, InterpCompiler* compiler)
{
    // Nothing to do, memory will be freed when the compiler is destroyed
}

inline void operator delete[](void* ptr, InterpCompiler* compiler)
{
    // Nothing to do, memory will be freed when the compiler is destroyed
}

template<typename T>
int32_t InterpDataItemIndexMap::GetDataItemIndexForT(const T& lookup)
{
    VarSizedDataWithPayload<T> key;
    key.payload = lookup;

    dn_simdhash_ght_t* hash = GetHash();

    void* resultAsPtr = nullptr;
    if (dn_simdhash_ght_try_get_value(hash, (void*)&key, &resultAsPtr))
    {
        return (int32_t)(size_t)resultAsPtr;
    }

    // Assert that there is no padding in the struct, so a fixed size hash unaware of padding is safe to use
    static_assert(sizeof(VarSizedData) == sizeof(void*));
    static_assert(sizeof(T) % sizeof(void*) == 0);
    static_assert(sizeof(VarSizedDataWithPayload<T>) == sizeof(T) + sizeof(void*));

    void** LookupAsPtrs = (void**)&lookup;
    int32_t dataItemIndex = _dataItems->Add(LookupAsPtrs[0]);
    for (unsigned i = 1; i < sizeof(T) / sizeof(void*); i++)
    {
        _dataItems->Add(LookupAsPtrs[i]);
    }

    void* hashItemPayload = _compiler->AllocMemPool0(sizeof(VarSizedDataWithPayload<T>));
    if (hashItemPayload == nullptr)
        NOMEM();

    VarSizedDataWithPayload<T>* pLookup = new(hashItemPayload) VarSizedDataWithPayload<T>();
    memcpy(&pLookup->payload, &lookup, sizeof(T));

    checkAddedNew(dn_simdhash_ght_try_insert(
        hash, (void*)pLookup, (void*)(size_t)dataItemIndex, DN_SIMDHASH_INSERT_MODE_ENSURE_UNIQUE
    ));

    return dataItemIndex;
}

#endif //_COMPILER_H_
