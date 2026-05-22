// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Encapsulates structure and logic for ArrayListBase implemented in arraylist.h
/// </summary>
[CdacType(nameof(DataType.ArrayListBase))]
internal sealed partial class ArrayListBase : IData<ArrayListBase>
{
    [Field] public uint Count { get; }

    [FieldAddress]
    public TargetPointer FirstBlock { get; }

    public IReadOnlyList<ArrayListBlock> Blocks { get; private set; } = [];
    public IReadOnlyList<TargetPointer> Elements { get; private set; } = [];

    [MemberNotNull(nameof(Blocks), nameof(Elements))]
    partial void OnInit(Target target, TargetPointer address)
    {
        List<ArrayListBlock> blocks = [];
        TargetPointer next = FirstBlock;
        while (next != TargetPointer.Null)
        {
            ArrayListBlock block = target.ProcessedData.GetOrAdd<ArrayListBlock>(next);
            blocks.Add(block);
            next = block.Next;
        }

        List<TargetPointer> elements = [];
        uint elementsFound = 0;
        foreach (ArrayListBlock block in blocks)
        {
            foreach (TargetPointer element in block.Elements)
            {
                if (elementsFound >= Count)
                {
                    break;
                }

                elements.Add(element);
                elementsFound++;
            }
        }

        Blocks = blocks;
        Elements = elements;
    }
}

[CdacType(nameof(DataType.ArrayListBlock))]
internal sealed partial class ArrayListBlock : IData<ArrayListBlock>
{
    [Field] public TargetPointer Next { get; }
    [Field] public uint Size { get; }

    [FieldAddress]
    public TargetPointer ArrayStart { get; }

    public IReadOnlyList<TargetPointer> Elements { get; private set; } = [];

    [MemberNotNull(nameof(Elements))]
    partial void OnInit(Target target, TargetPointer address)
    {
        List<TargetPointer> elements = new((int)Size);
        for (ulong i = 0; i < Size; i++)
        {
            elements.Add(target.ReadPointer(ArrayStart + (i * (ulong)target.PointerSize)));
        }

        Elements = elements;
    }
}
