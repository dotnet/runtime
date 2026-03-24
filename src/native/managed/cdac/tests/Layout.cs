// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal class Layout
{
    public Layout(string name, MockTarget.Architecture architecture, int size, LayoutField[] fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        Name = name;
        Architecture = architecture;
        Size = size;
        Fields = fields;
    }

    public string Name { get; }

    public MockTarget.Architecture Architecture { get; }

    public int Size { get; }

    public LayoutField[] Fields { get; }

    internal LayoutField GetField(string fieldName)
    {
        foreach (LayoutField field in Fields)
        {
            if (field.Name == fieldName)
            {
                return field;
            }
        }

        throw new InvalidOperationException($"Field '{fieldName}' not found.");
    }

}

internal sealed class Layout<TView> : Layout
    where TView : TypedView, new()
{
    public Layout(string name, MockTarget.Architecture architecture, int size, LayoutField[] fields)
        : base(name, architecture, size, fields)
    {
    }

    public TView Create(MockMemorySpace.HeapFragment fragment)
    {
        TView view = new();
        view.Init(fragment.Data.AsMemory(), fragment.Address, this);
        return view;
    }

    public TView Create(Memory<byte> memory, ulong address)
    {
        TView view = new();
        view.Init(memory, address, this);
        return view;
    }
}

internal readonly record struct LayoutField(string Name, int Offset, int Size);

internal sealed class LayoutBuilder
{
    private readonly string _name;
    private readonly MockTarget.Architecture _architecture;
    private readonly Dictionary<string, LayoutField> _fields = new(StringComparer.Ordinal);

    public LayoutBuilder(string name, MockTarget.Architecture architecture)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        _name = name;
        _architecture = architecture;
    }

    public int Size { get; set; }

    public LayoutBuilder AddField(string name, int offset, int size)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        _fields[name] = new LayoutField(name, offset, size);
        return this;
    }

    public Layout Build()
        => new(_name, _architecture, Size, [.. _fields.Values]);

    public Layout<TView> Build<TView>()
        where TView : TypedView, new()
        => new(_name, _architecture, Size, [.. _fields.Values]);
}

internal sealed class SequentialLayoutBuilder
{
    private readonly LayoutBuilder _layoutBuilder;
    private readonly MockTarget.Architecture _architecture;
    private int _currentSize;

    public SequentialLayoutBuilder(string name, MockTarget.Architecture architecture)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _layoutBuilder = new LayoutBuilder(name, architecture);
        _architecture = architecture;
    }

    public SequentialLayoutBuilder AddField(string name, int size)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        int alignment = Math.Min(size, _architecture.Is64Bit ? sizeof(ulong) : sizeof(uint));
        _currentSize = AlignUp(_currentSize, alignment);
        _layoutBuilder.AddField(name, _currentSize, size);
        _currentSize += size;
        _layoutBuilder.Size = _currentSize;
        return this;
    }

    public SequentialLayoutBuilder AddUInt32Field(string name)
        => AddField(name, sizeof(uint));

    public SequentialLayoutBuilder AddUInt64Field(string name)
        => AddField(name, sizeof(ulong));

    public SequentialLayoutBuilder AddNUIntField(string name)
        => AddField(name, _architecture.Is64Bit ? sizeof(ulong) : sizeof(uint));

    public SequentialLayoutBuilder AddPointerField(string name)
        => AddField(name, _architecture.Is64Bit ? sizeof(ulong) : sizeof(uint));

    public Layout Build()
        => _layoutBuilder.Build();

    public Layout<TView> Build<TView>()
        where TView : TypedView, new()
        => _layoutBuilder.Build<TView>();

    private static int AlignUp(int value, int alignment)
        => (value + alignment - 1) & ~(alignment - 1);
}
