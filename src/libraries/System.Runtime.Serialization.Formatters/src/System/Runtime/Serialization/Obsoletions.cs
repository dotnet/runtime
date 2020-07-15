// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    internal static class Obsoletions
    {
        internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

        internal const string InsecureSerializationMessage = "BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.";
        internal const string InsecureSerializationDiagId = "MSLIB0003";
    }
}
