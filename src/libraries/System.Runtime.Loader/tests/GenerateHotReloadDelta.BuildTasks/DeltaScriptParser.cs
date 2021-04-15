using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DeltaScriptParser
{
    public static async Task<Script> Parse(string scriptPath, CancellationToken ct = default)
    {
        using var stream = System.IO.File.OpenRead(scriptPath);
        var options = new JsonSerializerOptions {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        var json = await JsonSerializer.DeserializeAsync<Script>(stream, options: options, cancellationToken: ct);
        return json;
    }

    public class Script {
        [JsonPropertyName("changes")]
        public Change[] Changes {get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

    }


    public class Change {
        [JsonPropertyName("document")]
        public string Document {get; set;}
        [JsonPropertyName("update")]
        public string Update {get; set;}

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
