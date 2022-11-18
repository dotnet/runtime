// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    // This is a very rough equivalent of typestring.cpp in the CLR.
    // There's way more rules to capture. Hopefully, we'll only ever need this in the code to recognize SIMD intrisics
    // and we won't need to replicate all the details around escaping and such.
    internal sealed class TypeString : TypeNameFormatter
    {
        public static TypeString Instance { get; } = new TypeString();

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
            sb.Append(type.Index);
        }

        public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
        {
            sb.Append("!!");
            sb.Append(type.Index);
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

            sb.Append(')');
        }

        public override void AppendName(StringBuilder sb, ByRefType type)
        {
            AppendName(sb, type.ParameterType);
            sb.Append('&');
        }

        public override void AppendName(StringBuilder sb, ArrayType type)
        {
            AppendName(sb, type.ElementType);
            sb.Append('[');

            int rank = type.Rank;
            if (rank == 1 && type.IsMdArray)
                sb.Append('*');
            else
                sb.Append(',', type.Rank - 1);

            sb.Append(']');
        }

        protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
        {
            AppendName(sb, type.GetTypeDefinition());
            sb.Append('[');

            for (int i = 0; i < type.Instantiation.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                AppendName(sb, type.Instantiation[i]);
            }

            sb.Append(']');
        }

        protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
        {
            string ns = type.Namespace;
            if (ns.Length > 0)
            {
                sb.Append(ns);
                sb.Append('.');
            }
            sb.Append(type.Name);
        }

        protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
        {
            AppendName(sb, containingType);
            sb.Append('+');
            sb.Append(nestedType.Name);
        }
    }
}
