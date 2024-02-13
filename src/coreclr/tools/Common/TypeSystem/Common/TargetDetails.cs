// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Specifies the target ABI.
    /// </summary>
    public enum TargetOS
    {
        Unknown,
        Windows,
        Linux,
        OSX,
        MacCatalyst,
        iOS,
        iOSSimulator,
        tvOS,
        tvOSSimulator,
        FreeBSD,
        NetBSD,
        SunOS,
        WebAssembly
    }

    public enum TargetAbi
    {
        Unknown,
        /// <summary>
        /// Cross-platform console model
        /// </summary>
        NativeAot,
        /// <summary>
        /// model for armel execution model
        /// </summary>
        NativeAotArmel,
        /// <summary>
        /// Jit runtime ABI
        /// </summary>
        Jit,
        /// <summary>
        /// Cross-platform portable C++ codegen
        /// </summary>
        CppCodegen,
    }

    /// <summary>
    /// Represents various details about the compilation target that affect
    /// layout, padding, allocations, or ABI.
    /// </summary>
    public partial class TargetDetails
    {
        /// <summary>
        /// Gets the target CPU architecture.
        /// </summary>
        public TargetArchitecture Architecture
        {
            get;
        }

        /// <summary>
        /// Gets the target ABI.
        /// </summary>
        public TargetOS OperatingSystem
        {
            get;
        }

        public TargetAbi Abi
        {
            get;
        }

        /// <summary>
        /// Gets the size of a pointer for the target of the compilation.
        /// </summary>
        public int PointerSize
        {
            get
            {
                switch (Architecture)
                {
                    case TargetArchitecture.ARM64:
                    case TargetArchitecture.X64:
                    case TargetArchitecture.LoongArch64:
                    case TargetArchitecture.RiscV64:
                        return 8;
                    case TargetArchitecture.ARM:
                    case TargetArchitecture.X86:
                    case TargetArchitecture.Wasm32:
                        return 4;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public bool SupportsRelativePointers
        {
            get
            {
                return (Abi != TargetAbi.CppCodegen) && (Architecture != TargetArchitecture.Wasm32);
            }
        }

        /// <summary>
        /// Gets the maximum alignment to which something can be aligned
        /// </summary>
        public int MaximumAlignment
        {
            get
            {
                if (Architecture == TargetArchitecture.ARM)
                {
                    // Corresponds to alignment required for __m128 (there's no __m256/__m512)
                    return 8;
                }
                else if (Architecture == TargetArchitecture.ARM64)
                {
                    // Corresponds to alignmet required for __m128 (there's no __m256/__m512)
                    return 16;
                }
                else if (Architecture == TargetArchitecture.LoongArch64)
                {
                    return 16;
                }
                else if (Architecture == TargetArchitecture.RiscV64)
                {
                    return 16;
                }

                // 512-bit vector is the type with the highest alignment we support
                return 64;
            }
        }

        public LayoutInt LayoutPointerSize => new LayoutInt(PointerSize);

        /// <summary>
        /// Gets the default field packing size.
        /// </summary>
        public int DefaultPackingSize
        {
            get
            {
                // We use default packing size of 64 irrespective of the platform.
                return 64;
            }
        }

        /// <summary>
        /// Gets the minimum required alignment for methods whose address is visible
        /// to managed code.
        /// </summary>
        public int MinimumFunctionAlignment
        {
            get
            {
                // We use a minimum alignment of 4 irrespective of the platform.
                // This is to prevent confusing the method address with a fat function pointer.
                return 4;
            }
        }

        /// <summary>
        /// Gets the alignment that is optimal for this platform.
        /// </summary>
        public int OptimumFunctionAlignment
        {
            get
            {
                // Matches the choice in the C++ compiler.
                // We want a number that is optimized for micro-op caches in the processor.
                return 16;
            }
        }

        public int MinimumCodeAlignment
        {
            get
            {
                switch (Architecture)
                {
                    case TargetArchitecture.ARM:
                        return 2;
                    case TargetArchitecture.ARM64:
                    case TargetArchitecture.LoongArch64:
                    case TargetArchitecture.RiscV64:
                        return 4;
                    default:
                        return 1;
                }
            }
        }

        public TargetDetails(TargetArchitecture architecture, TargetOS targetOS, TargetAbi abi)
        {
            Architecture = architecture;
            OperatingSystem = targetOS;
            Abi = abi;
        }

        /// <summary>
        /// Gets the dyadic logarithm of the maximum size of a primitive type
        /// </summary>
        public static int MaximumLog2PrimitiveSize
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// Gets the maximum size of a primitive type
        /// </summary>
        public static int MaximumPrimitiveSize
        {
            get
            {
                return 1 << MaximumLog2PrimitiveSize;
            }
        }

        /// <summary>
        /// Retrieves the size of a well known type.
        /// </summary>
        public LayoutInt GetWellKnownTypeSize(DefType type)
        {
            switch (type.Category)
            {
                case TypeFlags.Void:
                    return new LayoutInt(PointerSize);
                case TypeFlags.Boolean:
                    return new LayoutInt(1);
                case TypeFlags.Char:
                    return new LayoutInt(2);
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                    return new LayoutInt(1);
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                    return new LayoutInt(2);
                case TypeFlags.UInt32:
                case TypeFlags.Int32:
                    return new LayoutInt(4);
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                    return new LayoutInt(8);
                case TypeFlags.Single:
                    return new LayoutInt(4);
                case TypeFlags.Double:
                    return new LayoutInt(8);
                case TypeFlags.UIntPtr:
                case TypeFlags.IntPtr:
                    return new LayoutInt(PointerSize);
            }

            // Add new well known types if necessary

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Retrieves the alignment required by a well known type.
        /// </summary>
        public LayoutInt GetWellKnownTypeAlignment(DefType type)
        {
            // Size == Alignment for all platforms.
            return GetWellKnownTypeSize(type);
        }

        /// <summary>
        /// Given an alignment of the fields of a type, determine the alignment that is necessary for allocating the object on the GC heap
        /// </summary>
        /// <returns></returns>
        public LayoutInt GetObjectAlignment(LayoutInt fieldAlignment)
        {
            switch (Architecture)
            {
                case TargetArchitecture.ARM:
                case TargetArchitecture.Wasm32:
                    // ARM & Wasm32 support two alignments for objects on the GC heap (4 byte and 8 byte)
                    if (fieldAlignment.IsIndeterminate)
                        return LayoutInt.Indeterminate;

                    if (fieldAlignment.AsInt <= 4)
                        return new LayoutInt(4);
                    else
                        return new LayoutInt(8);
                case TargetArchitecture.X64:
                case TargetArchitecture.ARM64:
                case TargetArchitecture.LoongArch64:
                case TargetArchitecture.RiscV64:
                    return new LayoutInt(8);
                case TargetArchitecture.X86:
                    return new LayoutInt(4);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns True if compiling for Windows
        /// </summary>
        public bool IsWindows
        {
            get
            {
                return OperatingSystem == TargetOS.Windows;
            }
        }

        /// <summary>
        /// Returns True if compiling for Apple family of operating systems.
        /// Currently including OSX, MacCatalyst, iOS, iOSSimulator, tvOS and tvOSSimulator
        /// </summary>
        public bool IsApplePlatform
        {
            get
            {
                return OperatingSystem == TargetOS.OSX ||
                    OperatingSystem == TargetOS.MacCatalyst ||
                    OperatingSystem == TargetOS.iOS ||
                    OperatingSystem == TargetOS.iOSSimulator ||
                    OperatingSystem == TargetOS.tvOS ||
                    OperatingSystem == TargetOS.tvOSSimulator;
            }
        }

        /// <summary>
        /// Maximum number of elements in a homogeneous aggregate type.
        /// </summary>
        public int MaxHomogeneousAggregateElementCount
        {
            get
            {
                // There is a hard limit of 4 elements on an HFA/HVA type, see
                // https://devblogs.microsoft.com/cppblog/introducing-vector-calling-convention/
                // and Procedure Call Standard for the Arm 64-bit Architecture.
                Debug.Assert(Architecture == TargetArchitecture.ARM ||
                    Architecture == TargetArchitecture.ARM64 ||
                    Architecture == TargetArchitecture.LoongArch64 ||
                    Architecture == TargetArchitecture.RiscV64 ||
                    Architecture == TargetArchitecture.X64 ||
                    Architecture == TargetArchitecture.X86);

                return 4;
            }
        }

        /// <summary>
        /// CodeDelta - encapsulate the fact that ARM requires a thumb bit
        /// </summary>
        public int CodeDelta { get => (Architecture == TargetArchitecture.ARM) ? 1 : 0; }
    }
}
