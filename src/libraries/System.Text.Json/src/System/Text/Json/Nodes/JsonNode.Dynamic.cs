// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Text.Json.Nodes
{
    public partial class JsonNode : IDynamicMetaObjectProvider
    {
        internal virtual MethodInfo? TryGetMemberMethodInfo => null;
        internal virtual MethodInfo? TrySetMemberMethodInfo
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            get => null;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            CreateDynamicObject(parameter, this);

        [RequiresUnreferencedCode("Using JsonNode instances as dynamic types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.")]
        private static DynamicMetaObject CreateDynamicObject(Expression parameter, JsonNode node) =>
            new MetaDynamic(parameter, node);
    }
}
