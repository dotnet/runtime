// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos that represent non-constructed types (IsTypeDefinition == true)
    //
    internal abstract class RuntimeTypeDefinitionTypeInfo : RuntimeTypeInfo
    {
        public sealed override bool IsTypeDefinition => true;

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            // Do not rewrite as a call to IsConstructedGenericType - we haven't yet established that "other" is a runtime-implemented member yet!
            if (other is RuntimeType otherRuntimeType && otherRuntimeType.IsConstructedGenericType)
                other = otherRuntimeType.GetGenericTypeDefinition();

            // Unlike most other MemberInfo objects, types never get cloned due to containing generic types being instantiated.
            // That is, their DeclaringType is always the generic type definition. As a Type, the ReflectedType property is always equal to the DeclaringType.
            //
            // Because of these conditions, we can safely implement both the method token equivalence and the "is this type from the same implementor"
            // check as our regular Equals() method.
            return ToType().Equals(other);
        }
    }
}
