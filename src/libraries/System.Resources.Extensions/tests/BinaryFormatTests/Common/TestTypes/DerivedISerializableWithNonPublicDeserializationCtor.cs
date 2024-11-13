// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public sealed class DerivedISerializableWithNonPublicDeserializationCtor : BasicISerializableObject
{
    public DerivedISerializableWithNonPublicDeserializationCtor(int value1, string value2) : base(value1, value2) { }
    private DerivedISerializableWithNonPublicDeserializationCtor(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
