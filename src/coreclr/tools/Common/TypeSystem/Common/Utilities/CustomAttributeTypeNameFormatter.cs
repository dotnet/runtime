// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Formats type names in the 'SerString' format as defined by the ECMA-335 standard.
    /// This is the inverse of what <see cref="CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(ModuleDesc, string, bool)"/> does.
    /// </summary>
    public sealed class CustomAttributeTypeNameFormatter : TypeNameFormatter<IAssemblyDesc, bool>
    {
        private readonly IAssemblyDesc _relativeHomeAssembly;

        public CustomAttributeTypeNameFormatter()
        {
        }

        public CustomAttributeTypeNameFormatter(IAssemblyDesc relativeHomeAssembly)
        {
            _relativeHomeAssembly = relativeHomeAssembly;
        }

        private void AppendAssemblyName(StringBuilder sb, IAssemblyDesc assembly)
        {
            if (assembly == _relativeHomeAssembly || assembly == null)
                return;

            sb.Append(',');
            AppendEscapedIdentifier(sb, assembly.GetName().Name);
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, ArrayType type, bool assemblyQualify)
        {
            IAssemblyDesc homeAssembly = AppendName(sb, type.ElementType, false);

            if (type.IsSzArray)
            {
                sb.Append("[]");
            }
            else if (type.Rank == 1)
            {
                sb.Append("[*]");
            }
            else
            {
                sb.Append('[');
                sb.Append(',', type.Rank - 1);
                sb.Append(']');
            }

            if (assemblyQualify)
                AppendAssemblyName(sb, homeAssembly);

            return homeAssembly;
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, ByRefType type, bool assemblyQualify)
        {
            IAssemblyDesc homeAssembly = AppendName(sb, type.ParameterType, false);

            sb.Append('&');

            if (assemblyQualify)
                AppendAssemblyName(sb, homeAssembly);

            return homeAssembly;
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, PointerType type, bool assemblyQualify)
        {
            IAssemblyDesc homeAssembly = AppendName(sb, type.ParameterType, false);

            sb.Append('*');

            if (assemblyQualify)
                AppendAssemblyName(sb, homeAssembly);

            return homeAssembly;
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, FunctionPointerType type, bool assemblyQualify)
        {
            throw new NotSupportedException();
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, GenericParameterDesc type, bool assemblyQualify)
        {
            throw new NotSupportedException();
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, SignatureMethodVariable type, bool assemblyQualify)
        {
            throw new NotSupportedException();
        }

        public override IAssemblyDesc AppendName(StringBuilder sb, SignatureTypeVariable type, bool assemblyQualify)
        {
            throw new NotSupportedException();
        }

        protected override IAssemblyDesc AppendNameForInstantiatedType(StringBuilder sb, DefType type, bool assemblyQualify)
        {
            IAssemblyDesc homeAssembly = AppendName(sb, type.GetTypeDefinition(), false);

            sb.Append('[');

            for (int i = 0; i < type.Instantiation.Length; i++)
            {
                if (i != 0)
                    sb.Append(',');

                sb.Append('[');
                AppendName(sb, type.Instantiation[i], true);
                sb.Append(']');
            }

            sb.Append(']');

            if (assemblyQualify)
                AppendAssemblyName(sb, homeAssembly);

            return homeAssembly;
        }

        protected override IAssemblyDesc AppendNameForNamespaceType(StringBuilder sb, DefType type, bool assemblyQualify)
        {
            string ns = type.Namespace;
            if (ns.Length > 0)
            {
                AppendEscapedIdentifier(sb, ns);
                sb.Append('.');
            }
            AppendEscapedIdentifier(sb, type.Name);

            if (type is MetadataType mdType)
            {
                Debug.Assert(mdType.Module is IAssemblyDesc, "Multi-module?");

                if (assemblyQualify)
                    AppendAssemblyName(sb, (IAssemblyDesc)mdType.Module);

                return (IAssemblyDesc)mdType.Module;
            }

            return null;
        }

        protected override IAssemblyDesc AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType, bool assemblyQualify)
        {
            IAssemblyDesc homeAssembly = AppendName(sb, containingType, false);

            sb.Append('+');

            AppendEscapedIdentifier(sb, nestedType.Name);

            if (assemblyQualify)
                AppendAssemblyName(sb, homeAssembly);

            return homeAssembly;
        }

        private static char[] s_escapedChars = new char[] { ',', '=', '"', ']', '[', '*', '&', '+', '\\' };
        private static void AppendEscapedIdentifier(StringBuilder sb, string identifier)
        {
            if (identifier.IndexOfAny(s_escapedChars) < 0)
            {
                string escapedIdentifier = identifier;
                foreach (char escapedChar in s_escapedChars)
                {
                    string escapedCharString = new string(escapedChar, 1);
                    escapedIdentifier = escapedIdentifier.Replace(escapedCharString, "\\" + escapedCharString);
                }
                sb.Append(escapedIdentifier);
            }
            else
            {
                sb.Append(identifier);
            }
        }
    }
}
