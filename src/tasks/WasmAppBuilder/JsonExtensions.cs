// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Text.Json.Nodes;

internal static class JsonExtensions
{
    public static T GetOrCreate<T>(this JsonObject json, string name, Func<JsonNode> creator) where T : JsonNode
    {
        if (json.TryGetPropertyValue(name, out JsonNode? node) && (node is T found))
            return found;

        JsonNode newObject = creator();
        if (newObject == null)
            throw new ArgumentNullException($"BUG: got a null object for {name}");

        json.Add(name, newObject);
        return (T)newObject;
    }
}
