// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Resources.Extensions.BinaryFormat;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class FormattedObjectSerializer : ISerializer
{
    public static object Deserialize(
        Stream stream,
        SerializationBinder? binder = null,
        FormatterAssemblyStyle assemblyMatching = FormatterAssemblyStyle.Simple,
        ISurrogateSelector? surrogateSelector = null)
    {
        BinaryFormattedObject format = new(
            stream,
            new()
            {
                Binder = binder,
                SurrogateSelector = surrogateSelector,
                AssemblyMatching = assemblyMatching
            });

        return format.Deserialize();
    }
}
