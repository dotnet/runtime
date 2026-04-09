// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public abstract class LazyGenericsPolicy
    {
        public abstract bool UsesLazyGenerics(MethodDesc method);
        public abstract bool UsesLazyGenerics(TypeDesc type);
        public abstract bool UsesLazyGenerics(MetadataType type);
    }

    public sealed class AttributeDrivenLazyGenericsPolicy : LazyGenericsPolicy
    {
        public sealed override bool UsesLazyGenerics(MethodDesc method)
        {
            if (UsesLazyGenerics(method.OwningType))
                return true;

            if (method.HasInstantiation)
                return method.IsVirtual || method.HasCustomAttribute("System.Runtime.CompilerServices", "ForceLazyDictionaryAttribute");

            return false;
        }

        public sealed override bool UsesLazyGenerics(TypeDesc type)
        {
            if (type is MetadataType)
                return UsesLazyGenerics(type as MetadataType);
            else
                return false;
        }

        public sealed override bool UsesLazyGenerics(MetadataType type)
        {
            return type.HasCustomAttribute("System.Runtime.CompilerServices", "ForceLazyDictionaryAttribute");
        }
    }

    public sealed class LazyGenericsDisabledPolicy : LazyGenericsPolicy
    {
        public sealed override bool UsesLazyGenerics(MethodDesc method) => false;
        public sealed override bool UsesLazyGenerics(TypeDesc type) => false;
        public sealed override bool UsesLazyGenerics(MetadataType type) => false;
    }
}
