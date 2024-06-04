// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        public static System.Text.Json.Serialization.JsonConverter<System.DateOnly> DateOnlyConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Half> HalfConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.TimeOnly> TimeOnlyConverter { get { throw null; } }

#if NET
        public static System.Text.Json.Serialization.JsonConverter<System.Int128> Int128Converter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<System.UInt128> UInt128Converter { get { throw null; } }
#endif
    }
}
