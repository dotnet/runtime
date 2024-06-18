// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial struct Metadata_1
{
    [Flags]
    internal enum WFLAGS_LOW : uint
    {
        // We are overloading the low 2 bytes of m_dwFlags to be a component size for Strings
        // and Arrays and some set of flags which we can be assured are of a specified state
        // for Strings / Arrays, currently these will be a bunch of generics flags which don't
        // apply to Strings / Arrays.

        UNUSED_ComponentSize_1 = 0x00000001,
        // GC depends on this bit
        HasCriticalFinalizer = 0x00000002, // finalizer must be run on Appdomain Unload


        GenericsMask = 0x00000030,
        GenericsMask_NonGeneric = 0x00000000,   // no instantiation
        GenericsMask_GenericInst = 0x00000010,   // regular instantiation, e.g. List<String>
        GenericsMask_SharedInst = 0x00000020,   // shared instantiation, e.g. List<__Canon> or List<MyValueType<__Canon>>
        GenericsMask_TypicalInst = 0x00000030,   // the type instantiated at its formal parameters, e.g. List<T>

        HasVariance = 0x00000100,   // This is an instantiated type some of whose type parameters are co- or contra-variant

        HasDefaultCtor = 0x00000200,
        HasPreciseInitCctors = 0x00000400,   // Do we need to run class constructors at allocation time? (Not perf important, could be moved to EEClass

        // if defined(FEATURE_HFA)
        IsHFA = 0x00000800,   // This type is an HFA (Homogeneous Floating-point Aggregate)

        // if defined(UNIX_AMD64_ABI)
        IsRegStructPassed = 0x00000800,   // This type is a System V register passed struct.

        IsByRefLike = 0x00001000,

        HasBoxedRegularStatics = 0x00002000,
        HasBoxedThreadStatics = 0x00004000,

        // In a perfect world we would fill these flags using other flags that we already have
        // which have a constant value for something which has a component size.
        UNUSED_ComponentSize_7 = 0x00008000,

        // IMPORTANT! IMPORTANT! IMPORTANT!
        //
        // As you change the flags in WFLAGS_LOW_ENUM you also need to change this
        // to be up to date to reflect the default values of those flags for the
        // case where this MethodTable is for a String or Array
        StringArrayValues = // SET_FALSE(enum_flag_HasCriticalFinalizer) |
                            // SET_FALSE(enum_flag_HasBoxedRegularStatics) |
                            // SET_FALSE(enum_flag_HasBoxedThreadStatics) |
                                      GenericsMask_NonGeneric |
                                      // SET_FALSE(enum_flag_HasVariance) |
                                      // SET_FALSE(enum_flag_HasDefaultCtor) |
                                      // SET_FALSE(enum_flag_HasPreciseInitCctors),
                                      0,
    }

    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        // DO NOT use flags that have bits set in the low 2 bytes.
        // These flags are DWORD sized so that our atomic masking
        // operations can operate on the entire 4-byte aligned DWORD
        // instead of the logical non-aligned WORD of flags.  The
        // low WORD of flags is reserved for the component size.

        // The following bits describe mutually exclusive locations of the type
        // in the type hierarchy.
        Category_Mask = 0x000F0000,

        Category_Class = 0x00000000,
        Category_Unused_1 = 0x00010000,
        Category_Unused_2 = 0x00020000,
        Category_Unused_3 = 0x00030000,

        Category_ValueType = 0x00040000,
        Category_ValueType_Mask = 0x000C0000,
        Category_Nullable = 0x00050000, // sub-category of ValueType
        Category_PrimitiveValueType = 0x00060000, // sub-category of ValueType, Enum or primitive value type
        Category_TruePrimitive = 0x00070000, // sub-category of ValueType, Primitive (ELEMENT_TYPE_I, etc.)

        Category_Array = 0x00080000,
        Category_Array_Mask = 0x000C0000,
        // enum_flag_Category_IfArrayThenUnused                 = 0x00010000, // sub-category of Array
        Category_IfArrayThenSzArray = 0x00020000, // sub-category of Array

        Category_Interface = 0x000C0000,
        Category_Unused_4 = 0x000D0000,
        Category_Unused_5 = 0x000E0000,
        Category_Unused_6 = 0x000F0000,

        Category_ElementTypeMask = 0x000E0000, // bits that matter for element type mask

        HasFinalizer = 0x00100000, // instances require finalization. GC depends on this bit.
        Collectible = 0x00200000, // GC depends on this bit.
        ICastable = 0x00400000, // class implements ICastable interface

        // ifdef FEATURE_64BIT_ALIGNMENT
        eRequiresAlign8 = 0x00800000, // Type requires 8-byte alignment (only set on platforms that require this and don't get it implicitly)

        ContainsPointers = 0x01000000, // Contains object references
        HasTypeEquivalence = 0x02000000, // can be equivalent to another type
        IsTrackedReferenceWithFinalizer = 0x04000000,
        // unused                             = 0x08000000,

        IDynamicInterfaceCastable = 0x10000000, // class implements IDynamicInterfaceCastable interface
        ContainsGenericVariables = 0x20000000, // we cache this flag to help detect these efficiently and
                                               // to detect this condition when restoring
        ComObject = 0x40000000, // class is a com object
        HasComponentSize = 0x80000000, // This is set if component size is used for flags.

        // Types that require non-trivial interface cast have this bit set in the category
        NonTrivialInterfaceCast = Category_Array
                                             | ComObject
                                             | ICastable
                                             | IDynamicInterfaceCastable
                                             | Category_ValueType
    }

    [Flags]
    internal enum WFLAGS2_ENUM : uint
    {
        HasPerInstInfo = 0x0001,
        DynamicStatics = 0x0002,
        HasDispatchMapSlot = 0x0004,

        // wflags2_unused_2          = 0x0008,
        HasModuleDependencies = 0x0010,
        IsIntrinsicType = 0x0020,
        HasCctor = 0x0040,
        HasVirtualStaticMethods = 0x0080,

        TokenMask = 0xFFFFFF00,
    }

    [Flags]
    internal enum MethodTableAuxiliaryDataFlags : uint
    {
        Initialized = 0x0001,
        HasCheckedCanCompareBitsOrUseFastGetHashCode = 0x0002,  // Whether we have checked the overridden Equals or GetHashCode
        CanCompareBitsOrUseFastGetHashCode = 0x0004,     // Is any field type or sub field type overridden Equals or GetHashCode

        HasApproxParent = 0x0010,
        // enum_unused                      = 0x0020,
        IsNotFullyLoaded = 0x0040,
        DependenciesLoaded = 0x0080,     // class and all dependencies loaded up to CLASS_LOADED_BUT_NOT_VERIFIED

        IsInitError = 0x0100,
        IsStaticDataAllocated = 0x0200,
        // unum_unused                      = 0x0400,
        IsTlsIndexAllocated = 0x0800,
        MayHaveOpenInterfaceInInterfaceMap = 0x1000,
        // enum_unused                      = 0x2000,

        // ifdef _DEBUG
        DEBUG_ParentMethodTablePointerValid = 0x4000,
        DEBUG_HasInjectedInterfaceDuplicates = 0x8000,
    }

    internal struct MethodTableFlags
    {
        public uint DwFlags { get; init; }
        public uint DwFlags2 { get; init; }
        public uint BaseSize { get; init; }

        private WFLAGS_HIGH FlagsHigh => (WFLAGS_HIGH)DwFlags;
        private WFLAGS_LOW FlagsLow => (WFLAGS_LOW)DwFlags;
        public int GetTypeDefRid() => (int)(DwFlags2 >> Constants.MethodTableDwFlags2TypeDefRidShift);

        public WFLAGS_LOW GetFlag(WFLAGS_LOW mask) => throw new NotImplementedException("TODO");
        public WFLAGS_HIGH GetFlag(WFLAGS_HIGH mask) => FlagsHigh & mask;

        public WFLAGS2_ENUM GetFlag(WFLAGS2_ENUM mask) => (WFLAGS2_ENUM)DwFlags2 & mask;
        public bool IsInterface => GetFlag(WFLAGS_HIGH.Category_Mask) == WFLAGS_HIGH.Category_Interface;
        public bool IsString => HasComponentSize && !IsArray && RawGetComponentSize() == 2;

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH.HasComponentSize) != 0;

        public bool IsArray => GetFlag(WFLAGS_HIGH.Category_Array_Mask) == WFLAGS_HIGH.Category_Array;

        public bool IsStringOrArray => HasComponentSize;
        public ushort RawGetComponentSize() => (ushort)(DwFlags >> 16);

        public bool TestFlagWithMask(WFLAGS_LOW mask, WFLAGS_LOW flag)
        {
            if (IsStringOrArray)
            {
                return (WFLAGS_LOW.StringArrayValues & mask) == flag;
            }
            else
            {
                return (FlagsLow & mask) == flag;
            }
        }

        public bool TestFlagWithMask(WFLAGS2_ENUM mask, WFLAGS2_ENUM flag)
        {
            return ((WFLAGS2_ENUM)DwFlags2 & mask) == flag;
        }

        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_NonGeneric);

        public bool ContainsPointers => GetFlag(WFLAGS_HIGH.ContainsPointers) != 0;
    }
}
