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
}
