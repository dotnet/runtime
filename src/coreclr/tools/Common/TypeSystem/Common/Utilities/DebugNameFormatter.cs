// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Internal.TypeSystem
{
    public partial class DebugNameFormatter : TypeNameFormatter<DebugNameFormatter.Void, DebugNameFormatter.FormatOptions>
    {
        public static readonly DebugNameFormatter Instance = new DebugNameFormatter();

        public override Void AppendName(StringBuilder sb, ArrayType type, FormatOptions options)
        {
            AppendName(sb, type.ElementType, options);

            if (!type.IsSzArray && type.Rank == 1)
            {
                sb.Append("[*]");
            }
            else
            {
                sb.Append('[');
                sb.Append(',', type.Rank - 1);
                sb.Append(']');
            }

            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, ByRefType type, FormatOptions options)
        {
            AppendName(sb, type.ParameterType, options);
            sb.Append('&');

            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, PointerType type, FormatOptions options)
        {
            AppendName(sb, type.ParameterType, options);
            sb.Append('*');

            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, FunctionPointerType type, FormatOptions options)
        {
            MethodSignature signature = type.Signature;

            sb.Append("(*");
            AppendName(sb, signature.ReturnType, options);
            sb.Append(")(");
            for (int i = 0; i < signature.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                AppendName(sb, signature[i], options);
            }
            sb.Append(')');

            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, GenericParameterDesc type, FormatOptions options)
        {
            sb.Append(type.DiagnosticName);
            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, SignatureMethodVariable type, FormatOptions options)
        {
            sb.Append("!!");
            sb.Append(type.Index.ToStringInvariant());

            return Void.Value;
        }

        public override Void AppendName(StringBuilder sb, SignatureTypeVariable type, FormatOptions options)
        {
            sb.Append('!');
            sb.Append(type.Index.ToStringInvariant());

            return Void.Value;
        }

        protected override Void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType, FormatOptions options)
        {
            if ((options & FormatOptions.NamespaceQualify) != 0)
            {
                AppendName(sb, containingType, options);
                sb.Append('+');
            }

            sb.Append(nestedType.DiagnosticName);

            return Void.Value;
        }

        protected override Void AppendNameForNamespaceType(StringBuilder sb, DefType type, FormatOptions options)
        {
            int initialLen = sb.Length;
            try
            {
                // Shortcut some of the well known types
                switch (type.Category)
                {
                    case TypeFlags.Void:
                        sb.Append("void");
                        return Void.Value;
                    case TypeFlags.Boolean:
                        sb.Append("bool");
                        return Void.Value;
                    case TypeFlags.Char:
                        sb.Append("char");
                        return Void.Value;
                    case TypeFlags.SByte:
                        sb.Append("int8");
                        return Void.Value;
                    case TypeFlags.Byte:
                        sb.Append("uint8");
                        return Void.Value;
                    case TypeFlags.Int16:
                        sb.Append("int16");
                        return Void.Value;
                    case TypeFlags.UInt16:
                        sb.Append("uint16");
                        return Void.Value;
                    case TypeFlags.Int32:
                        sb.Append("int32");
                        return Void.Value;
                    case TypeFlags.UInt32:
                        sb.Append("uint32");
                        return Void.Value;
                    case TypeFlags.Int64:
                        sb.Append("int64");
                        return Void.Value;
                    case TypeFlags.UInt64:
                        sb.Append("uint64");
                        return Void.Value;
                    case TypeFlags.IntPtr:
                        sb.Append("native int");
                        return Void.Value;
                    case TypeFlags.UIntPtr:
                        sb.Append("native uint");
                        return Void.Value;
                    case TypeFlags.Single:
                        sb.Append("float32");
                        return Void.Value;
                    case TypeFlags.Double:
                        sb.Append("float64");
                        return Void.Value;
                }

                if (type.IsString)
                {
                    sb.Append("string");
                    return Void.Value;
                }

                if (type.IsObject)
                {
                    sb.Append("object");
                    return Void.Value;
                }

                AssemblyQualify(sb, type, options);
                NamespaceQualify(sb, type, options);
                sb.Append(type.DiagnosticName);
            }
            catch
            {
                sb.Length = initialLen;

                AssemblyQualify(sb, type, options);
                NamespaceQualify(sb, type, options);
                sb.Append(type.DiagnosticName);
            }

            return Void.Value;
        }

        private static void AssemblyQualify(StringBuilder sb, DefType type, FormatOptions options)
        {
            if (((options & FormatOptions.AssemblyQualify) != 0)
                && type is MetadataType mdType
                && mdType.Module is IAssemblyDesc)
            {
                sb.Append('[');

                // Trim the "System.Private." prefix
                string assemblyName;
                try
                {
                    assemblyName = ((IAssemblyDesc)mdType.Module).GetName().Name;
                }
                catch
                {
                    assemblyName = "Unknown";
                }

                if (assemblyName.StartsWith("System.Private", StringComparison.Ordinal))
                {
                    sb.Append("S.P");
                    sb.Append(assemblyName, 14, assemblyName.Length - 14);
                }
                else
                {
                    sb.Append(assemblyName);
                }

                sb.Append(']');
            }
        }

        private static void NamespaceQualify(StringBuilder sb, DefType type, FormatOptions options)
        {
            if ((options & FormatOptions.NamespaceQualify) != 0)
            {
                string ns = type.DiagnosticNamespace;
                if (!string.IsNullOrEmpty(ns))
                {
                    sb.Append(ns);
                    sb.Append('.');
                }
            }
        }

        protected override Void AppendNameForInstantiatedType(StringBuilder sb, DefType type, FormatOptions options)
        {
            AppendName(sb, type.GetTypeDefinition(), options);

            FormatOptions parameterOptions = options & ~FormatOptions.AssemblyQualify;

            sb.Append('<');

            for (int i = 0; i < type.Instantiation.Length; i++)
            {
                if (i != 0)
                    sb.Append(',');

                AppendName(sb, type.Instantiation[i], parameterOptions);
            }

            sb.Append('>');

            return Void.Value;
        }

        protected override DefType GetContainingType(DefType possibleInnerType, FormatOptions options)
        {
            try
            {
                return possibleInnerType.ContainingType;
            }
            catch
            {
                return null;
            }
        }

        public struct Void
        {
            public static Void Value => default(Void);
        }

        [Flags]
        public enum FormatOptions
        {
            None = 0,
            AssemblyQualify = 0x1,
            NamespaceQualify = 0x2,

            Default = AssemblyQualify | NamespaceQualify,
        }
    }
}
