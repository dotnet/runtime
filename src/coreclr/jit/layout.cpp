// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "layout.h"
#include "compiler.h"

// Key used in ClassLayoutTable's hash table for custom layouts.
struct CustomLayoutKey
{
    unsigned    Size;
    const BYTE* GCPtrTypes;

    CustomLayoutKey(ClassLayout* layout)
        : Size(layout->GetSize())
        , GCPtrTypes(layout->m_gcPtrCount > 0 ? layout->GetGCPtrs() : nullptr)
    {
        assert(layout->IsCustomLayout());
    }

    CustomLayoutKey(const ClassLayoutBuilder& builder)
        : Size(builder.m_size)
        , GCPtrTypes(builder.m_gcPtrCount > 0 ? builder.m_gcPtrs : nullptr)
    {
    }

    static bool Equals(const CustomLayoutKey& l, const CustomLayoutKey& r)
    {
        if (l.Size != r.Size)
        {
            return false;
        }

        if ((l.GCPtrTypes == nullptr) != (r.GCPtrTypes == nullptr))
        {
            return false;
        }

        if ((l.GCPtrTypes != nullptr) && (memcmp(l.GCPtrTypes, r.GCPtrTypes, l.Size / TARGET_POINTER_SIZE) != 0))
        {
            return false;
        }

        return true;
    }

    static unsigned GetHashCode(const CustomLayoutKey& key)
    {
        unsigned hash = key.Size;
        if (key.GCPtrTypes != nullptr)
        {
            hash ^= 0xc4cfbb2a + (hash << 19) + (hash >> 13);
            for (unsigned i = 0; i < key.Size / TARGET_POINTER_SIZE; i++)
            {
                hash ^= key.GCPtrTypes[i] + 0x9e3779b9 + (hash << 19) + (hash >> 13);
            }
        }
        else
        {
            hash ^= 0x324ba6da + (hash << 19) + (hash >> 13);
        }

        return hash;
    }
};

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
    static constexpr unsigned ZeroSizedBlockLayoutNum = TYP_UNKNOWN + 1;
    static constexpr unsigned FirstLayoutNum          = TYP_UNKNOWN + 2;

    typedef JitHashTable<CustomLayoutKey, CustomLayoutKey, unsigned>                            CustomLayoutIndexMap;
    typedef JitHashTable<CORINFO_CLASS_HANDLE, JitPtrKeyFuncs<CORINFO_CLASS_STRUCT_>, unsigned> ObjLayoutIndexMap;

    union
    {
        // Up to 3 layouts can be stored "inline" and finding a layout by handle/size can be done using linear search.
        // Most methods need no more than 2 layouts.
        ClassLayout* m_layoutArray[3];
        // Otherwise a dynamic array is allocated and hashtables are used to map from handle/size to layout array index.
        struct
        {
            ClassLayout**         m_layoutLargeArray;
            CustomLayoutIndexMap* m_customLayoutMap;
            ObjLayoutIndexMap*    m_objLayoutMap;
        };
    };
    // The number of layout objects stored in this table.
    unsigned m_layoutCount = 0;
    // The capacity of m_layoutLargeArray (when more than 3 layouts are stored).
    unsigned m_layoutLargeCapacity = 0;
    // We furthermore fast-path the 0-sized block layout which is used for
    // block locals that may grow (e.g. the outgoing arg area in every non-x86
    // compilation).
    ClassLayout m_zeroSizedBlockLayout;

public:
    ClassLayoutTable()
        : m_zeroSizedBlockLayout(0)
    {
    }

    // Get a number that uniquely identifies the specified layout.
    unsigned GetLayoutNum(ClassLayout* layout) const
    {
        if (layout == &m_zeroSizedBlockLayout)
        {
            return ZeroSizedBlockLayoutNum;
        }

        return GetLayoutIndex(layout) + FirstLayoutNum;
    }

    // Get the layout that corresponds to the specified identifier number.
    ClassLayout* GetLayoutByNum(unsigned num) const
    {
        if (num == ZeroSizedBlockLayoutNum)
        {
            // Fine to cast away const as ClassLayout is immutable
            return const_cast<ClassLayout*>(&m_zeroSizedBlockLayout);
        }

        assert(num >= FirstLayoutNum);
        return GetLayoutByIndex(num - FirstLayoutNum);
    }

    // Get the layout having the specified size but no class handle.
    ClassLayout* GetCustomLayout(Compiler* compiler, const ClassLayoutBuilder& builder)
    {
        if (builder.m_size == 0)
        {
            return &m_zeroSizedBlockLayout;
        }

        return GetLayoutByIndex(GetCustomLayoutIndex(compiler, builder));
    }

    // Get a number that uniquely identifies a layout having the specified size but no class handle.
    unsigned GetCustomLayoutNum(Compiler* compiler, const ClassLayoutBuilder& builder)
    {
        if (builder.m_size == 0)
        {
            return ZeroSizedBlockLayoutNum;
        }

        return GetCustomLayoutIndex(compiler, builder) + FirstLayoutNum;
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
        assert(layout != &m_zeroSizedBlockLayout);

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
            if (layout->IsCustomLayout() ? m_customLayoutMap->Lookup(CustomLayoutKey(layout), &index)
                                         : m_objLayoutMap->Lookup(layout->GetClassHandle(), &index))
            {
                return index;
            }
        }

        unreached();
    }

    unsigned GetCustomLayoutIndex(Compiler* compiler, const ClassLayoutBuilder& builder)
    {
        // The 0-sized layout has its own fast path.
        assert(builder.m_size != 0);

        CustomLayoutKey key(builder);

        if (HasSmallCapacity())
        {
            for (unsigned i = 0; i < m_layoutCount; i++)
            {
                if (m_layoutArray[i]->IsCustomLayout() &&
                    CustomLayoutKey::Equals(key, CustomLayoutKey(m_layoutArray[i])))
                {
                    return i;
                }
            }
        }
        else
        {
            unsigned index;
            if (m_customLayoutMap->Lookup(key, &index))
            {
                return index;
            }
        }

        return AddCustomLayout(compiler, ClassLayout::Create(compiler, builder));
    }

    unsigned AddCustomLayout(Compiler* compiler, ClassLayout* layout)
    {
        if (m_layoutCount < ArrLen(m_layoutArray))
        {
            m_layoutArray[m_layoutCount] = layout;
            return m_layoutCount++;
        }

        unsigned index = AddLayoutLarge(compiler, layout);
        m_customLayoutMap->Set(CustomLayoutKey(layout), index);
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

        return AddObjLayout(compiler, ClassLayout::Create(compiler, classHandle));
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
                CustomLayoutIndexMap* customLayoutMap = new (alloc) CustomLayoutIndexMap(alloc);
                ObjLayoutIndexMap*    objLayoutMap    = new (alloc) ObjLayoutIndexMap(alloc);

                for (unsigned i = 0; i < m_layoutCount; i++)
                {
                    ClassLayout* l = m_layoutArray[i];
                    newArray[i]    = l;

                    if (l->IsCustomLayout())
                    {
                        customLayoutMap->Set(CustomLayoutKey(l), i);
                    }
                    else
                    {
                        objLayoutMap->Set(l->GetClassHandle(), i);
                    }
                }

                m_customLayoutMap = customLayoutMap;
                m_objLayoutMap    = objLayoutMap;
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

unsigned Compiler::typGetObjLayoutNum(CORINFO_CLASS_HANDLE classHandle)
{
    return typGetClassLayoutTable()->GetObjLayoutNum(this, classHandle);
}

ClassLayout* Compiler::typGetObjLayout(CORINFO_CLASS_HANDLE classHandle)
{
    return typGetClassLayoutTable()->GetObjLayout(this, classHandle);
}

unsigned Compiler::typGetCustomLayoutNum(const ClassLayoutBuilder& builder)
{
    return typGetClassLayoutTable()->GetCustomLayoutNum(this, builder);
}

ClassLayout* Compiler::typGetCustomLayout(const ClassLayoutBuilder& builder)
{
    return typGetClassLayoutTable()->GetCustomLayout(this, builder);
}

unsigned Compiler::typGetBlkLayoutNum(unsigned blockSize)
{
    return typGetCustomLayoutNum(ClassLayoutBuilder(this, blockSize));
}

ClassLayout* Compiler::typGetBlkLayout(unsigned blockSize)
{
    return typGetCustomLayout(ClassLayoutBuilder(this, blockSize));
}

unsigned Compiler::typGetArrayLayoutNum(CORINFO_CLASS_HANDLE classHandle, unsigned length)
{
    ClassLayoutBuilder b = ClassLayoutBuilder::BuildArray(this, classHandle, length);
    return typGetCustomLayoutNum(b);
}

ClassLayout* Compiler::typGetArrayLayout(CORINFO_CLASS_HANDLE classHandle, unsigned length)
{
    ClassLayoutBuilder b = ClassLayoutBuilder::BuildArray(this, classHandle, length);
    return typGetCustomLayout(b);
}

//------------------------------------------------------------------------
// Create: Create a ClassLayout from an EE side class handle.
//
// Parameters:
//   compiler    - The Compiler object
//   classHandle - The class handle
//
// Return value:
//   New layout representing an EE side class.
//
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

    if (layout->m_size < TARGET_POINTER_SIZE)
    {
        assert(layout->GetSlotCount() == 1);
        assert(layout->m_gcPtrCount == 0);

        layout->m_gcPtrsArray[0] = TYPE_GC_NONE;
    }
    else
    {
        BYTE* gcPtrs;
        if (layout->GetSlotCount() <= sizeof(m_gcPtrsArray))
        {
            gcPtrs = layout->m_gcPtrsArray;
        }
        else
        {
            layout->m_gcPtrs = gcPtrs = new (compiler, CMK_ClassLayout) BYTE[layout->GetSlotCount()];
        }

        unsigned gcPtrCount = compiler->info.compCompHnd->getClassGClayout(classHandle, gcPtrs);

        assert((gcPtrCount == 0) || ((compiler->info.compCompHnd->getClassAttribs(classHandle) &
                                      (CORINFO_FLG_CONTAINS_GC_PTR | CORINFO_FLG_BYREF_LIKE)) != 0));

        // Since class size is unsigned there's no way we could have more than 2^30 slots
        // so it should be safe to fit this into a 30 bits bit field.
        assert(gcPtrCount < (1 << 30));

        layout->m_gcPtrCount = gcPtrCount;
    }

    return layout;
}

//------------------------------------------------------------------------
// Create: Create a ClassLayout from a ClassLayoutBuilder.
//
// Parameters:
//   compiler - The Compiler object
//   builder  - Builder representing the layout
//
// Return value:
//   New layout representing a custom (JIT internal) class layout.
//
ClassLayout* ClassLayout::Create(Compiler* compiler, const ClassLayoutBuilder& builder)
{
    ClassLayout* newLayout  = new (compiler, CMK_ClassLayout) ClassLayout(builder.m_size);
    newLayout->m_gcPtrCount = builder.m_gcPtrCount;
    newLayout->m_nonPadding = builder.m_nonPadding;

#ifdef DEBUG
    newLayout->m_name      = builder.m_name;
    newLayout->m_shortName = builder.m_shortName;
#endif

    if (newLayout->GetSlotCount() <= sizeof(newLayout->m_gcPtrsArray))
    {
        if (builder.m_gcPtrCount > 0)
        {
            memcpy(newLayout->m_gcPtrsArray, builder.m_gcPtrs, newLayout->GetSlotCount());
        }
        else
        {
            memset(newLayout->m_gcPtrsArray, TYPE_GC_NONE, newLayout->GetSlotCount());
        }
    }
    else if (builder.m_gcPtrCount > 0)
    {
        newLayout->m_gcPtrs = builder.m_gcPtrs;
    }
    else
    {
        newLayout->m_gcPtrs = new (compiler, CMK_ClassLayout) BYTE[newLayout->GetSlotCount()]{};
    }

    return newLayout;
}

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
// GetNonPadding:
//   Get a SegmentList containing segments for all the non-padding in the
//   layout. This is generally the areas of the layout covered by fields, but
//   in some cases may also include other parts.
//
// Parameters:
//   comp - Compiler instance
//
// Return value:
//   A segment list.
//
const SegmentList& ClassLayout::GetNonPadding(Compiler* comp)
{
    if (m_nonPadding != nullptr)
    {
        return *m_nonPadding;
    }

    m_nonPadding = new (comp, CMK_ClassLayout) SegmentList(comp->getAllocator(CMK_ClassLayout));
    if (IsCustomLayout())
    {
        if (m_size > 0)
        {
            m_nonPadding->Add(SegmentList::Segment(0, GetSize()));
        }

        return *m_nonPadding;
    }

    CORINFO_TYPE_LAYOUT_NODE nodes[256];
    size_t                   numNodes = ArrLen(nodes);
    GetTypeLayoutResult      result   = comp->info.compCompHnd->getTypeLayout(GetClassHandle(), nodes, &numNodes);

    if (result != GetTypeLayoutResult::Success)
    {
        m_nonPadding->Add(SegmentList::Segment(0, GetSize()));
    }
    else
    {
        for (size_t i = 0; i < numNodes; i++)
        {
            const CORINFO_TYPE_LAYOUT_NODE& node = nodes[i];
            if ((node.type != CORINFO_TYPE_VALUECLASS) || (node.simdTypeHnd != NO_CLASS_HANDLE) ||
                node.hasSignificantPadding)
            {
                m_nonPadding->Add(SegmentList::Segment(node.offset, node.offset + node.size));
            }
        }
    }

    return *m_nonPadding;
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

    if ((clsHnd1 != NO_CLASS_HANDLE) == (clsHnd2 != NO_CLASS_HANDLE))
    {
        // Either both are class-based layout or both are custom layouts.
        // Custom layouts only match each other if they are the same pointer.
        if (clsHnd1 == NO_CLASS_HANDLE)
        {
            return layout1 == layout2;
        }

        // For class-based layouts they are definitely compatible for the same
        // handle
        if (clsHnd1 == clsHnd2)
        {
            return true;
        }

        // But they may still be compatible for different handles.
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

//------------------------------------------------------------------------
// ClassLayoutBuilder: Construct a new builder for a class layout of the
// specified size.
//
// Arguments:
//    compiler - Compiler instance
//    size     - Size of the layout
//
ClassLayoutBuilder::ClassLayoutBuilder(Compiler* compiler, unsigned size)
    : m_compiler(compiler)
    , m_size(size)
{
}

//------------------------------------------------------------------------
// BuildArray: Construct a builder for an array layout
//
// Arguments:
//    compiler      - Compiler instance
//    arrayHandle   - class handle for array
//    length        - array length (in elements)
//
// Note:
//    For arrays of structs we currently do not copy any struct padding,
//    with the presumption that it is unlikely we will ever promote array elements.
//
ClassLayoutBuilder ClassLayoutBuilder::BuildArray(Compiler* compiler, CORINFO_CLASS_HANDLE arrayHandle, unsigned length)
{
    assert(length <= CORINFO_Array_MaxLength);
    assert(arrayHandle != NO_CLASS_HANDLE);

    CORINFO_CLASS_HANDLE elemClsHnd = NO_CLASS_HANDLE;
    CorInfoType          corType    = compiler->info.compCompHnd->getChildType(arrayHandle, &elemClsHnd);
    var_types            type       = JITtype2varType(corType);

    ClassLayout* elementLayout = nullptr;
    unsigned     elementSize   = 0;

    if (type == TYP_STRUCT)
    {
        elementLayout = compiler->typGetObjLayout(elemClsHnd);
        elementSize   = elementLayout->GetSize();
    }
    else
    {
        elementSize = genTypeSize(type);
    }

    ClrSafeInt<unsigned> totalSize(elementSize);
    totalSize *= static_cast<unsigned>(length);
    totalSize.AlignUp(TARGET_POINTER_SIZE);
    totalSize += static_cast<unsigned>(OFFSETOF__CORINFO_Array__data);
    assert(!totalSize.IsOverflow());

    ClassLayoutBuilder builder(compiler, totalSize.Value());

    if (elementLayout != nullptr)
    {
        if (elementLayout->HasGCPtr())
        {
            unsigned offset = OFFSETOF__CORINFO_Array__data;
            for (unsigned i = 0; i < length; i++)
            {
                builder.CopyInfoFrom(offset, elementLayout, /* copy padding */ false);
                offset += elementSize;
            }
        }
    }
    else if (varTypeIsGC(type))
    {
        unsigned offset = OFFSETOF__CORINFO_Array__data;
        for (unsigned i = 0; i < length; i++)
        {
            assert((offset % TARGET_POINTER_SIZE) == 0);
            unsigned const slot = offset / TARGET_POINTER_SIZE;
            builder.SetGCPtrType(slot, type);
            offset += elementSize;
        }
    }

#ifdef DEBUG
    const char* className      = compiler->eeGetClassName(arrayHandle);
    const char* shortClassName = compiler->eeGetShortClassName(arrayHandle);
    builder.SetName(className, shortClassName);
#endif

    return builder;
}

//------------------------------------------------------------------------
// GetOrCreateGCPtrs: Get or create the array indicating GC pointer types.
//
// Returns:
//   The array of CorInfoGCType.
//
BYTE* ClassLayoutBuilder::GetOrCreateGCPtrs()
{
    assert(m_size % TARGET_POINTER_SIZE == 0);

    if (m_gcPtrs == nullptr)
    {
        m_gcPtrs = new (m_compiler, CMK_ClassLayout) BYTE[m_size / TARGET_POINTER_SIZE]{};
    }

    return m_gcPtrs;
}

//------------------------------------------------------------------------
// SetGCPtr: Set a slot to have specified GC pointer type.
//
// Arguments:
//   slot - The GC pointer slot. The slot number corresponds to offset slot * TARGET_POINTER_SIZE.
//   type - Type of GC pointer that this slot contains.
//
// Remarks:
//   GC pointer information can only be set in layouts of size divisible by
//   TARGET_POINTER_SIZE.
//
void ClassLayoutBuilder::SetGCPtr(unsigned slot, CorInfoGCType type)
{
    BYTE* ptrs = GetOrCreateGCPtrs();

    assert(slot * TARGET_POINTER_SIZE < m_size);

    if (ptrs[slot] != TYPE_GC_NONE)
    {
        m_gcPtrCount--;
    }

    ptrs[slot] = static_cast<BYTE>(type);

    if (type != TYPE_GC_NONE)
    {
        m_gcPtrCount++;
    }
}

//------------------------------------------------------------------------
// SetGCPtrType: Set a slot to have specified type.
//
// Arguments:
//   slot - The GC pointer slot. The slot number corresponds to offset slot * TARGET_POINTER_SIZE.
//   type - Type that this slot contains. Must be TYP_REF, TYP_BYREF or TYP_I_IMPL.
//
// Remarks:
//   GC pointer information can only be set in layouts of size divisible by
//   TARGET_POINTER_SIZE.
//
void ClassLayoutBuilder::SetGCPtrType(unsigned slot, var_types type)
{
    switch (type)
    {
        case TYP_REF:
            SetGCPtr(slot, TYPE_GC_REF);
            break;
        case TYP_BYREF:
            SetGCPtr(slot, TYPE_GC_BYREF);
            break;
        case TYP_I_IMPL:
            SetGCPtr(slot, TYPE_GC_NONE);
            break;
        default:
            assert(!"Invalid type passed to ClassLayoutBuilder::SetGCPtrType");
            break;
    }
}

//------------------------------------------------------------------------
// CopyInfoFrom: Copy GC pointers and padding information from another layout.
//
// Arguments:
//   offset      - Offset in this builder to start copy information into.
//   layout      - Layout to get information from.
//   copyPadding - Whether padding info should also be copied from the layout.
//
void ClassLayoutBuilder::CopyInfoFrom(unsigned offset, ClassLayout* layout, bool copyPadding)
{
    assert(offset + layout->GetSize() <= m_size);

    if (layout->GetGCPtrCount() > 0)
    {
        assert(offset % TARGET_POINTER_SIZE == 0);
        unsigned startSlot = offset / TARGET_POINTER_SIZE;
        for (unsigned slot = 0; slot < layout->GetSlotCount(); slot++)
        {
            SetGCPtr(startSlot + slot, layout->GetGCPtr(slot));
        }
    }

    if (copyPadding)
    {
        AddPadding(SegmentList::Segment(offset, offset + layout->GetSize()));

        for (const SegmentList::Segment& nonPadding : layout->GetNonPadding(m_compiler))
        {
            RemovePadding(SegmentList::Segment(offset + nonPadding.Start, offset + nonPadding.End));
        }
    }
}

//------------------------------------------------------------------------
// GetOrCreateNonPadding: Get the non padding segment list, or create it if it
// does not exist.
//
// Remarks:
//   The ClassLayoutBuilder starts out with the entire layout being considered
//   to NOT be padding.
//
SegmentList* ClassLayoutBuilder::GetOrCreateNonPadding()
{
    if (m_nonPadding == nullptr)
    {
        m_nonPadding = new (m_compiler, CMK_ClassLayout) SegmentList(m_compiler->getAllocator(CMK_ClassLayout));
        m_nonPadding->Add(SegmentList::Segment(0, m_size));
    }

    return m_nonPadding;
}

//------------------------------------------------------------------------
// AddPadding: Mark that part of the layout has padding.
//
// Arguments:
//   padding - The segment to mark as being padding.
//
// Remarks:
//   The ClassLayoutBuilder starts out with the entire layout being considered
//   to NOT be padding.
//
void ClassLayoutBuilder::AddPadding(const SegmentList::Segment& padding)
{
    assert((padding.Start <= padding.End) && (padding.End <= m_size));
    GetOrCreateNonPadding()->Subtract(padding);
}

//------------------------------------------------------------------------
// RemovePadding: Mark that part of the layout does not have padding.
//
// Arguments:
//   nonPadding - The segment to mark as having significant data.
//
// Remarks:
//   The ClassLayoutBuilder starts out with the entire layout being considered
//   to NOT be padding.
//
void ClassLayoutBuilder::RemovePadding(const SegmentList::Segment& nonPadding)
{
    assert((nonPadding.Start <= nonPadding.End) && (nonPadding.End <= m_size));
    GetOrCreateNonPadding()->Add(nonPadding);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// SetName: Set the long and short name of the layout.
//
// Arguments:
//   name      - The long name
//   shortName - The short name
//
void ClassLayoutBuilder::SetName(const char* name, const char* shortName)
{
    m_name      = name;
    m_shortName = shortName;
}
#endif
