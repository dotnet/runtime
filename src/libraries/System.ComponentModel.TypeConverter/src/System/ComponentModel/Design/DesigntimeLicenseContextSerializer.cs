// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            formatter.Serialize(o, new object[] { cryptoKey, context._savedLicenseKeys });
        }

        [SuppressMessage("Microsoft.Security", "CA2107:ReviewDenyAndPermitOnlyUsage")] // Use of PermitOnly here is appropriate. 
        internal static void Deserialize(Stream o, string cryptoKey, RuntimeLicenseContext context)
        {
            IFormatter formatter = new BinaryFormatter();

            object obj = formatter.Deserialize(o);

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
