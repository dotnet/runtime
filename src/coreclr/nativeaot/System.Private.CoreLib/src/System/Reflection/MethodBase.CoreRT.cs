// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Reflection.Augments;

namespace System.Reflection
{
    public abstract partial class MethodBase : MemberInfo
    {
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => ReflectionAugments.ReflectionCoreCallbacks.GetMethodFromHandle(handle);
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType) => ReflectionAugments.ReflectionCoreCallbacks.GetMethodFromHandle(handle, declaringType);

        [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
        [System.Runtime.CompilerServices.Intrinsic]
        public static MethodBase GetCurrentMethod() { throw NotImplemented.ByDesign; } //Implemented by toolchain.

        // This is not an api but needs to be declared public so that System.Private.Reflection.Core can access (and override it)
        public virtual ParameterInfo[] GetParametersNoCopy() => GetParameters();

        //
        // MethodBase MetadataDefinitionMethod { get; }
        //
        //  Returns the canonical MethodBase that this is an instantiation or reflected mirror of.
        //  If MethodBase is already a canonical MethodBase, returns "this".
        //
        //  Guarantees on returned MethodBase:
        //
        //    IsConstructedGenericMethod == false.
        //    DeclaringType.IsConstructedGenericType == false.
        //    ReflectedType == DeclaringType
        //
        //  Throws NotSupportedException if the MethodBase is not a metadata-represented method
        //  (for example, the methods returned on Array types.)
        //
        // This is not an api but needs to be declared public so that System.Private.Reflection.Core can access (and override it)
        public virtual MethodBase MetadataDefinitionMethod { get { throw NotImplemented.ByDesign; } }
    }
}
