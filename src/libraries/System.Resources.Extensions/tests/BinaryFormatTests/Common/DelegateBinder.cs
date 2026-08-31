// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common;

internal class DelegateBinder : SerializationBinder
{
    public Func<string, string, Type>? BindToTypeDelegate;
    public override Type? BindToType(string assemblyName, string typeName) => BindToTypeDelegate?.Invoke(assemblyName, typeName);
}
