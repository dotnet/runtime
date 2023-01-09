// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal static partial class JsonConstants
    {
        // Standard format for double and single on non-inbox frameworks.
        public const string DoubleFormatString = "G17";
        public const string SingleFormatString = "G9";

        public const int StackallocByteThreshold = 256;
        public const int StackallocCharThreshold = StackallocByteThreshold / 2;
    }
}
