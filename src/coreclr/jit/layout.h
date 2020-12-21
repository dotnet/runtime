#ifndef LAYOUT_H
#define LAYOUT_H

#include "jit.h"

// Encapsulates layout information about a class (typically a value class but this can also be
// be used for reference classes when they are stack allocated). The class handle is optional,
// allowing the creation of "block" layout objects having a specific size but lacking any other
// layout information. The JIT uses such layout objects in cases where a class handle is not
// available (cpblk/initblk operations) or not necessary (classes that do not contain GC pointers).
class ClassLayout
{
    // Class handle or NO_CLASS_HANDLE for "block" layouts.
    const CORINFO_CLASS_HANDLE m_classHandle;

    // Size of the layout in bytes (as reported by ICorJitInfo::getClassSize/getHeapClassSize
    // for non "block" layouts). For "block" layouts this may be 0 due to 0 being a valid size
    // for cpblk/initblk.
    const unsigned m_size;

    const unsigned m_isValueClass : 1;
    INDEBUG(unsigned m_gcPtrsInitialized : 1;)
    // The number of GC pointers in this layout. Since the the maximum size is 2^32-1 the count
    // can fit in at most 30 bits.
    unsigned m_gcPtrCount : 30;

    // Array of CorInfoGCType (as BYTE) that describes the GC layout of the class.
    // For small classes the array is stored inline, avoiding an extra allocation
    // and the pointer size overhead.
    union {
        BYTE* m_gcPtrs;
        BYTE  m_gcPtrsArray[sizeof(BYTE*)];
    };

#ifdef TARGET_AMD64
    // A layout that has its size artificially inflated to avoid stack corruption due to
    // bugs in user code - see Compiler::compQuirkForPPP for details.
    ClassLayout* m_pppQuirkLayout;
#endif

    // Class name as reported by ICorJitInfo::getClassName
    INDEBUG(const char* m_className;)

    // ClassLayout instances should only be obtained via ClassLayoutTable.
    friend class ClassLayoutTable;

    ClassLayout(unsigned size)
        : m_classHandle(NO_CLASS_HANDLE)
        , m_size(size)
        , m_isValueClass(false)
#ifdef DEBUG
        , m_gcPtrsInitialized(true)
#endif
        , m_gcPtrCount(0)
        , m_gcPtrs(nullptr)
#ifdef TARGET_AMD64
        , m_pppQuirkLayout(nullptr)
#endif
#ifdef DEBUG
        , m_className("block")
#endif
    {
    }

    static ClassLayout* Create(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle);

    ClassLayout(CORINFO_CLASS_HANDLE classHandle, bool isValueClass, unsigned size DEBUGARG(const char* className))
        : m_classHandle(classHandle)
        , m_size(size)
        , m_isValueClass(isValueClass)
#ifdef DEBUG
        , m_gcPtrsInitialized(false)
#endif
        , m_gcPtrCount(0)
        , m_gcPtrs(nullptr)
#ifdef TARGET_AMD64
        , m_pppQuirkLayout(nullptr)
#endif
#ifdef DEBUG
        , m_className(className)
#endif
    {
        assert(size != 0);
    }

    void InitializeGCPtrs(Compiler* compiler);

public:
#ifdef TARGET_AMD64
    // Get the layout for the PPP quirk - see Compiler::compQuirkForPPP for details.
    ClassLayout* GetPPPQuirkLayout(CompAllocator alloc);
#endif

    CORINFO_CLASS_HANDLE GetClassHandle() const
    {
        return m_classHandle;
    }

    bool IsBlockLayout() const
    {
        return m_classHandle == NO_CLASS_HANDLE;
    }

#ifdef DEBUG
    const char* GetClassName() const
    {
        return m_className;
    }
#endif

    bool IsValueClass() const
    {
        assert(!IsBlockLayout());

        return m_isValueClass;
    }

    unsigned GetSize() const
    {
        return m_size;
    }

    //------------------------------------------------------------------------
    // GetRegisterType: Determine register type for the layout.
    //
    // Return Value:
    //    TYP_UNDEF if the layout is enregistrable, register type otherwise.
    //
    var_types GetRegisterType() const
    {
        if (HasGCPtr())
        {
            return (GetSlotCount() == 1) ? GetGCPtrType(0) : TYP_UNDEF;
        }

        switch (m_size)
        {
            case 1:
                return TYP_UBYTE;
            case 2:
                return TYP_USHORT;
            case 4:
                return TYP_INT;
#ifdef TARGET_64BIT
            case 8:
                return TYP_LONG;
#endif
#ifdef FEATURE_SIMD
            // TODO: check TYP_SIMD12 profitability,
            // it will need additional support in `BuildStoreLoc`.
            case 16:
                return TYP_SIMD16;
#endif
            default:
                return TYP_UNDEF;
        }
    }

    unsigned GetSlotCount() const
    {
        return roundUp(m_size, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
    }

    unsigned GetGCPtrCount() const
    {
        assert(m_gcPtrsInitialized);

        return m_gcPtrCount;
    }

    bool HasGCPtr() const
    {
        assert(m_gcPtrsInitialized);

        return m_gcPtrCount != 0;
    }

    bool IsGCPtr(unsigned slot) const
    {
        return GetGCPtr(slot) != TYPE_GC_NONE;
    }

    var_types GetGCPtrType(unsigned slot) const
    {
        switch (GetGCPtr(slot))
        {
            case TYPE_GC_NONE:
                return TYP_I_IMPL;
            case TYPE_GC_REF:
                return TYP_REF;
            case TYPE_GC_BYREF:
                return TYP_BYREF;
            default:
                unreached();
        }
    }

    static bool AreCompatible(const ClassLayout* layout1, const ClassLayout* layout2);

private:
    const BYTE* GetGCPtrs() const
    {
        assert(m_gcPtrsInitialized);
        assert(!IsBlockLayout());

        return (GetSlotCount() > sizeof(m_gcPtrsArray)) ? m_gcPtrs : m_gcPtrsArray;
    }

    CorInfoGCType GetGCPtr(unsigned slot) const
    {
        assert(m_gcPtrsInitialized);
        assert(slot < GetSlotCount());

        if (m_gcPtrCount == 0)
        {
            return TYPE_GC_NONE;
        }

        return static_cast<CorInfoGCType>(GetGCPtrs()[slot]);
    }
};

#endif // LAYOUT_H
