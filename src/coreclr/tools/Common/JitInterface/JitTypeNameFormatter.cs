// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    // This is a very rough equivalent of typestring.cpp in the CLR used with
    // FormatNamespace|FormatNoInst, with some adjustments related to generic
    // instantiations. This is currently only used for printClassName in the
    // JIT-EE interface that is used for the various method set JIT variables
    // (JitDisasm for example).
    internal sealed class JitTypeNameFormatter : TypeNameFormatter
    {
        public static JitTypeNameFormatter Instance { get; } = new JitTypeNameFormatter();

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
            Debug.Fail("Unexpected signature type variable in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
        {
            Debug.Fail("Unexpected signature method variable in JitTypeNameFormatter");
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
            sb.Append(',', type.Rank - 1);
            sb.Append(']');
        }

        protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
        {
            AppendName(sb, type.GetTypeDefinition());
            // Type name intentionally excludes instantiations.
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
