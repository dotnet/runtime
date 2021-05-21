// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Reflection;

namespace System.Text.Json.Nodes
{
    public partial class JsonObject
    {
        private bool TryGetMemberCallback(GetMemberBinder binder, out object? result)
        {
            if (TryGetPropertyValue(binder.Name, out JsonNode? node))
            {
                result = node;
                return true;
            }

            // Return null for missing properties.
            result = null;
            return true;
        }

        private bool TrySetMemberCallback(SetMemberBinder binder, object? value)
        {
            JsonNode? node = null;
            if (value != null)
            {
                node = value as JsonNode;
                if (node == null)
                {
                    node = new JsonValueNotTrimmable<object>(value, Options);
                }
            }

            this[binder.Name] = node;
            return true;
        }

        private static MethodInfo GetMethod(string name) => typeof(JsonObject).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static MethodInfo? s_TryGetMember;
        internal override MethodInfo? TryGetMemberMethodInfo =>
            s_TryGetMember ??
            (s_TryGetMember = GetMethod(nameof(TryGetMemberCallback)));

        private static MethodInfo? s_TrySetMember;
        internal override MethodInfo? TrySetMemberMethodInfo =>
            s_TrySetMember ??
            (s_TrySetMember = GetMethod(nameof(TrySetMemberCallback)));
    }
}
