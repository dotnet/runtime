// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

internal sealed class ArrayUpdater : ValueUpdater
{
    private readonly int[] _indices;

    internal ArrayUpdater(int objectId, int valueId, int[] indices) : base(objectId, valueId)
    {
        _indices = indices;
    }

    internal override void UpdateValue(IDictionary<int, object> objects)
    {
        object value = objects[ValueId];
        Array array = (Array)objects[ObjectId];
        array.SetValue(value, _indices);
    }
}
