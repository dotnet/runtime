// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class PropertyVisibilityTestsDynamic : PropertyVisibilityTests
    {
        public PropertyVisibilityTestsDynamic() : base(new JsonSerializerWrapperForString_Dynamic()) { }
    }
}
