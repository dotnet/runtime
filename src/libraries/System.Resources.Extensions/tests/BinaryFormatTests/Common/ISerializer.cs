// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Resources.Extensions.Tests.Common;

public interface ISerializer
{
    static virtual Stream Serialize(
        object value,
        SerializationBinder? binder = null,
        ISurrogateSelector? surrogateSelector = null,
        FormatterTypeStyle typeStyle = FormatterTypeStyle.TypesAlways)
    {
        MemoryStream stream = new();
        BinaryFormatter formatter = new()
        {
            SurrogateSelector = surrogateSelector,
            TypeFormat = typeStyle,
            Binder = binder
        };

        formatter.Serialize(stream, value);
        stream.Position = 0;
        return stream;
    }

    static abstract object Deserialize(
        Stream stream,
        SerializationBinder? binder = null,
        FormatterAssemblyStyle assemblyMatching = FormatterAssemblyStyle.Simple,
        ISurrogateSelector? surrogateSelector = null);
}
