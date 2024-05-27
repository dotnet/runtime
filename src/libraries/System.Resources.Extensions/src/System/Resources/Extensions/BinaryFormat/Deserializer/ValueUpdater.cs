// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

internal abstract class ValueUpdater
{
    /// <summary>
    ///  The value id that needs to be reapplied.
    /// </summary>
    internal int ValueId { get; }

    /// <summary>
    ///  The object id that is dependent on <see cref="ValueId"/>.
    /// </summary>
    internal int ObjectId { get; }

    private protected ValueUpdater(int objectId, int valueId)
    {
        ObjectId = objectId;
        ValueId = valueId;
    }

    internal abstract void UpdateValue(IDictionary<int, object> objects);
}
