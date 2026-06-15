// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library;

public record Person(string FirstName, string LastName);

[JsonSerializable(typeof(Person))]
public partial class PersonJsonSerializerContext : JsonSerializerContext
{
}
