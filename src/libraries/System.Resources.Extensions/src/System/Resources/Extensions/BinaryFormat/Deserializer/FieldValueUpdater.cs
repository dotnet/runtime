// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

internal sealed class FieldValueUpdater : ValueUpdater
{
    private readonly FieldInfo _field;

    internal FieldValueUpdater(int objectId, int valueId, FieldInfo field) : base(objectId, valueId)
    {
        _field = field;
    }

    internal override void UpdateValue(IDictionary<int, object> objects)
    {
        object newValue = objects[ValueId];
        _field.SetValue(objects[ObjectId], newValue);
    }
}
