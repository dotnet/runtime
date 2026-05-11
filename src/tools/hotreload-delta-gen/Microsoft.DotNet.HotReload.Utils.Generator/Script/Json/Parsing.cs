// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using CancellationToken = System.Threading.CancellationToken;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Script.Json;

/// Read a diff script from a json file
public class Parser {

    public readonly string Path;
    private readonly string _absDir;
    public Parser (string path) {
        Path = path;
        _absDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!;
    }
    public async ValueTask<Script?> ReadRawAsync (Stream stream, CancellationToken ct = default) {
        var options = new JsonSerializerOptions {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        try {
            var result = await JsonSerializer.DeserializeAsync<Script>(stream, options: options, cancellationToken: ct);
            return result;
        } catch (JsonException exn) {
            throw new DiffyException($"error parsing diff script '{Path}'", exn, exitStatus: 15);
        }
    }


    private string AbsPath (string relativePath) {
        return System.IO.Path.GetFullPath(relativePath, _absDir);
    }
    public async ValueTask<ParsedScript> ReadAsync (Stream stream, CancellationToken ct = default) {
        var script = await ReadRawAsync(stream, ct);
        if (script == null)
            return ParsedScript.Empty;

        Plan.Change<string,string>[] changes;
        if (script.Changes == null)
            changes = Array.Empty<Plan.Change<string,string>>();
        else
            changes = script.Changes.Select(c => Plan.Change.Create(AbsPath(c.Document), AbsPath(c.Update))).ToArray();
        EnC.EditAndContinueCapabilities? caps = null;
        IEnumerable<string> unknowns = Array.Empty<string>();
        if (script.Capabilities != null) {
            IEnumerable<EnC.EditAndContinueCapabilities> goodCaps;
            (goodCaps, unknowns) = EditAndContinueCapabilitiesParser.Parse(script.Capabilities);
            var totalCaps = EnC.EditAndContinueCapabilities.None;
            foreach (var cap in goodCaps) {
                totalCaps |= cap;
            }
            caps = totalCaps;
        }
        return ParsedScript.Make(changes, caps, unknowns);
    }
}
