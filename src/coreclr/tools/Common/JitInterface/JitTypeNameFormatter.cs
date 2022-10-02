// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    // This is a very rough equivalent of typestring.cpp in the CLR used with
    // FormatNamespace|FormatNoInst. This is currently only used for
    // appendClassName in the JIT-EE interface, which is used to print class
    // names to use for the various method set JIT variables (JitDisasm for
    // example). The JIT handles formatting of most type names on its own so we
    // only need the basics here.
    internal sealed class JitTypeNameFormatter : TypeNameFormatter
    {
        public static JitTypeNameFormatter Instance { get; } = new JitTypeNameFormatter();

        public override void AppendName(StringBuilder sb, PointerType type)
        {
            Debug.Fail("Unexpected pointer type in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, GenericParameterDesc type)
        {
            Debug.Fail("Unexpected generic parameter in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, SignatureTypeVariable type)
        {
            Debug.Fail("Unexpected TVar in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
        {
            Debug.Fail("Unexpected MVar in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, FunctionPointerType type)
        {
            Debug.Fail("Unexpected function pointer type in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, ByRefType type)
        {
            Debug.Fail("Unexpected ByRef type in JitTypeNameFormatter");
        }

        public override void AppendName(StringBuilder sb, ArrayType type)
        {
            Debug.Fail("Unexpected array type in JitTypeNameFormatter");
        }

        protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
        {
            AppendName(sb, type.GetTypeDefinition());
            // Instantiation itself is handled by JIT.
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
