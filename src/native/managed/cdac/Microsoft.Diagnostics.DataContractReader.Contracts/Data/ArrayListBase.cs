// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Generated;

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

    public List<ArrayListBlock> Blocks { get; } = [];
    public List<TargetPointer> Elements { get; } = [];

    partial void OnInit(Target target, TargetPointer address)
    {
        TargetPointer next = FirstBlock;
        while (next != TargetPointer.Null)
        {
            ArrayListBlock block = target.ProcessedData.GetOrAdd<ArrayListBlock>(next);
            Blocks.Add(block);
            next = block.Next;
        }

        uint elementsFound = 0;
        foreach (ArrayListBlock block in Blocks)
        {
            foreach (TargetPointer element in block.Elements)
            {
                if (elementsFound >= Count)
                {
                    break;
                }

                Elements.Add(element);
                elementsFound++;
            }
        }
    }
}

[CdacType(nameof(DataType.ArrayListBlock))]
internal sealed partial class ArrayListBlock : IData<ArrayListBlock>
{
    [Field] public TargetPointer Next { get; }
    [Field] public uint Size { get; }

    [FieldAddress]
    public TargetPointer ArrayStart { get; }

    public List<TargetPointer> Elements { get; } = [];

    partial void OnInit(Target target, TargetPointer address)
    {
        for (ulong i = 0; i < Size; i++)
        {
            Elements.Add(target.ReadPointer(ArrayStart + (i * (ulong)target.PointerSize)));
        }
    }
}
