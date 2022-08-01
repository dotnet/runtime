// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal static partial class JsonConstants
    {
        // The maximum number of parameters a constructor can have where it can be supported by the serializer.
        public const int MaxParameterCount = 64;

        // Standard format for double and single on non-inbox frameworks.
        public const string DoubleFormatString = "G17";
        public const string SingleFormatString = "G9";
    }
}
