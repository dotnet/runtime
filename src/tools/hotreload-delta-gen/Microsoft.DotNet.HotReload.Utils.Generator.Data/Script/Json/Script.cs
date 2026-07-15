// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Script.Json;
public class Script {

    [JsonConverter(typeof(ScriptCapabilitiesConverter))]
    public string? Capabilities {get; init;}
        public Change[]? Changes {get; init;}

    [System.Text.Json.Serialization.JsonConstructor]
    public Script (string? capabilities, Change[]? changes) {
        Capabilities = capabilities;
        Changes = changes;
    }

}

public class Change {
    public string Document {get; init;}
    public string Update {get; init;}

    [System.Text.Json.Serialization.JsonConstructor]
    public Change (string document, string update) {
        Document = document;
        Update = update;
    }
}
