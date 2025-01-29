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

    union
    {
        // Up to 3 layouts can be stored "inline" and finding a layout by handle/size can be done using linear search.
        // Most methods need no more than 2 layouts.
        ClassLayout* m_layoutArray[3];
        // Otherwise a dynamic array is allocated and hashtables are used to map from handle/size to layout array index.
        struct
        {
            ClassLayout**      m_layoutLargeArray;
            BlkLayoutIndexMap* m_blkLayoutMap;
            ObjLayoutIndexMap* m_objLayoutMap;
        };
    };
    // The number of layout objects stored in this table.
    unsigned m_layoutCount;
    // The capacity of m_layoutLargeArray (when more than 3 layouts are stored).
    unsigned m_layoutLargeCapacity;

    // Associated layout builder
    ClassLayoutBuilder* m_layoutBuilder;

public:
    ClassLayoutTable(ClassLayoutBuilder* builder)
        : m_layoutCount(0)
        , m_layoutLargeCapacity(0)
        , m_layoutBuilder(builder)
    {
    }

    // Get a number that uniquely identifies the specified layout.
    unsigned GetLayoutNum(ClassLayout* layout) const
    {
        return GetLayoutIndex(layout) + FirstLayoutNum;
    }

    // Get the layout that corresponds to the specified identifier number.
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

    // Get a number that uniquely identifies a layout having the specified size but no class handle.
    unsigned GetBlkLayoutNum(Compiler* compiler, unsigned blockSize)
    {
        return GetBlkLayoutIndex(compiler, blockSize) + FirstLayoutNum;
    }

    // Get the layout for the specified class handle.
    ClassLayout* GetObjLayout(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        return GetLayoutByIndex(GetObjLayoutIndex(compiler, classHandle));
    }

    // Get a number that uniquely identifies a layout for the specified class handle.
    unsigned GetObjLayoutNum(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        return GetObjLayoutIndex(compiler, classHandle) + FirstLayoutNum;
    }

private:
    bool HasSmallCapacity() const
    {
        return m_layoutCount <= ArrLen(m_layoutArray);
    }

    ClassLayout* GetLayoutByIndex(unsigned index) const
    {
        assert(index < m_layoutCount);

        if (HasSmallCapacity())
        {
            return m_layoutArray[index];
        }
        else
        {
            return m_layoutLargeArray[index];
        }
    }

    unsigned GetLayoutIndex(ClassLayout* layout) const
    {
        assert(layout != nullptr);

        if (HasSmallCapacity())
        {
            for (unsigned i = 0; i < m_layoutCount; i++)
            {
                if (m_layoutArray[i] == layout)
                {
                    return i;
                }
            }
        }
        else
        {
            unsigned index = 0;
            if ((layout->IsBlockLayout() && m_blkLayoutMap->Lookup(layout->GetSize(), &index)) ||
                m_objLayoutMap->Lookup(layout->GetClassHandle(), &index))
            {
                return index;
            }
        }

        unreached();
    }

    unsigned GetBlkLayoutIndex(Compiler* compiler, unsigned blockSize)
    {
        if (HasSmallCapacity())
        {
            for (unsigned i = 0; i < m_layoutCount; i++)
            {
                if (m_layoutArray[i]->IsBlockLayout() && (m_layoutArray[i]->GetSize() == blockSize))
                {
                    return i;
                }
            }
        }
        else
        {
            unsigned index;
            if (m_blkLayoutMap->Lookup(blockSize, &index))
            {
                return index;
            }
        }

        return AddBlkLayout(compiler, CreateBlkLayout(compiler, blockSize));
    }

    ClassLayout* CreateBlkLayout(Compiler* compiler, unsigned blockSize)
    {
        return m_layoutBuilder->NewBlock(blockSize);
    }

    unsigned AddBlkLayout(Compiler* compiler, ClassLayout* layout)
    {
        if (m_layoutCount < ArrLen(m_layoutArray))
        {
            m_layoutArray[m_layoutCount] = layout;
            return m_layoutCount++;
        }

        unsigned index = AddLayoutLarge(compiler, layout);
        m_blkLayoutMap->Set(layout->GetSize(), index);
        return index;
    }

    unsigned GetObjLayoutIndex(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        assert(classHandle != NO_CLASS_HANDLE);

        if (HasSmallCapacity())
        {
            for (unsigned i = 0; i < m_layoutCount; i++)
            {
                if (m_layoutArray[i]->GetClassHandle() == classHandle)
                {
                    return i;
                }
            }
        }
        else
        {
            unsigned index;
            if (m_objLayoutMap->Lookup(classHandle, &index))
            {
                return index;
            }
        }

        return AddObjLayout(compiler, CreateObjLayout(compiler, classHandle));
    }

    ClassLayout* CreateObjLayout(Compiler* compiler, CORINFO_CLASS_HANDLE classHandle)
    {
        return m_layoutBuilder->New(classHandle);
    }

    unsigned AddObjLayout(Compiler* compiler, ClassLayout* layout)
    {
        if (m_layoutCount < ArrLen(m_layoutArray))
        {
            m_layoutArray[m_layoutCount] = layout;
            return m_layoutCount++;
        }

        unsigned index = AddLayoutLarge(compiler, layout);
        m_objLayoutMap->Set(layout->GetClassHandle(), index);
        return index;
    }

    unsigned AddLayoutLarge(Compiler* compiler, ClassLayout* layout)
    {
        if (m_layoutCount >= m_layoutLargeCapacity)
        {
            CompAllocator alloc       = compiler->getAllocator(CMK_ClassLayout);
            unsigned      newCapacity = m_layoutCount * 2;
            ClassLayout** newArray    = alloc.allocate<ClassLayout*>(newCapacity);

            if (m_layoutCount <= ArrLen(m_layoutArray))
            {
                BlkLayoutIndexMap* blkLayoutMap = new (alloc) BlkLayoutIndexMap(alloc);
                ObjLayoutIndexMap* objLayoutMap = new (alloc) ObjLayoutIndexMap(alloc);

                for (unsigned i = 0; i < m_layoutCount; i++)
                {
                    ClassLayout* l = m_layoutArray[i];
                    newArray[i]    = l;

                    if (l->IsBlockLayout())
                    {
                        blkLayoutMap->Set(l->GetSize(), i);
                    }
                    else
                    {
                        objLayoutMap->Set(l->GetClassHandle(), i);
                    }
                }

                m_blkLayoutMap = blkLayoutMap;
                m_objLayoutMap = objLayoutMap;
            }
            else
            {
                memcpy(newArray, m_layoutLargeArray, m_layoutCount * sizeof(newArray[0]));
            }

            m_layoutLargeArray    = newArray;
            m_layoutLargeCapacity = newCapacity;
        }

        m_layoutLargeArray[m_layoutCount] = layout;
        return m_layoutCount++;
    }
};

//
// --------------------- Compiler ----------------
//

ClassLayoutBuilder* Compiler::typGetClassLayoutBuilder()
{
    Compiler* const rootCompiler = impInlineRoot();

    if (rootCompiler->m_classLayoutBuilder == nullptr)
    {
        rootCompiler->m_classLayoutBuilder = new (this, CMK_ClassLayout) ClassLayoutBuilder(rootCompiler);
    }

    return rootCompiler->m_classLayoutBuilder;
}

ClassLayout* Compiler::typGetBlkLayout(unsigned blockSize)
{
    return typGetClassLayoutBuilder()->CreateBlock(blockSize);
}

ClassLayout* Compiler::typGetBoxLayout(ClassLayout* payloadLayout)
{
    return typGetClassLayoutBuilder()->CreateBox(payloadLayout);
}

ClassLayout* Compiler::typGetObjLayout(CORINFO_CLASS_HANDLE classHandle)
{
    return typGetClassLayoutBuilder()->Create(classHandle);
}

ClassLayout* Compiler::typGetArrayLayout(ClassLayout* elementLayout, unsigned length)
{
    return typGetClassLayoutBuilder()->CreateArray(elementLayout, length);
}

//
// --------------------- Class Layout Builder ----------------
//

ClassLayoutBuilder::ClassLayoutBuilder(Compiler* compiler)
    : m_compiler(compiler)
    , m_layoutTable(nullptr)
{
    assert(!m_compiler->compIsForInlining());

    m_layoutTable = new (m_compiler, CMK_ClassLayout) ClassLayoutTable(this);
}

ClassLayout* ClassLayoutBuilder::Create(CORINFO_CLASS_HANDLE classHandle)
{
    return m_layoutTable->GetObjLayout(m_compiler, classHandle);
}

ClassLayout* ClassLayoutBuilder::New(CORINFO_CLASS_HANDLE classHandle)
{
    bool     isValueClass = m_compiler->eeIsValueClass(classHandle);
    unsigned size;

    if (isValueClass)
    {
        size = m_compiler->info.compCompHnd->getClassSize(classHandle);
    }
    else
    {
        size = m_compiler->info.compCompHnd->getHeapClassSize(classHandle);
    }

    var_types type = m_compiler->impNormStructType(classHandle);

    INDEBUG(const char* className = m_compiler->eeGetClassName(classHandle);)
    INDEBUG(const char* shortClassName = m_compiler->eeGetShortClassName(classHandle);)

    ClassLayout* layout = new (m_compiler, CMK_ClassLayout)
        ClassLayout(classHandle, isValueClass, size, type DEBUGARG(className) DEBUGARG(shortClassName));

    layout->InitializeGCPtrs(m_compiler);
    layout->Finalize();

    return layout;
}

void ClassLayout::InitializeGCPtrs(Compiler* compiler)
{
    assert(!m_finalized);

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
}

ClassLayout* ClassLayoutBuilder::CreateBlock(unsigned blockSize)
{
    return m_layoutTable->GetBlkLayout(m_compiler, blockSize);
}

ClassLayout* ClassLayoutBuilder::NewBlock(unsigned blockSize)
{
    ClassLayout* layout = new (m_compiler, CMK_ClassLayout) ClassLayout(blockSize);
    layout->Finalize();
    return layout;
}

ClassLayout* ClassLayoutBuilder::CreateBox(ClassLayout* payloadLayout)
{
    return nullptr;
}

ClassLayout* ClassLayoutBuilder::CreateArray(ClassLayout* elementLayout, unsigned elementCount)
{
    return nullptr;
}

ClassLayout* ClassLayoutBuilder::CreateCustom(unsigned length)
{
    return nullptr;
}

void ClassLayoutBuilder::AddRepeatingElements(ClassLayout* customLayout,
                                              unsigned     offset,
                                              ClassLayout* elementLayout,
                                              unsigned     count)
{
}

void ClassLayoutBuilder::AddGCFields(ClassLayout* customLayout, unsigned offset, unsigned count)
{
}

void ClassLayoutBuilder::Finalize(ClassLayout* customLayout)
{
    customLayout->Finalize();
}

//
// --------------------- Class Layout ----------------
//

//------------------------------------------------------------------------
// IsStackOnly: does the layout represent a block that can never be on the heap?
//
// Parameters:
//   comp - The Compiler object
//
// Return value:
//    true if the block is stack only
//
bool ClassLayout::IsStackOnly(Compiler* comp) const
{
    // Byref-like structs are stack only
    if ((m_classHandle != NO_CLASS_HANDLE) && comp->eeIsByrefLike(m_classHandle))
    {
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// IntersectsGCPtr: check if the specified interval intersects with a GC
// pointer.
//
// Parameters:
//   offset - The start offset of the interval
//   size   - The size of the interval
//
// Return value:
//   True if it does.
//
bool ClassLayout::IntersectsGCPtr(unsigned offset, unsigned size) const
{
    if (!HasGCPtr())
    {
        return false;
    }

    unsigned startSlot = offset / TARGET_POINTER_SIZE;
    unsigned endSlot   = (offset + size - 1) / TARGET_POINTER_SIZE;
    assert((startSlot < GetSlotCount()) && (endSlot < GetSlotCount()));

    for (unsigned i = startSlot; i <= endSlot; i++)
    {
        if (IsGCPtr(i))
        {
            return true;
        }
    }

    return false;
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

    // validate elemcount

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
