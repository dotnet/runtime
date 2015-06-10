// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace System.IO.Compression.Tests
{
    internal static class Common
    {
        internal static void SetDeflaterMode(string mode)
        {
            const string fieldName = "s_forcedTestingDeflaterType";
            FieldInfo forceType = typeof(DeflateStream).GetTypeInfo().GetDeclaredField(fieldName);
            if (forceType != null)
            {
                forceType.SetValue(null, mode == "zlib" ? (byte)2 : mode == "managed" ? (byte)1 : (byte)0);
            }
            else if (mode == "zlib" || mode == "managed")
            {
                Console.Error.WriteLine("Could not change deflater type to " + mode + ": missing " + fieldName);
            }
        }
    }
}
