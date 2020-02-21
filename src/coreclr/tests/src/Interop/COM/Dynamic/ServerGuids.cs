// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Dynamic
{
    /// <summary>
    /// CLSIDs for COM servers used for dynamic binding
    /// </summary>
    internal static class ServerGuids
    {
        public static readonly Guid BasicTest = Guid.Parse("ED349A5B-257B-4349-8CB8-2C30B0C05FC3");
        public static readonly Guid CollectionTest = Guid.Parse("1FFF64AE-FF9C-41AB-BF80-3ECA831AEC40");
    }
}
