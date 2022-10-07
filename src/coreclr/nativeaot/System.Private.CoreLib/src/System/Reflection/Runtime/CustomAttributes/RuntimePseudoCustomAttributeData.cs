// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.CustomAttributes
{
    //
    // The Runtime's implementation of a pseudo-CustomAttributeData.
    //
    internal sealed class RuntimePseudoCustomAttributeData : RuntimeCustomAttributeData
    {
        public RuntimePseudoCustomAttributeData(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type attributeType, IList<CustomAttributeTypedArgument> constructorArguments)
        {
            _attributeType = attributeType;
            constructorArguments ??= Array.Empty<CustomAttributeTypedArgument>();
            _constructorArguments = new ReadOnlyCollection<CustomAttributeTypedArgument>(constructorArguments);
        }

        public sealed override Type AttributeType
        {
            get
            {
                return _attributeType;
            }
        }

        public sealed override ConstructorInfo Constructor
        {
            get
            {
                int numArguments = _constructorArguments.Count;
                if (numArguments == 0)
                    return ResolveAttributeConstructor(_attributeType, Array.Empty<Type>());

                Type[] expectedParameterTypes = new Type[numArguments];
                for (int i = 0; i < numArguments; i++)
                {
                    expectedParameterTypes[i] = _constructorArguments[i].ArgumentType;
                }
                return ResolveAttributeConstructor(_attributeType, expectedParameterTypes);
            }
        }

        internal sealed override IList<CustomAttributeTypedArgument> GetConstructorArguments(bool throwIfMissingMetadata)
        {
            return _constructorArguments;
        }

        internal sealed override IList<CustomAttributeNamedArgument> GetNamedArguments(bool throwIfMissingMetadata)
        {
            // Note: if we ever need to return non-empty named arguments, we need to ensure the reflection metadata for the
            // corresponding fields/properties is kept (we might have to bump the dataflow annotation on _attributeType).
            return Array.Empty<CustomAttributeNamedArgument>();
        }

        // Equals/GetHashCode no need to override (they just implement reference equality but desktop never unified these things.)

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        private readonly Type _attributeType;
        private readonly ReadOnlyCollection<CustomAttributeTypedArgument> _constructorArguments;
    }
}
