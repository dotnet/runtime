// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class InternalUseFieldName
{
    public static string Hidden => "__hidden__";
    public static string State => "__state__";
    public static string Section => "__section__";
    public static string Owner => "__owner__";
    public static string IsStatic => "__isStatic__";
    public static string IsNewSlot => "__isNewSlot__";
    public static string IsBackingField => "__isBackingField__";
    public static string ParentTypeId => "__parentTypeId__";

    private static readonly HashSet<string> s_names = new()
    {
        Hidden,
        State,
        Section,
        Owner,
        IsStatic,
        IsNewSlot,
        IsBackingField,
        ParentTypeId
    };

    public static int Count => s_names.Count;
    public static bool IsKnown(string name) => !string.IsNullOrEmpty(name) && s_names.Contains(name);
}
