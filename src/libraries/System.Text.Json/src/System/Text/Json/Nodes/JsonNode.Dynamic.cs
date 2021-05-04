// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Text.Json.Nodes
{
    public partial class JsonNode : IDynamicMetaObjectProvider
    {
        internal virtual MethodInfo? TryGetMemberMethodInfo => null;
        internal virtual MethodInfo? TrySetMemberMethodInfo => null;

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            new MetaDynamic(parameter, this);
    }
}
