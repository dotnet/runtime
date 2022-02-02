// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Fundamental runtime type representation

#pragma warning(push)
#pragma warning(disable:4200) // nonstandard extension used : zero-sized array in struct/union
//-------------------------------------------------------------------------------------------------
// Forward declarations

class MethodTable;
class TypeManager;
struct TypeManagerHandle;
class DynamicModule;
struct EETypeRef;

#if !defined(USE_PORTABLE_HELPERS)
#define SUPPORTS_WRITABLE_DATA 1
#endif

//-------------------------------------------------------------------------------------------------
// Array of these represents the interfaces implemented by a type

class EEInterfaceInfo
{
  public:
    MethodTable * GetInterfaceEEType()
    {
        return ((UIntTarget)m_pInterfaceEEType & ((UIntTarget)1)) ?
               *(MethodTable**)((UIntTarget)m_ppInterfaceEETypeViaIAT & ~((UIntTarget)1)) :
               m_pInterfaceEEType;
    }

  private:
    union
    {
        MethodTable *    m_pInterfaceEEType;         // m_uFlags == InterfaceFlagNormal
        MethodTable **   m_ppInterfaceEETypeViaIAT;  // m_uFlags == InterfaceViaIATFlag
    };
};

//-------------------------------------------------------------------------------------------------
// The subset of TypeFlags that Redhawk knows about at runtime
// This should match the TypeFlags enum in the managed type system.
enum EETypeElementType : uint8_t
{
    // Primitive
    ElementType_Unknown = 0x00,
    ElementType_Void = 0x01,
    ElementType_Boolean = 0x02,
    ElementType_Char = 0x03,
    ElementType_SByte = 0x04,
    ElementType_Byte = 0x05,
    ElementType_Int16 = 0x06,
    ElementType_UInt16 = 0x07,
    ElementType_Int32 = 0x08,
    ElementType_UInt32 = 0x09,
    ElementType_Int64 = 0x0A,
    ElementType_UInt64 = 0x0B,
    ElementType_IntPtr = 0x0C,
    ElementType_UIntPtr = 0x0D,
    ElementType_Single = 0x0E,
    ElementType_Double = 0x0F,

    ElementType_ValueType = 0x10,
    // Enum = 0x11, // EETypes store enums as their underlying type
    ElementType_Nullable = 0x12,
    // Unused 0x13,

    ElementType_Class = 0x14,
    ElementType_Interface = 0x15,

    ElementType_SystemArray = 0x16, // System.Array type

    ElementType_Array = 0x17,
    ElementType_SzArray = 0x18,
    ElementType_ByRef = 0x19,
    ElementType_Pointer = 0x1A,
};

//-------------------------------------------------------------------------------------------------
// Support for encapsulating the location of fields in the MethodTable that have variable offsets or may be
// optional.
//
// The following enumaration gives symbolic names for these fields and is used with the GetFieldPointer() and
// GetFieldOffset() APIs.
enum EETypeField
{
    ETF_InterfaceMap,
    ETF_TypeManagerIndirection,
    ETF_WritableData,
    ETF_Finalizer,
    ETF_OptionalFieldsPtr,
    ETF_SealedVirtualSlots,
    ETF_DynamicTemplateType,
    ETF_DynamicDispatchMap,
    ETF_DynamicModule,
    ETF_GenericDefinition,
    ETF_GenericComposition,
    ETF_DynamicGcStatics,
    ETF_DynamicNonGcStatics,
    ETF_DynamicThreadStaticOffset,
};

//-------------------------------------------------------------------------------------------------
// Fundamental runtime type representation
typedef DPTR(class MethodTable) PTR_EEType;
typedef DPTR(PTR_EEType) PTR_PTR_EEType;

extern "C" void PopulateDebugHeaders();

class MethodTable
{
    friend class AsmOffsets;
    friend void PopulateDebugHeaders();

private:
    struct RelatedTypeUnion
    {
        union
        {
            // Kinds.CanonicalEEType
            MethodTable*     m_pBaseType;
            MethodTable**    m_ppBaseTypeViaIAT;

            // Kinds.ClonedEEType
            MethodTable** m_pCanonicalType;
            MethodTable** m_ppCanonicalTypeViaIAT;

            // Kinds.ParameterizedEEType
            MethodTable*  m_pRelatedParameterType;
            MethodTable** m_ppRelatedParameterTypeViaIAT;
        };
    };

    uint16_t              m_usComponentSize;
    uint16_t              m_usFlags;
    uint32_t              m_uBaseSize;
    RelatedTypeUnion    m_RelatedType;
    uint16_t              m_usNumVtableSlots;
    uint16_t              m_usNumInterfaces;
    uint32_t              m_uHashCode;

    TgtPTR_Void         m_VTable[];  // make this explicit so the binder gets the right alignment

    // after the m_usNumVtableSlots vtable slots, we have m_usNumInterfaces slots of
    // EEInterfaceInfo, and after that a couple of additional pointers based on whether the type is
    // finalizable (the address of the finalizer code) or has optional fields (pointer to the compacted
    // fields).

    enum Flags
    {
        // There are four kinds of EETypes, the three of them regular types that use the full MethodTable encoding
        // plus a fourth kind used as a grab bag of unusual edge cases which are encoded in a smaller,
        // simplified version of MethodTable. See LimitedEEType definition below.
        EETypeKindMask = 0x0003,

        // This flag is set when m_pRelatedType is in a different module.  In that case, m_pRelatedType
        // actually points to a 'fake' MethodTable whose m_pRelatedType field lines up with an IAT slot in this
        // module, which then points to the desired MethodTable.  In other words, there is an extra indirection
        // through m_pRelatedType to get to the related type in the other module.
        RelatedTypeViaIATFlag   = 0x0004,

        IsDynamicTypeFlag       = 0x0008,

        // This MethodTable represents a type which requires finalization
        HasFinalizerFlag        = 0x0010,

        // This type contain gc pointers
        HasPointersFlag         = 0x0020,

        // This type is generic and one or more of it's type parameters is co- or contra-variant. This only
        // applies to interface and delegate types.
        GenericVarianceFlag     = 0x0080,

        // This type has optional fields present.
        OptionalFieldsFlag      = 0x0100,

        // Unused         = 0x0200,

        // This type is generic.
        IsGenericFlag           = 0x0400,

        // We are storing a EETypeElementType in the upper bits for unboxing enums
        ElementTypeMask      = 0xf800,
        ElementTypeShift     = 11,
    };

public:

    enum Kinds
    {
        CanonicalEEType         = 0x0000,
        ClonedEEType            = 0x0001,
        ParameterizedEEType     = 0x0002,
        GenericTypeDefEEType    = 0x0003,
    };

    uint32_t get_BaseSize()
        { return m_uBaseSize; }

    uint16_t get_ComponentSize()
        { return m_usComponentSize; }

    PTR_Code get_Slot(uint16_t slotNumber);

    PTR_PTR_Code get_SlotPtr(uint16_t slotNumber);

    Kinds get_Kind();

    bool IsCloned()
        { return get_Kind() == ClonedEEType; }

    bool IsRelatedTypeViaIAT()
        { return ((m_usFlags & (uint16_t)RelatedTypeViaIATFlag) != 0); }

    bool IsArray()
    {
        EETypeElementType elementType = GetElementType();
        return elementType == ElementType_Array || elementType == ElementType_SzArray;
    }

    bool IsSzArray()
        { return GetElementType() == ElementType_SzArray; }

    bool IsParameterizedType()
        { return (get_Kind() == ParameterizedEEType); }

    bool IsGenericTypeDefinition()
        { return (get_Kind() == GenericTypeDefEEType); }

    bool IsCanonical()
        { return get_Kind() == CanonicalEEType; }

    bool IsInterface()
        { return GetElementType() == ElementType_Interface; }

    MethodTable * get_CanonicalEEType();

    MethodTable * get_RelatedParameterType();

    // A parameterized type shape less than SZARRAY_BASE_SIZE indicates that this is not
    // an array but some other parameterized type (see: ParameterizedTypeShapeConstants)
    // For arrays, this number uniquely captures both Sz/Md array flavor and rank.
    uint32_t get_ParameterizedTypeShape() { return m_uBaseSize; }

    bool get_IsValueType()
        { return GetElementType() < ElementType_Class; }

    bool HasFinalizer()
    {
        return (m_usFlags & HasFinalizerFlag) != 0;
    }

    bool HasReferenceFields()
    {
        return (m_usFlags & HasPointersFlag) != 0;
    }

    bool HasOptionalFields()
    {
        return (m_usFlags & OptionalFieldsFlag) != 0;
    }

    bool IsEquivalentTo(MethodTable * pOtherEEType)
    {
        if (this == pOtherEEType)
            return true;

        MethodTable * pThisEEType = this;

        if (pThisEEType->IsCloned())
            pThisEEType = pThisEEType->get_CanonicalEEType();

        if (pOtherEEType->IsCloned())
            pOtherEEType = pOtherEEType->get_CanonicalEEType();

        if (pThisEEType == pOtherEEType)
            return true;

        if (pThisEEType->IsParameterizedType() && pOtherEEType->IsParameterizedType())
        {
            return pThisEEType->get_RelatedParameterType()->IsEquivalentTo(pOtherEEType->get_RelatedParameterType()) &&
                pThisEEType->get_ParameterizedTypeShape() == pOtherEEType->get_ParameterizedTypeShape();
        }

        return false;
    }

    // How many vtable slots are there?
    uint16_t GetNumVtableSlots()
        { return m_usNumVtableSlots; }

    // How many entries are in the interface map after the vtable slots?
    uint16_t GetNumInterfaces()
        { return m_usNumInterfaces; }

    // Does this class (or its base classes) implement any interfaces?
    bool HasInterfaces()
        { return GetNumInterfaces() != 0; }

    bool IsGeneric()
        { return (m_usFlags & IsGenericFlag) != 0; }

    DynamicModule* get_DynamicModule();

    TypeManagerHandle* GetTypeManagerPtr();

    // Used only by GC initialization, this initializes the MethodTable used to mark free entries in the GC heap.
    // It should be an array type with a component size of one (so the GC can easily size it as appropriate)
    // and should be marked as not containing any references. The rest of the fields don't matter: the GC does
    // not query them and the rest of the runtime will never hold a reference to free object.
    inline void InitializeAsGcFreeType();

#ifdef DACCESS_COMPILE
    bool DacVerify();
    static bool DacVerifyWorker(MethodTable* pThis);
#endif // DACCESS_COMPILE

    // Mark or determine that a type is generic and one or more of it's type parameters is co- or
    // contra-variant. This only applies to interface and delegate types.
    bool HasGenericVariance()
        { return (m_usFlags & GenericVarianceFlag) != 0; }

    EETypeElementType GetElementType()
        { return (EETypeElementType)((m_usFlags & ElementTypeMask) >> ElementTypeShift); }

    // Determine whether a type is an instantiation of Nullable<T>.
    bool IsNullable()
        { return GetElementType() == ElementType_Nullable; }

    // Determine whether a type was created by dynamic type loader
    bool IsDynamicType()
        { return (m_usFlags & IsDynamicTypeFlag) != 0; }

    uint32_t GetHashCode();

    // Helper methods that deal with MethodTable topology (size and field layout). These are useful since as we
    // optimize for pay-for-play we increasingly want to customize exactly what goes into an MethodTable on a
    // per-type basis. The rules that govern this can be both complex and volatile and we risk sprinkling
    // various layout rules through the binder and runtime that obscure the basic meaning of the code and are
    // brittle: easy to overlook when one of the rules changes.
    //
    // The following methods can in some cases have fairly complex argument lists of their own and in that way
    // they expose more of the implementation details than we'd ideally like. But regardless they still serve
    // an arguably more useful purpose: they identify all the places that rely on the MethodTable layout. As we
    // change layout rules we might have to change the arguments to the methods below but in doing so we will
    // instantly identify all the other parts of the binder and runtime that need to be updated.

    // Calculate the offset of a field of the MethodTable that has a variable offset.
    inline uint32_t GetFieldOffset(EETypeField eField);

    // Validate an MethodTable extracted from an object.
    bool Validate(bool assertOnFail = true);

public:
    // Methods expected by the GC
    uint32_t GetBaseSize() { return get_BaseSize(); }
    uint16_t GetComponentSize() { return get_ComponentSize(); }
    uint16_t RawGetComponentSize() { return get_ComponentSize(); }
    uint32_t ContainsPointers() { return HasReferenceFields(); }
    uint32_t ContainsPointersOrCollectible() { return HasReferenceFields(); }
    bool  HasComponentSize() const { return true; }
    bool HasCriticalFinalizer() { return false; }
    bool IsValueType() { return get_IsValueType(); }
    UInt32_BOOL SanityCheck() { return Validate(); }
};

#pragma warning(pop)

