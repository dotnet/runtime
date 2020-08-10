// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides support for design-time license context serialization.
    /// </summary>
    public class DesigntimeLicenseContextSerializer
    {
        // Not creatable.
        private DesigntimeLicenseContextSerializer()
        {
        }

        /// <summary>
        /// Serializes the licenses within the specified design-time license context
        /// using the specified key and output stream.
        /// </summary>
        public static void Serialize(Stream o, string cryptoKey, DesigntimeLicenseContext context)
        {
            IFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Issue https://github.com/dotnet/runtime/issues/39293 tracks finding an alternative to BinaryFormatter
            formatter.Serialize(o, new object[] { cryptoKey, context._savedLicenseKeys });
#pragma warning restore SYSLIB0011
        }

        internal static void Deserialize(Stream o, string cryptoKey, RuntimeLicenseContext context)
        {
#pragma warning disable SYSLIB0011 // Issue https://github.com/dotnet/runtime/issues/39293 tracks finding an alternative to BinaryFormatter
            IFormatter formatter = new BinaryFormatter();

            object obj = formatter.Deserialize(o);
#pragma warning restore SYSLIB0011

            if (obj is object[] value)
            {
                if (value[0] is string && (string)value[0] == cryptoKey)
                {
                    context._savedLicenseKeys = (Hashtable)value[1];
                }
            }
        }
    }
}
