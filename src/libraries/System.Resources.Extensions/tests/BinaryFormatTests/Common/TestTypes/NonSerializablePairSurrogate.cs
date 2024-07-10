// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

internal sealed class NonSerializablePairSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
    {
        var pair = (NonSerializablePair<int, string>)obj;
        info.AddValue("Value1", pair.Value1);
        info.AddValue("Value2", pair.Value2);
    }

    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector)
    {
        var pair = (NonSerializablePair<int, string>)obj;
        pair.Value1 = info.GetInt32("Value1");
        pair.Value2 = info.GetString("Value2")!;
        return pair;
    }
}
