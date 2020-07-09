// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.Formatters
{
    public interface IFieldInfo
    {
        string[]? FieldNames { get; set; }
        Type[]? FieldTypes { get; set; }
    }
}
