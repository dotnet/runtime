// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class InternalUseFieldName
{
    public static InternalUseFieldName Hidden = new(nameof(Hidden));
    public static InternalUseFieldName State = new(nameof(State));
    public static InternalUseFieldName Section = new(nameof(Section));
    public static InternalUseFieldName Owner = new(nameof(Owner));
    public static InternalUseFieldName IsStatic = new(nameof(IsStatic));
    public static InternalUseFieldName IsNewSlot = new(nameof(IsNewSlot));
    public static InternalUseFieldName IsBackingField = new(nameof(IsBackingField));
    public static InternalUseFieldName ParentTypeId = new(nameof(ParentTypeId));

    private static readonly HashSet<string> s_names = new()
    {
        Hidden.Name,
        State.Name,
        Section.Name,
        Owner.Name,
        IsStatic.Name,
        IsNewSlot.Name,
        IsBackingField.Name,
        ParentTypeId.Name
    };

    private InternalUseFieldName(string fieldName) => Name = $"__{fieldName}__";

    public static int Count => s_names.Count;
    public static bool IsKnown(string name) => !string.IsNullOrEmpty(name) && s_names.Contains(name);
    public string Name { get; init; }
}
