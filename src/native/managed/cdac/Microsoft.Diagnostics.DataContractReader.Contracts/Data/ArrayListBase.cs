// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Encapsulates structure and logic for ArrayListBase implemented in arraylist.h
/// </summary>
[CdacType(nameof(DataType.ArrayListBase))]
internal sealed partial class ArrayListBase : IData<ArrayListBase>
{
    [Field] public partial uint Count { get; }

    [FieldAddress]
    public partial TargetPointer FirstBlock { get; }

    [CustomInit(nameof(InitBlocks))] public partial IReadOnlyList<ArrayListBlock> Blocks { get; }
    [CustomInit(nameof(InitElements))] public partial IReadOnlyList<TargetPointer> Elements { get; }

    private partial IReadOnlyList<ArrayListBlock> InitBlocks(Target target, TargetPointer address)
    {
        List<ArrayListBlock> blocks = [];
        TargetPointer next = FirstBlock;
        while (next != TargetPointer.Null)
        {
            ArrayListBlock block = target.ProcessedData.GetOrAdd<ArrayListBlock>(next);
            blocks.Add(block);
            next = block.Next;
        }

        return blocks;
    }

    private partial IReadOnlyList<TargetPointer> InitElements(Target target, TargetPointer address)
    {
        List<TargetPointer> elements = [];
        uint elementsFound = 0;
        foreach (ArrayListBlock block in Blocks)
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

        return elements;
    }
}

[CdacType(nameof(DataType.ArrayListBlock))]
internal sealed partial class ArrayListBlock : IData<ArrayListBlock>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial uint Size { get; }

    [FieldAddress]
    public partial TargetPointer ArrayStart { get; }

    [CustomInit(nameof(InitElements))] public partial IReadOnlyList<TargetPointer> Elements { get; }

    private partial IReadOnlyList<TargetPointer> InitElements(Target target, TargetPointer address)
    {
        List<TargetPointer> elements = new((int)Size);
        for (ulong i = 0; i < Size; i++)
        {
            elements.Add(target.ReadPointer(ArrayStart + (i * (ulong)target.PointerSize)));
        }

        return elements;
    }
}
