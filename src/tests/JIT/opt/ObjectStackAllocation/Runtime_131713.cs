// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Xunit;
using TestLibrary;

public class Runtime_131713
{
    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static void Test()
    {
        // The bug needs the serializer's methods to reach tier-1 with PGO data, so give
        // the background compilations time to land in between calls.
        for (int i = 0; i < 100; i++)
        {
            Serialize();
            Thread.Sleep(15);
        }
    }

    private static void Serialize()
    {
        Foo model = new()
        {
            Foos =
            [
                new Foo
                {
                    Bars = [new Bar()]
                }
            ]
        };

        XmlSerializer serializer = new(typeof(Foo));
        using StringWriter stringWriter = new();
        using XmlWriter xmlWriter = XmlWriter.Create(stringWriter);
        serializer.Serialize(xmlWriter, model);
    }
}

public class Foo
{
    public CustomList<Bar> Bars { get; set; } = [];

    public CustomList<Foo> Foos { get; set; } = [];
}

public class Bar
{
}

public class CustomList<T> : IEnumerable<T>
{
    private readonly List<T> _innerList = [];

    public void Add(T item) => _innerList.Add(item);

    public IEnumerator<T> GetEnumerator() => _innerList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _innerList.GetEnumerator();
}
