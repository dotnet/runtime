// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;


namespace Microsoft.DotNet.HotReload.Utils.Generator;

public class UpdateHandlerInfo {

    [JsonPropertyName("updatedTypes")]
    public ImmutableArray<int> UpdatedTypes {get; init;}

    public UpdateHandlerInfo(ImmutableArray<int> updatedTypes) {
        UpdatedTypes = updatedTypes;
    }
}
