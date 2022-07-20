// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class DisplayNameHelpers
    {
        public static string GetDisplayName(this TypeSystemEntity entity)
        {
            return entity switch
            {
                MethodDesc method => method.GetDisplayName(),
                FieldDesc field => field.GetDisplayName(),
                TypeDesc type => type.GetDisplayName(),
#if !READYTORUN
                PropertyPseudoDesc property => property.GetDisplayName(),
                EventPseudoDesc @event => @event.GetDisplayName(),
#endif
                _ => throw new InvalidOperationException(),
            };
        }

        public static string GetDisplayName(this MethodDesc method)
        {
            var sb = new StringBuilder();

            sb.Append(method.OwningType.GetDisplayName());
            sb.Append('.');

            if (method.IsConstructor)
            {
                sb.Append(method.OwningType.GetDisplayNameWithoutNamespace());
            }
            else if (method.GetPropertyForAccessor() is PropertyPseudoDesc property)
            {
                sb.Append(property.Name);
                sb.Append('.');
                sb.Append(property.GetMethod == method ? "get" : "set");
            }
            else
            {
                sb.Append(method.Name);
            }

            if (method.HasInstantiation)
            {
                sb.Append('<');
                for (int i = 0; i < method.Instantiation.Length - 1; i++)
                    sb.Append(method.Instantiation[i].GetDisplayNameWithoutNamespace()).Append(',');

                sb.Append(method.Instantiation[method.Instantiation.Length - 1].GetDisplayNameWithoutNamespace());
                sb.Append('>');
            }

            // Append parameters
            sb.Append('(');
            if (method.Signature.Length > 0)
            {
                for (int i = 0; i < method.Signature.Length - 1; i++)
                    sb.Append(method.Signature[i].GetDisplayNameWithoutNamespace()).Append(',');

                sb.Append(method.Signature[method.Signature.Length - 1].GetDisplayNameWithoutNamespace());
            }

            sb.Append(')');

            return sb.ToString();
        }

        public static string GetParameterDisplayName(this EcmaMethod method, int parameterIndex)
        {
            var reader = method.MetadataReader;
            var methodDefinition = reader.GetMethodDefinition(method.Handle);
            foreach (var parameterHandle in methodDefinition.GetParameters())
            {
                var parameter = reader.GetParameter(parameterHandle);
                if (parameter.SequenceNumber == parameterIndex + 1)
                    return reader.GetString(parameter.Name);
            }

            return $"#{parameterIndex}";
        }

        public static string GetDisplayName(this FieldDesc field)
        {
            return new StringBuilder(field.OwningType.GetDisplayName())
                .Append('.')
                .Append(field.Name).ToString();
        }

#if !READYTORUN
        public static string GetDisplayName(this PropertyPseudoDesc property)
        {
            return new StringBuilder(property.OwningType.GetDisplayName())
                .Append('.')
                .Append(property.Name).ToString();
        }
        
        public static string GetDisplayName(this EventPseudoDesc @event)
        {
            return new StringBuilder(@event.OwningType.GetDisplayName())
                .Append('.')
                .Append(@event.Name).ToString();
        }
#endif

        public static string GetDisplayName(this TypeDesc type)
        {
            return Formatter.Instance.FormatName(type, FormatOptions.NamespaceQualify);
        }

        public static string GetDisplayNameWithoutNamespace(this TypeDesc type)
        {
            return Formatter.Instance.FormatName(type, FormatOptions.None);
        }

        private class Formatter : TypeNameFormatter<Formatter.Unit, FormatOptions>
        {
            public readonly static Formatter Instance = new Formatter();

            public override Unit AppendName(StringBuilder sb, ArrayType type, FormatOptions options)
            {
                AppendName(sb, type.ElementType, options);
                sb.Append('[');
                if (type.Rank > 1)
                    sb.Append(new string(',', type.Rank - 1));
                sb.Append(']');
                return default;
            }

            public override Unit AppendName(StringBuilder sb, ByRefType type, FormatOptions options)
            {
                AppendName(sb, type.ParameterType, options);
                sb.Append('&');
                return default;
            }

            public override Unit AppendName(StringBuilder sb, PointerType type, FormatOptions options)
            {
                AppendName(sb, type.ParameterType, options);
                sb.Append('*');
                return default;
            }

            public override Unit AppendName(StringBuilder sb, FunctionPointerType type, FormatOptions options)
            {
                MethodSignature signature = type.Signature;

                sb.Append("delegate*<");
                for (int i = 0; i < signature.Length; i++)
                {
                    AppendName(sb, signature[i], options);
                    sb.Append(',');
                }
                AppendName(sb, signature.ReturnType, options);
                sb.Append('>');

                return default;
            }

            public override Unit AppendName(StringBuilder sb, GenericParameterDesc type, FormatOptions options)
            {
                sb.Append(type.Name);
                return default;
            }

            public override Unit AppendName(StringBuilder sb, SignatureMethodVariable type, FormatOptions options)
            {
                sb.Append("!!" + type.Index);
                return default;
            }

            public override Unit AppendName(StringBuilder sb, SignatureTypeVariable type, FormatOptions options)
            {
                sb.Append("!" + type.Index);
                return default;
            }

            protected override Unit AppendNameForInstantiatedType(StringBuilder sb, DefType type, FormatOptions options)
            {
                AppendName(sb, type.GetTypeDefinition(), options);

                FormatOptions parameterOptions = options & ~FormatOptions.NamespaceQualify;

                sb.Append('<');

                for (int i = 0; i < type.Instantiation.Length; i++)
                {
                    if (i != 0)
                        sb.Append(',');

                    AppendName(sb, type.Instantiation[i], parameterOptions);
                }

                sb.Append('>');

                return default;
            }

            protected override Unit AppendNameForNamespaceType(StringBuilder sb, DefType type, FormatOptions options)
            {
                NamespaceQualify(sb, type, options);
                sb.Append(type.Name);
                return default;
            }

            protected override Unit AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType, FormatOptions options)
            {
                if ((options & FormatOptions.NamespaceQualify) != 0)
                {
                    AppendName(sb, containingType, options);
                    sb.Append('.');
                }

                sb.Append(nestedType.Name);

                return default;
            }

            private void NamespaceQualify(StringBuilder sb, DefType type, FormatOptions options)
            {
                if ((options & FormatOptions.NamespaceQualify) != 0)
                {
                    string ns = type.Namespace;
                    if (!string.IsNullOrEmpty(ns))
                    {
                        sb.Append(ns);
                        sb.Append('.');
                    }
                }
            }

            public struct Unit { }
        }

        private enum FormatOptions
        {
            None = 0,
            NamespaceQualify = 1,
        }
    }
}
