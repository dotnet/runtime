// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public struct CallingConventionConverterKey : IEquatable<CallingConventionConverterKey>
    {
        public CallingConventionConverterKey(Internal.NativeFormat.CallingConventionConverterKind converterKind,
                                             MethodSignature signature)
        {
            ConverterKind = converterKind;
            Signature = signature;
        }

        public Internal.NativeFormat.CallingConventionConverterKind ConverterKind { get; }
        public MethodSignature Signature { get; }

        public override bool Equals(object obj)
        {
            return obj is CallingConventionConverterKey && Equals((CallingConventionConverterKey)obj);
        }

        public bool Equals(CallingConventionConverterKey other)
        {
            if (ConverterKind != other.ConverterKind)
                return false;

            if (!Signature.Equals(other.Signature))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return Signature.GetHashCode() ^ (int)ConverterKind;
        }

        public string GetName()
        {
            return ConverterKind.ToString() + Signature.GetName();
        }
    }

    public static class MethodSignatureExtensions
    {
        public static void AppendName(this MethodSignature signature, StringBuilder nameBuilder, UniqueTypeNameFormatter typeNameFormatter)
        {
            if (signature.GenericParameterCount > 0)
            {
                nameBuilder.Append("GenParams:");
                nameBuilder.Append(signature.GenericParameterCount);
                nameBuilder.Append(' ');
            }

            if (signature.IsStatic)
                nameBuilder.Append("Static ");

            typeNameFormatter.AppendName(nameBuilder, signature.ReturnType);
            nameBuilder.Append('(');
            for (int i = 0; i < signature.Length; i++)
            {
                if (i != 0)
                    nameBuilder.Append(',');
                typeNameFormatter.AppendName(nameBuilder, signature[i]);
            }
            nameBuilder.Append(')');
        }

        public static string GetName(this MethodSignature signature)
        {
            StringBuilder nameBuilder = new StringBuilder();
            signature.AppendName(nameBuilder, UniqueTypeNameFormatter.Instance);
            return nameBuilder.ToString();
        }
    }

    public class UniqueTypeNameFormatter : TypeNameFormatter
    {
        public static UniqueTypeNameFormatter Instance { get; } = new UniqueTypeNameFormatter();

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

            if (type.Rank == 1 && type.IsMdArray)
                sb.Append('*');
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
            string ns = GetTypeNamespace(type);
            if (ns.Length > 0)
            {
                AppendEscapedIdentifier(sb, ns);
                sb.Append('.');
            }
            AppendEscapedIdentifier(sb, GetTypeName(type));

            if (type is MetadataType)
            {
                IAssemblyDesc homeAssembly = ((MetadataType)type).Module as IAssemblyDesc;
                AppendAssemblyName(sb, homeAssembly);
            }
        }

        private static void AppendAssemblyName(StringBuilder sb, IAssemblyDesc assembly)
        {
            if (assembly == null)
                return;

            sb.Append(',');
            AppendEscapedIdentifier(sb, assembly.GetName().Name);
        }

        protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
        {
            AppendName(sb, containingType);

            sb.Append('+');

            string ns = GetTypeNamespace(nestedType);
            if (ns.Length > 0)
            {
                AppendEscapedIdentifier(sb, ns);
                sb.Append('.');
            }
            AppendEscapedIdentifier(sb, GetTypeName(nestedType));
        }

        private static string GetTypeName(DefType type)
        {
            return type.Name;
        }

        private static string GetTypeNamespace(DefType type)
        {
            return type.Namespace;
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
