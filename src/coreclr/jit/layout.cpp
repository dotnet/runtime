// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "layout.h"
#include "compiler.h"

// Keeps track of layout objects associated to class handles or block sizes. A layout is usually
// referenced by a pointer (ClassLayout*) but can also be referenced by a number (unsigned,
// FirstLayoutNum-based), when space constraints or other needs make numbers more appealing.
// Layout objects are immutable and there's always a 1:1 mapping between class handles/block sizes,
// pointers and numbers (e.g. class handle equality implies ClassLayout pointer equality).
class ClassLayoutTable
{
    // Each layout is assigned a number, starting with TYP_UNKNOWN + 1. This way one could use a single
    // unsigned value to represent the notion of type - values below TYP_UNKNOWN are var_types and values
    // above it are struct layouts.
    static constexpr unsigned FirstLayoutNum = TYP_UNKNOWN + 1;

    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, unsigned>               BlkLayoutIndexMap;
    typedef JitHashTable<CORINFO_CLASS_HANDLE, JitPtrKeyFuncs<CORINFO_CLASS_STRUCT_>, unsigned> ObjLayoutIndexMap;

    static const int NumInlineLayouts = 3;
    alignas(alignof(ClassLayout)) char m_inlineLayouts[sizeof(ClassLayout) * NumInlineLayouts];
    ClassLayout** m_layoutLargeArray = nullptr;
    BlkLayoutIndexMap* m_blkLayoutMap = nullptr;
    ObjLayoutIndexMap* m_objLayoutMap = nullptr;
    // The number of layout objects stored in this table.
    unsigned m_layoutCount = 0;
    // The capacity of m_layoutLargeArray (when more than 3 layouts are stored).
    unsigned m_layoutLargeCapacity = 0;

public:
    // Get the layout number (FirstLayoutNum-based) of the specified layout.
    unsigned GetLayoutNum(ClassLayout* layout) const
    {
        return GetLayoutIndex(layout) + FirstLayoutNum;
    }

    // Get the layout having the specified layout number (FirstLayoutNum-based)
    ClassLayout* GetLayoutByNum(unsigned num) const
    {
        assert(num >= FirstLayoutNum);
        return GetLayoutByIndex(num - FirstLayoutNum);
    }

    // Get the layout having the specified size but no class handle.
    ClassLayout* GetBlkLayout(Compiler* compiler, unsigned blockSize)
    {
        return GetLayoutByIndex(GetBlkLayoutIndex(compiler, blockSize));
    }

    // Get the number of a layout having the specified size but no class handle.
    unsigned GetBlkLayoutNum(Compiler* compiler, unsigned blockSize)
    {
        return GetBlkLayoutIndex(compiler, blockSize) + FirstLayoutNum;
    }

    // Get the layout for the specified class handle.
    ClassLayout* GetObjLayout(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        return GetLayoutByIndex(GetObjLayoutIndex(compiler, classHandle));
    }

    // Get the number of a layout for the specified class handle.
    unsigned GetObjLayoutNum(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        return GetObjLayoutIndex(compiler, classHandle) + FirstLayoutNum;
    }

private:
    ClassLayout* GetLayoutByIndex(unsigned index) const
    {
        assert(index < m_layoutCount);

        if (index < NumInlineLayouts)
        {
            return const_cast<ClassLayout*>(reinterpret_cast<const ClassLayout*>(&m_inlineLayouts[index * sizeof(ClassLayout)]));
        }
        else
        {
            return m_layoutLargeArray[index - NumInlineLayouts];
        }
    }

    unsigned GetLayoutIndex(ClassLayout* layout) const
    {
        assert(layout != nullptr);

        if ((reinterpret_cast<char*>(layout) >= m_inlineLayouts) && (reinterpret_cast<char*>(layout) < m_inlineLayouts + ArrLen(m_inlineLayouts)))
        {
            size_t index = layout - reinterpret_cast<const ClassLayout*>(m_inlineLayouts);
            assert(index <= UINT_MAX);
            return static_cast<unsigned>(index);
        }

        unsigned index;
        if ((layout->IsBlockLayout() && m_blkLayoutMap->Lookup(layout->GetSize(), &index)) ||
            m_objLayoutMap->Lookup(layout->GetClassHandle(), &index))
        {
            return index;
        }

        unreached();
    }

    unsigned GetBlkLayoutIndex(Compiler* compiler, unsigned blockSize)
    {
        for (unsigned i = 0; i < NumInlineLayouts && i < m_layoutCount; i++)
        {
            ClassLayout* inlineLayout = reinterpret_cast<ClassLayout*>(&m_inlineLayouts[i * sizeof(ClassLayout)]);
            if (inlineLayout->IsBlockLayout() && (inlineLayout->GetSize() == blockSize))
            {
                return i;
            }
        }

        unsigned index;
        if ((m_blkLayoutMap != nullptr) && m_blkLayoutMap->Lookup(blockSize, &index))
        {
            return index;
        }

        return AddBlkLayout(compiler, blockSize);
    }

    unsigned AddBlkLayout(Compiler* compiler, unsigned blockSize)
    {
        if (m_layoutCount < NumInlineLayouts)
        {
            ClassLayout* inlineLayout = new (&m_inlineLayouts[m_layoutCount * sizeof(ClassLayout)], jitstd::placement_t()) ClassLayout(blockSize);
            return m_layoutCount++;
        }

        ClassLayout* layout = new (compiler, CMK_ClassLayout) ClassLayout(blockSize);
        unsigned index = AddLayoutLarge(compiler, layout);
        m_blkLayoutMap->Set(layout->GetSize(), index);
        return index;
    }

    unsigned GetObjLayoutIndex(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        assert(classHandle != NO_CLASS_HANDLE);

        for (unsigned i = 0; i < NumInlineLayouts && i < m_layoutCount; i++)
        {
            ClassLayout* inlineLayout = reinterpret_cast<ClassLayout*>(&m_inlineLayouts[i * sizeof(ClassLayout)]);
            if (inlineLayout->GetClassHandle() == classHandle)
            {
                return i;
            }
        }

        unsigned index;
        if ((m_objLayoutMap != nullptr) && m_objLayoutMap->Lookup(classHandle, &index))
        {
            return index;
        }

        return AddObjLayout(compiler, classHandle);
    }

    unsigned AddObjLayout(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        bool     isValueClass = compiler->eeIsValueClass(classHandle);
        unsigned size;

        if (isValueClass)
        {
            size = compiler->info.compCompHnd->getClassSize(classHandle);
        }
        else
        {
            size = compiler->info.compCompHnd->getHeapClassSize(classHandle);
        }

        var_types type = compiler->impNormStructType(classHandle);

        INDEBUG(const char* className = compiler->eeGetClassName(classHandle);)
        INDEBUG(const char* shortClassName = compiler->eeGetShortClassName(classHandle);)

        if (m_layoutCount < NumInlineLayouts)
        {
            ClassLayout* layout = new (&m_inlineLayouts[m_layoutCount * sizeof(ClassLayout)], jitstd::placement_t())
                ClassLayout(classHandle, isValueClass, size, type DEBUGARG(className) DEBUGARG(shortClassName));
            layout->InitializeGCPtrs(compiler);
            return m_layoutCount++;
        }

        ClassLayout* layout = new (compiler, CMK_ClassLayout)
            ClassLayout(classHandle, isValueClass, size, type DEBUGARG(className) DEBUGARG(shortClassName));
        layout->InitializeGCPtrs(compiler);

        unsigned index = AddLayoutLarge(compiler, layout);
        m_objLayoutMap->Set(layout->GetClassHandle(), index);
        return index;
    }

    unsigned AddLayoutLarge(Compiler* compiler, ClassLayout* layout)
    {
        unsigned count = m_layoutCount - NumInlineLayouts;
        assert((count == 0) == (m_layoutLargeArray == nullptr));
        if (count >= m_layoutLargeCapacity)
        {
            CompAllocator alloc       = compiler->getAllocator(CMK_ClassLayout);
            if (count == 0)
            {
                m_layoutLargeCapacity = 4;
                m_layoutLargeArray = alloc.allocate<ClassLayout*>(m_layoutLargeCapacity);
                m_blkLayoutMap = new (alloc) BlkLayoutIndexMap(alloc);
                m_objLayoutMap = new (alloc) ObjLayoutIndexMap(alloc);
            }
            else
            {
                unsigned      newCapacity = count * 2;
                ClassLayout** newArray = alloc.allocate<ClassLayout*>(newCapacity);
                memcpy(newArray, m_layoutLargeArray, count * sizeof(newArray[0]));

                m_layoutLargeArray    = newArray;
                m_layoutLargeCapacity = newCapacity;
            }
        }

        m_layoutLargeArray[count] = layout;
        return m_layoutCount++;
    }
};

ClassLayoutTable* Compiler::typCreateClassLayoutTable()
{
    assert(m_classLayoutTable == nullptr);

    if (compIsForInlining())
    {
        m_classLayoutTable = impInlineInfo->InlinerCompiler->m_classLayoutTable;

        if (m_classLayoutTable == nullptr)
        {
            m_classLayoutTable = new (this, CMK_ClassLayout) ClassLayoutTable();

            impInlineInfo->InlinerCompiler->m_classLayoutTable = m_classLayoutTable;
        }
    }
    else
    {
        m_classLayoutTable = new (this, CMK_ClassLayout) ClassLayoutTable();
    }

    return m_classLayoutTable;
}

ClassLayoutTable* Compiler::typGetClassLayoutTable()
{
    if (m_classLayoutTable == nullptr)
    {
        return typCreateClassLayoutTable();
    }

    return m_classLayoutTable;
}

ClassLayout* Compiler::typGetLayoutByNum(unsigned layoutNum)
{
    return typGetClassLayoutTable()->GetLayoutByNum(layoutNum);
}

unsigned Compiler::typGetLayoutNum(ClassLayout* layout)
{
    return typGetClassLayoutTable()->GetLayoutNum(layout);
}

unsigned Compiler::typGetBlkLayoutNum(unsigned blockSize)
{
    return typGetClassLayoutTable()->GetBlkLayoutNum(this, blockSize);
}

ClassLayout* Compiler::typGetBlkLayout(unsigned blockSize)
{
    return typGetClassLayoutTable()->GetBlkLayout(this, blockSize);
}

unsigned Compiler::typGetObjLayoutNum(CORINFO_CLASS_HANDLE classHandle)
{
    return typGetClassLayoutTable()->GetObjLayoutNum(this, classHandle);
}

ClassLayout* Compiler::typGetObjLayout(CORINFO_CLASS_HANDLE classHandle)
{
    return typGetClassLayoutTable()->GetObjLayout(this, classHandle);
}

ClassLayout* ClassLayout::Create(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
{
    bool     isValueClass = compiler->eeIsValueClass(classHandle);
    unsigned size;

    if (isValueClass)
    {
        size = compiler->info.compCompHnd->getClassSize(classHandle);
    }
    else
    {
        size = compiler->info.compCompHnd->getHeapClassSize(classHandle);
    }

    var_types type = compiler->impNormStructType(classHandle);

    INDEBUG(const char* className = compiler->eeGetClassName(classHandle);)
    INDEBUG(const char* shortClassName = compiler->eeGetShortClassName(classHandle);)

    ClassLayout* layout = new (compiler, CMK_ClassLayout)
        ClassLayout(classHandle, isValueClass, size, type DEBUGARG(className) DEBUGARG(shortClassName));
    layout->InitializeGCPtrs(compiler);

    return layout;
}

void ClassLayout::InitializeGCPtrs(Compiler* compiler)
{
    assert(!m_gcPtrsInitialized);
    assert(!IsBlockLayout());

    if (m_size < TARGET_POINTER_SIZE)
    {
        assert(GetSlotCount() == 1);
        assert(m_gcPtrCount == 0);

        m_gcPtrsArray[0] = TYPE_GC_NONE;
    }
    else
    {
        BYTE* gcPtrs;

        if (GetSlotCount() > sizeof(m_gcPtrsArray))
        {
            gcPtrs = m_gcPtrs = new (compiler, CMK_ClassLayout) BYTE[GetSlotCount()];
        }
        else
        {
            gcPtrs = m_gcPtrsArray;
        }

        unsigned gcPtrCount = compiler->info.compCompHnd->getClassGClayout(m_classHandle, gcPtrs);

        assert((gcPtrCount == 0) || ((compiler->info.compCompHnd->getClassAttribs(m_classHandle) &
                                      (CORINFO_FLG_CONTAINS_GC_PTR | CORINFO_FLG_BYREF_LIKE)) != 0));

        // Since class size is unsigned there's no way we could have more than 2^30 slots
        // so it should be safe to fit this into a 30 bits bit field.
        assert(gcPtrCount < (1 << 30));

        m_gcPtrCount = gcPtrCount;
    }

    INDEBUG(m_gcPtrsInitialized = true;)
}

//------------------------------------------------------------------------
// AreCompatible: check if 2 layouts are the same for copying.
//
// Arguments:
//    layout1 - the first layout;
//    layout2 - the second layout.
//
// Return value:
//    true if compatible, false otherwise.
//
// Notes:
//    Layouts are called compatible if they are equal or if
//    they have the same size and the same GC slots.
//
// static
bool ClassLayout::AreCompatible(const ClassLayout* layout1, const ClassLayout* layout2)
{
    if ((layout1 == nullptr) || (layout2 == nullptr))
    {
        return false;
    }

    CORINFO_CLASS_HANDLE clsHnd1 = layout1->GetClassHandle();
    CORINFO_CLASS_HANDLE clsHnd2 = layout2->GetClassHandle();

    if ((clsHnd1 != NO_CLASS_HANDLE) && (clsHnd1 == clsHnd2))
    {
        return true;
    }

    if (layout1->GetSize() != layout2->GetSize())
    {
        return false;
    }

    if (layout1->HasGCPtr() != layout2->HasGCPtr())
    {
        return false;
    }

    if (layout1->GetType() != layout2->GetType())
    {
        return false;
    }

    if (!layout1->HasGCPtr() && !layout2->HasGCPtr())
    {
        return true;
    }

    assert(clsHnd1 != NO_CLASS_HANDLE);
    assert(clsHnd2 != NO_CLASS_HANDLE);
    assert(layout1->HasGCPtr() && layout2->HasGCPtr());

    if (layout1->GetGCPtrCount() != layout2->GetGCPtrCount())
    {
        return false;
    }

    assert(layout1->GetSlotCount() == layout2->GetSlotCount());
    unsigned slotsCount = layout1->GetSlotCount();

    for (unsigned i = 0; i < slotsCount; ++i)
    {
        if (layout1->GetGCPtrType(i) != layout2->GetGCPtrType(i))
        {
            return false;
        }
    }
    return true;
}
