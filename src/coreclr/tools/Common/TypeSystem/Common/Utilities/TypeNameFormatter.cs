// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Provides services to convert types to strings.
    /// </summary>
    public abstract class TypeNameFormatter
    {
        public void AppendName(StringBuilder sb, TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                    AppendName(sb, (ArrayType)type);
                    return;
                case TypeFlags.ByRef:
                    AppendName(sb, (ByRefType)type);
                    return;
                case TypeFlags.Pointer:
                    AppendName(sb, (PointerType)type);
                    return;
                case TypeFlags.FunctionPointer:
                    AppendName(sb, (FunctionPointerType)type);
                    return;
                case TypeFlags.GenericParameter:
                    AppendName(sb, (GenericParameterDesc)type);
                    return;
                case TypeFlags.SignatureTypeVariable:
                    AppendName(sb, (SignatureTypeVariable)type);
                    return;
                case TypeFlags.SignatureMethodVariable:
                    AppendName(sb, (SignatureMethodVariable)type);
                    return;
                default:
                    Debug.Assert(type.IsDefType);
                    AppendName(sb, (DefType)type);
                    return;
            }
        }

        public void AppendName(StringBuilder sb, DefType type)
        {
            if (!type.IsTypeDefinition)
            {
                AppendNameForInstantiatedType(sb, type);
            }
            else
            {
                DefType containingType = type.ContainingType;
                if (containingType != null)
                    AppendNameForNestedType(sb, type, containingType);
                else
                    AppendNameForNamespaceType(sb, type);
            }
        }

        public abstract void AppendName(StringBuilder sb, ArrayType type);
        public abstract void AppendName(StringBuilder sb, ByRefType type);
        public abstract void AppendName(StringBuilder sb, PointerType type);
        public abstract void AppendName(StringBuilder sb, FunctionPointerType type);
        public abstract void AppendName(StringBuilder sb, GenericParameterDesc type);
        public abstract void AppendName(StringBuilder sb, SignatureMethodVariable type);
        public abstract void AppendName(StringBuilder sb, SignatureTypeVariable type);

        protected abstract void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType);
        protected abstract void AppendNameForNamespaceType(StringBuilder sb, DefType type);
        protected abstract void AppendNameForInstantiatedType(StringBuilder sb, DefType type);

        public string FormatName(TypeDesc type)
        {
            StringBuilder sb = new StringBuilder();
            AppendName(sb, type);
            return sb.ToString();
        }
    }

    public abstract class TypeNameFormatter<TState, TOptions>
    {
        public TState AppendName(StringBuilder sb, TypeDesc type, TOptions options)
        {
            // Don't use Category to switch here as it may fail for some types, and the DebugNameFormatter
            // which is derived from that type cannot tolerate those failures
            return type switch
            {
                ArrayType arrayType => AppendName(sb, arrayType, options),
                ByRefType byRefType => AppendName(sb, byRefType, options),
                PointerType pointerType => AppendName(sb, pointerType, options),
                FunctionPointerType functionPointerType => AppendName(sb, functionPointerType, options),
                GenericParameterDesc genericPointerType => AppendName(sb, genericPointerType, options),
                SignatureTypeVariable sigTypeVar => AppendName(sb, sigTypeVar, options),
                SignatureMethodVariable sigMethodVar => AppendName(sb, sigMethodVar, options),
                _ => AppendName(sb, (DefType)type, options)
            };
        }

        public TState AppendName(StringBuilder sb, DefType type, TOptions options)
        {
            if (!type.IsTypeDefinition)
            {
                return AppendNameForInstantiatedType(sb, type, options);
            }
            else
            {
                DefType containingType = GetContainingType(type, options);

                if (containingType != null)
                    return AppendNameForNestedType(sb, type, containingType, options);
                else
                    return AppendNameForNamespaceType(sb, type, options);
            }
        }

        public abstract TState AppendName(StringBuilder sb, ArrayType type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, ByRefType type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, PointerType type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, FunctionPointerType type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, GenericParameterDesc type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, SignatureMethodVariable type, TOptions options);
        public abstract TState AppendName(StringBuilder sb, SignatureTypeVariable type, TOptions options);

        protected abstract TState AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType, TOptions options);
        protected abstract TState AppendNameForNamespaceType(StringBuilder sb, DefType type, TOptions options);
        protected abstract TState AppendNameForInstantiatedType(StringBuilder sb, DefType type, TOptions options);

        protected virtual DefType GetContainingType(DefType possibleInnerType, TOptions options)
        {
            return possibleInnerType.ContainingType;
        }

        public string FormatName(TypeDesc type, TOptions options)
        {
            StringBuilder sb = new StringBuilder();
            AppendName(sb, type, options);
            return sb.ToString();
        }
    }
}
