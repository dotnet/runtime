// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides support for design-time license context serialization.
    ///    </para>
    /// </summary>
    public class DesigntimeLicenseContextSerializer
    {
        // not creatable...
        //
        private DesigntimeLicenseContextSerializer()
        {
        }

        /// <summary>
        ///    <para>
        ///       Serializes the licenses within the specified design-time license context
        ///       using the specified key and output stream.
        ///    </para>
        /// </summary>
        public static void Serialize(Stream o, string cryptoKey, DesigntimeLicenseContext context)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(o, new object[] { cryptoKey, context.savedLicenseKeys });
        }

        [SuppressMessage("Microsoft.Security", "CA2107:ReviewDenyAndPermitOnlyUsage")] // Use of PermitOnly here is appropriate. 
        internal static void Deserialize(Stream o, string cryptoKey, RuntimeLicenseContext context)
        {
            IFormatter formatter = new BinaryFormatter();

            object obj = formatter.Deserialize(o);

            if (obj is object[])
            {
                object[] value = (object[])obj;
                if (value[0] is string && (string)value[0] == cryptoKey)
                {
                    context.savedLicenseKeys = (Hashtable)value[1];
                }
            }
        }
    }
}
