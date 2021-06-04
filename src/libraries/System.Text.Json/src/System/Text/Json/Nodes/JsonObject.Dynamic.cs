// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
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

        private const BindingFlags MemberInfoBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static MethodInfo? s_TryGetMember;
        internal override MethodInfo? TryGetMemberMethodInfo =>
            s_TryGetMember ??= typeof(JsonObject).GetMethod(nameof(TryGetMemberCallback), MemberInfoBindingFlags);

        private static MethodInfo? s_TrySetMember;
        internal override MethodInfo? TrySetMemberMethodInfo
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            get => s_TrySetMember ??= typeof(JsonObject).GetMethod(nameof(TrySetMemberCallback), MemberInfoBindingFlags);
        }
    }
}
