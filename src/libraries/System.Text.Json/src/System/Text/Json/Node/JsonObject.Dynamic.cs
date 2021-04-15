// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Reflection;

namespace System.Text.Json.Node
{
    public partial class JsonObject
    {
        internal bool TryGetMemberCallback(GetMemberBinder binder, out object? result)
        {
            if (Dictionary.TryGetValue(binder.Name, out JsonNode? node))
            {
                result = node;
                return true;
            }

            // Return null for missing properties.
            result = null;
            return true;
        }

        internal bool TrySetMemberCallback(SetMemberBinder binder, object? value)
        {
            JsonNode? node = null;
            if (value != null)
            {
                node = value as JsonNode;
                if (node == null)
                {
                    node = new JsonValue<object>(value, Options);
                }
            }

            Dictionary[binder.Name] = node;
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
