// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Provides a name formatter that is compatible with SigFormat.cpp in the CLR.
    /// </summary>
    public partial class ExceptionTypeNameFormatter : TypeNameFormatter
    {
        public static ExceptionTypeNameFormatter Instance { get; } = new ExceptionTypeNameFormatter();

        public override void AppendName(StringBuilder sb, PointerType type)
        {
            AppendName(sb, type.ParameterType);
            sb.Append('*');
        }

        public override void AppendName(StringBuilder sb, GenericParameterDesc type)
        {
            string prefix = type.Kind == GenericParameterKind.Type ? "!" : "!!";
            sb.Append(prefix);
            sb.Append(type.Name);
        }

        public override void AppendName(StringBuilder sb, SignatureTypeVariable type)
        {
            sb.Append('!');
            sb.Append(type.Index.ToStringInvariant());
        }

        public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
        {
            sb.Append("!!");
            sb.Append(type.Index.ToStringInvariant());
        }

        public override void AppendName(StringBuilder sb, FunctionPointerType type)
        {
            MethodSignature signature = type.Signature;

            AppendName(sb, signature.ReturnType);

            sb.Append(" (");
            for (int i = 0; i < signature.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                AppendName(sb, signature[i]);
            }

            // TODO: Append '...' for vararg methods

            sb.Append(')');
        }

        public override void AppendName(StringBuilder sb, ByRefType type)
        {
            AppendName(sb, type.ParameterType);
            sb.Append(" ByRef");
        }

        public override void AppendName(StringBuilder sb, ArrayType type)
        {
            AppendName(sb, type.ElementType);
            sb.Append('[');

            // NOTE: We're ignoring difference between SzArray and MdArray rank 1 for SigFormat.cpp compat.
            sb.Append(',', type.Rank - 1);

            sb.Append(']');
        }

        protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
        {
            AppendName(sb, type.GetTypeDefinition());
            sb.Append('<');

            for (int i = 0; i < type.Instantiation.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                AppendName(sb, type.Instantiation[i]);
            }

            sb.Append('>');
        }

        protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
        {
            if (type.IsPrimitive)
            {
                sb.Append(GetTypeName(type));
            }
            else
            {
                string ns = GetTypeNamespace(type);
                if (ns.Length > 0)
                {
                    sb.Append(ns);
                    sb.Append('.');
                }
                sb.Append(GetTypeName(type));
            }
        }

        protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
        {
            // NOTE: We're ignoring the containing type for compatibility with SigFormat.cpp
            sb.Append(GetTypeName(nestedType));
        }
    }
}
