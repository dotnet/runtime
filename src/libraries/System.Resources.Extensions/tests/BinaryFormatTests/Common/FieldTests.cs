// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;

namespace System.Resources.Extensions.Tests.Common;

public abstract class FieldTests<T> : SerializationTest<T> where T : ISerializer
{
    [Theory]
    [InlineData(FormatterAssemblyStyle.Simple, false)]
    [InlineData(FormatterAssemblyStyle.Full, true)]
    public void MissingField_FailsWithAppropriateStyle(FormatterAssemblyStyle assemblyMatching, bool exceptionExpected)
    {
        Stream stream = Serialize(new Version1ClassWithoutField());

        var binder = new DelegateBinder
        {
            BindToTypeDelegate = (_, _) => typeof(Version2ClassWithoutOptionalField)
        };

        if (exceptionExpected)
        {
            Assert.Throws<SerializationException>(() => Deserialize(stream, binder, assemblyMatching));
        }
        else
        {
            var result = (Version2ClassWithoutOptionalField)Deserialize(stream, binder, assemblyMatching);
            Assert.NotNull(result);
            Assert.Null(result.Value);
        }
    }

    [Theory]
    [InlineData(FormatterAssemblyStyle.Simple)]
    [InlineData(FormatterAssemblyStyle.Full)]
    public void OptionalField_Missing_Success(FormatterAssemblyStyle assemblyMatching)
    {
        Stream stream = Serialize(new Version1ClassWithoutField());

        var binder = new DelegateBinder
        {
            BindToTypeDelegate = (_, _) => typeof(Version2ClassWithOptionalField)
        };

        var result = (Version2ClassWithOptionalField)Deserialize(stream, binder, assemblyMatching);
        Assert.NotNull(result);
        Assert.Null(result.Value);
    }

    [Serializable]
    public class Version1ClassWithoutField
    {
    }

    [Serializable]
    public class Version2ClassWithoutOptionalField
    {
        public object? Value;
    }

    [Serializable]
    public class Version2ClassWithOptionalField
    {
        [OptionalField(VersionAdded = 2)]
        public object? Value;
    }
}
