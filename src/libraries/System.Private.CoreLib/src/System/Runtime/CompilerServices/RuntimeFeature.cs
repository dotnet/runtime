// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
        /// <summary>
        /// Name of the Portable PDB feature.
        /// </summary>
        public const string PortablePdb = nameof(PortablePdb);

        /// <summary>
        /// Indicates that this version of runtime supports default interface method implementations.
        /// </summary>
        public const string DefaultImplementationsOfInterfaces = nameof(DefaultImplementationsOfInterfaces);

        /// <summary>
        /// Indicates that this version of runtime supports the Unmanaged calling convention value.
        /// </summary>
        public const string UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);

        /// <summary>
        /// Indicates that this version of runtime supports covariant returns in overrides of methods declared in classes.
        /// </summary>
        public const string CovariantReturnsOfClasses = nameof(CovariantReturnsOfClasses);

        /// <summary>
        /// Indicates that this version of runtime supports virtual static members of interfaces.
        /// </summary>
        [RequiresPreviewFeatures]
        public const string VirtualStaticsInInterfaces = nameof(VirtualStaticsInInterfaces);

        /// <summary>
        /// Checks whether a certain feature is supported by the Runtime.
        /// </summary>
        public static bool IsSupported(string feature)
        {
            switch (feature)
            {
                case PortablePdb:
                case CovariantReturnsOfClasses:
                case UnmanagedSignatureCallingConvention:
                case DefaultImplementationsOfInterfaces:
                case VirtualStaticsInInterfaces:
                    return true;
                case nameof(IsDynamicCodeSupported):
                    return IsDynamicCodeSupported;
                case nameof(IsDynamicCodeCompiled):
                    return IsDynamicCodeCompiled;
            }

            return false;
        }
    }
}
