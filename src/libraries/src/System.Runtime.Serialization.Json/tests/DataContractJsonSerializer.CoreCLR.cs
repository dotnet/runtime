// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes.CoreCLR;
using System;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

public static partial class DataContractJsonSerializerTests
{
    [Fact]
    public static void DCJS_DifferentCollectionsOfSameTypeAsKnownTypes()
    {
        Assert.Throws<InvalidOperationException>(() => {
            (new DataContractSerializer(typeof(TypeWithKnownTypesOfCollectionsWithConflictingXmlName))).WriteObject(new MemoryStream(), new TypeWithKnownTypesOfCollectionsWithConflictingXmlName());
        });
    }
}
