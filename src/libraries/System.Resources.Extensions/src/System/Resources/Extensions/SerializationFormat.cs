// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Resources.Extensions
{
    // Internal Enum that's shared between reader and writer to indicate the 
    // deserialization method for a resource.
    internal enum SerializationFormat
    {
        BinaryFormatter = 1,
        TypeConverterByteArray = 2,
        TypeConverterString = 3,
        ActivatorStream = 4
    }
}
