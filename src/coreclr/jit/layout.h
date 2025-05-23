// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LAYOUT_H
#define LAYOUT_H

#include "jit.h"
#include "segmentlist.h"

// Builder for class layouts
//
class ClassLayoutBuilder
{
    friend class ClassLayout;
    friend class ClassLayoutTable;
    friend struct CustomLayoutKey;

    Compiler*    m_compiler;
    BYTE*        m_gcPtrs = nullptr;
    unsigned     m_size;
    unsigned     m_gcPtrCount = 0;
    SegmentList* m_nonPadding = nullptr;
#ifdef DEBUG
    const char* m_name      = "UNNAMED";
    const char* m_shortName = "UNNAMED";
#endif

    BYTE*        GetOrCreateGCPtrs();
    void         SetGCPtr(unsigned slot, CorInfoGCType type);
    SegmentList* GetOrCreateNonPadding();
public:
    // Create a class layout builder.
    //
    ClassLayoutBuilder(Compiler* compiler, unsigned size);

    void SetGCPtrType(unsigned slot, var_types type);
    void CopyGCInfoFrom(unsigned offset, ClassLayout* layout);
    void CopyPaddingFrom(unsigned offset, ClassLayout* layout);
    void AddPadding(const SegmentList::Segment& padding);
    void RemovePadding(const SegmentList::Segment& nonPadding);

#ifdef DEBUG
    void SetName(const char* name, const char* shortName);
    void CopyNameFrom(ClassLayout* layout, const char* prefix);
#endif

    static ClassLayoutBuilder BuildArray(Compiler* compiler, CORINFO_CLASS_HANDLE arrayType, unsigned length);
};

// Encapsulates layout information about a class (typically a value class but this can also be
// be used for reference classes when they are stack allocated). The class handle is optional,
// allowing the creation of custom layout objects having a specific size where the offsets of
// GC fields can be specified during creation.
class ClassLayout
{
private:

    // Class handle or NO_CLASS_HANDLE for "block" layouts.
    const CORINFO_CLASS_HANDLE m_classHandle;

    // Size of the layout in bytes (as reported by ICorJitInfo::getClassSize/getHeapClassSize
    // for non "block" layouts). For "block" layouts this may be 0 due to 0 being a valid size
    // for cpblk/initblk.
    const unsigned m_size;

    const unsigned m_isValueClass : 1;
    // The number of GC pointers in this layout. Since the maximum size is 2^32-1 the count
    // can fit in at most 30 bits.
    unsigned m_gcPtrCount : 30;

    // Array of CorInfoGCType (as BYTE) that describes the GC layout of the class.
    // For small classes the array is stored inline, avoiding an extra allocation
    // and the pointer size overhead.
    union
    {
        BYTE* m_gcPtrs;
        BYTE  m_gcPtrsArray[sizeof(BYTE*)];
    };

    class SegmentList* m_nonPadding = nullptr;

    // The normalized type to use in IR for block nodes with this layout.
    const var_types m_type;

    // Name of the layout
    INDEBUG(const char* m_name;)

    // Short name of the layout
    INDEBUG(const char* m_shortName;)

    // ClassLayout instances should only be obtained via ClassLayoutTable.
    friend class ClassLayoutTable;
    friend class ClassLayoutBuilder;
    friend struct CustomLayoutKey;

    ClassLayout(unsigned size)
        : m_classHandle(NO_CLASS_HANDLE)
        , m_size(size)
        , m_isValueClass(false)
        , m_gcPtrCount(0)
        , m_gcPtrs(nullptr)
        , m_type(TYP_STRUCT)
#ifdef DEBUG
        , m_name(size == 0 ? "Empty" : "Custom")
        , m_shortName(size == 0 ? "Empty" : "Custom")
#endif
    {
    }

    static ClassLayout* Create(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle);
    static ClassLayout* Create(Compiler* compiler, const ClassLayoutBuilder& builder);

    ClassLayout(CORINFO_CLASS_HANDLE classHandle,
                bool                 isValueClass,
                unsigned             size,
                var_types type       DEBUGARG(const char* className) DEBUGARG(const char* shortClassName))
        : m_classHandle(classHandle)
        , m_size(size)
        , m_isValueClass(isValueClass)
        , m_gcPtrCount(0)
        , m_gcPtrs(nullptr)
        , m_type(type)
#ifdef DEBUG
        , m_name(className)
        , m_shortName(shortClassName)
#endif
    {
        assert(size != 0);
    }
public:

    CORINFO_CLASS_HANDLE GetClassHandle() const
    {
        return m_classHandle;
    }

    bool IsCustomLayout() const
    {
        return m_classHandle == NO_CLASS_HANDLE;
    }

    bool IsBlockLayout() const
    {
        return IsCustomLayout() && !HasGCPtr();
    }

#ifdef DEBUG

    const char* GetClassName() const
    {
        return m_name;
    }

    const char* GetShortClassName() const
    {
        return m_shortName;
    }

#endif // DEBUG

    bool IsValueClass() const
    {
        return m_isValueClass;
    }

    unsigned GetSize() const
    {
        return m_size;
    }

    var_types GetType() const
    {
        return m_type;
    }

    //------------------------------------------------------------------------
    // GetRegisterType: Determine register type for the layout.
    //
    // Return Value:
    //    TYP_UNDEF if the layout is not enregistrable, register type otherwise.
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
        return m_gcPtrCount;
    }

    bool HasGCPtr() const
    {
        return m_gcPtrCount != 0;
    }

    bool HasGCByRef() const;

    bool IsStackOnly(Compiler* comp) const;

    bool IsGCPtr(unsigned slot) const
    {
        return GetGCPtr(slot) != TYPE_GC_NONE;
    }

    bool IsGCRef(unsigned slot) const
    {
        return GetGCPtr(slot) == TYPE_GC_REF;
    }

    bool IsGCByRef(unsigned slot) const
    {
        return GetGCPtr(slot) == TYPE_GC_BYREF;
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

    bool IntersectsGCPtr(unsigned offset, unsigned size) const;

    const SegmentList& GetNonPadding(Compiler* comp);

    static bool AreCompatible(const ClassLayout* layout1, const ClassLayout* layout2);

    bool CanAssignFrom(const ClassLayout* sourceLayout);

private:
    const BYTE* GetGCPtrs() const
    {
        return (GetSlotCount() > sizeof(m_gcPtrsArray)) ? m_gcPtrs : m_gcPtrsArray;
    }

    CorInfoGCType GetGCPtr(unsigned slot) const
    {
        assert(slot < GetSlotCount());

        if (m_gcPtrCount == 0)
        {
            return TYPE_GC_NONE;
        }

        return static_cast<CorInfoGCType>(GetGCPtrs()[slot]);
    }
};

#endif // LAYOUT_H
