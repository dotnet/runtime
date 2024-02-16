// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection.Runtime.General;
using System.Text;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Internal.TypeSystem;

namespace System
{
    internal static class TypeLoaderFormattingHelpers
    {
        public static string ToStringInvariant(this int arg)
        {
            return arg.LowLevelToString();
        }

        public static string ToStringInvariant(this uint arg)
        {
            return arg.LowLevelToString();
        }

        public static string ToStringInvariant(this byte arg)
        {
            return arg.LowLevelToString();
        }

        public static string ToStringInvariant(this ushort arg)
        {
            return arg.LowLevelToString();
        }

        public static string ToStringInvariant(this ulong arg)
        {
            return arg.LowLevelToString();
        }

        public static string ToStringInvariant(this float _)
        {
            return "FLOAT";
        }

        public static string ToStringInvariant(this double _)
        {
            return "DOUBLE";
        }
    }
}

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Extension methods that provide low level ToString() equivalents for some of the core types.
    /// Calling regular ToString() on these types goes through a lot of the CultureInfo machinery
    /// which is not low level enough for the type loader purposes.
    /// </summary>
    internal static partial class LowLevelStringConverter
    {
        private const string HexDigits = "0123456789ABCDEF";

        private static string LowLevelToString(ulong arg, int shift)
        {
            StringBuilder sb = new StringBuilder(16);
            while (shift > 0)
            {
                shift -= 4;
                int digit = (int)((arg >> shift) & 0xF);
                sb.Append(HexDigits[digit]);
            }
            return sb.ToString();
        }

        public static string LowLevelToString(this LayoutInt arg)
        {
            if (arg.IsIndeterminate)
                return "Indeterminate";
            else
                return ((uint)arg.AsInt).LowLevelToString();
        }

        public static string LowLevelToString(this byte arg)
        {
            return LowLevelToString((ulong)arg, 4 * 2);
        }

        public static string LowLevelToString(this ushort arg)
        {
            return LowLevelToString((ulong)arg, 4 * 4);
        }

        public static string LowLevelToString(this int arg)
        {
            return ((uint)arg).LowLevelToString();
        }

        public static string LowLevelToString(this uint arg)
        {
            return LowLevelToString((ulong)arg, 4 * 8);
        }

        public static string LowLevelToString(this ulong arg)
        {
            return LowLevelToString((ulong)arg, 4 * 16);
        }

        public static string LowLevelToString(this IntPtr arg)
        {
            return LowLevelToString((ulong)arg, IntPtr.Size * 8);
        }

        public static string LowLevelToString(this RuntimeTypeHandle rtth)
        {
            QTypeDefinition qTypeDefinition;
            MetadataReader reader;

            // Try to get the name from metadata
            if (TypeLoaderEnvironment.TryGetMetadataForNamedType(rtth, out qTypeDefinition))
            {
                reader = qTypeDefinition.NativeFormatReader;
                TypeDefinitionHandle typeDefHandle = qTypeDefinition.NativeFormatHandle;
                return typeDefHandle.GetFullName(reader);
            }

            // Fallback implementation when no metadata available
            return LowLevelToStringRawEETypeAddress(rtth);
        }

        public static string LowLevelToStringRawEETypeAddress(this RuntimeTypeHandle rtth)
        {
            return "MethodTable:0x" + LowLevelToString(rtth.ToIntPtr());
        }
    }
}
