// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Internal.TypeSystem
{
    public sealed class CSharpTypeNameFormatter : TypeNameFormatter
    {
        public static CSharpTypeNameFormatter Instance { get; } = new CSharpTypeNameFormatter();

        private CSharpTypeNameFormatter()
        {
        }

        public string FormatName(MethodDesc method)
        {
            StringBuilder sb = new StringBuilder();
            AppendName(sb, method.OwningType);
            sb.Append('.');
            sb.Append(method.GetName());

            if (method.HasInstantiation)
            {
                AppendGenericArguments(sb, method.Instantiation);
            }

            return sb.ToString();
        }

        public override void AppendName(StringBuilder sb, ArrayType type)
        {
            AppendName(sb, type.ElementType);
            sb.Append('[');
            if (type.IsMdArray && type.Rank == 1)
            {
                sb.Append('*');
            }
            else
            {
                sb.Append(',', type.Rank - 1);
            }
            sb.Append(']');
        }

        public override void AppendName(StringBuilder sb, ByRefType type)
        {
            AppendName(sb, type.ParameterType);
            sb.Append('&');
        }

        public override void AppendName(StringBuilder sb, PointerType type)
        {
            AppendName(sb, type.ParameterType);
            sb.Append('*');
        }

        public override void AppendName(StringBuilder sb, FunctionPointerType type)
        {
            MethodSignature signature = type.Signature;

            sb.Append("delegate*<");
            for (int i = 0; i < signature.Length; i++)
            {
                AppendName(sb, signature[i]);
                sb.Append(", ");
            }
            AppendName(sb, signature.ReturnType);
            sb.Append('>');
        }

        public override void AppendName(StringBuilder sb, GenericParameterDesc type)
        {
            sb.Append(type.Name);
        }

        public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
        {
            sb.Append("!!");
            sb.Append(type.Index);
        }

        public override void AppendName(StringBuilder sb, SignatureTypeVariable type)
        {
            sb.Append('!');
            sb.Append(type.Index);
        }

        protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
        {
            AppendName(sb, containingType);
            sb.Append('.');
            AppendTypeName(sb, nestedType);
        }

        protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
        {
            if (TryAppendKeyword(sb, type))
            {
                return;
            }

            string ns = type.GetNamespace();
            if (ns.Length > 0)
            {
                sb.Append(ns);
                sb.Append('.');
            }
            AppendTypeName(sb, type);
        }

        protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
        {
            int argumentIndex = 0;
            AppendInstantiatedType(sb, (DefType)type.GetTypeDefinition(), type.Instantiation, ref argumentIndex);
        }

        private void AppendInstantiatedType(StringBuilder sb, DefType type, Instantiation arguments, ref int argumentIndex)
        {
            DefType containingType = type.ContainingType;
            int containingArgumentCount = 0;
            if (containingType is not null)
            {
                AppendInstantiatedType(sb, containingType, arguments, ref argumentIndex);
                sb.Append('.');
                AppendTypeName(sb, type);
                containingArgumentCount = containingType.Instantiation.Length;
            }
            else
            {
                AppendNameForNamespaceType(sb, type);
            }

            int argumentCount = type.Instantiation.Length - containingArgumentCount;
            if (argumentCount > 0)
            {
                sb.Append('<');
                for (int i = 0; i < argumentCount; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    AppendName(sb, arguments[argumentIndex++]);
                }
                sb.Append('>');
            }
        }

        private static void AppendTypeName(StringBuilder sb, DefType type)
        {
            string name = type.GetName();
            int genericArityIndex = name.LastIndexOf('`');
            sb.Append(name, 0, genericArityIndex >= 0 ? genericArityIndex : name.Length);
        }

        private void AppendGenericArguments(StringBuilder sb, Instantiation arguments)
        {
            sb.Append('<');
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                AppendName(sb, arguments[i]);
            }
            sb.Append('>');
        }

        private static bool TryAppendKeyword(StringBuilder sb, DefType type)
        {
            string keyword = type.Category switch
            {
                TypeFlags.Void => "void",
                TypeFlags.Boolean => "bool",
                TypeFlags.Char => "char",
                TypeFlags.SByte => "sbyte",
                TypeFlags.Byte => "byte",
                TypeFlags.Int16 => "short",
                TypeFlags.UInt16 => "ushort",
                TypeFlags.Int32 => "int",
                TypeFlags.UInt32 => "uint",
                TypeFlags.Int64 => "long",
                TypeFlags.UInt64 => "ulong",
                TypeFlags.IntPtr => "nint",
                TypeFlags.UIntPtr => "nuint",
                TypeFlags.Single => "float",
                TypeFlags.Double => "double",
                _ when type.IsString => "string",
                _ when type.IsObject => "object",
                _ => null,
            };

            if (keyword is null)
            {
                return false;
            }

            sb.Append(keyword);
            return true;
        }
    }
}
