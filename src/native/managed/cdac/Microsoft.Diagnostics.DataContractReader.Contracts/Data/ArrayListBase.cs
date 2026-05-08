// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Encapsulates structure and logic for ArrayListBase implemented in arraylist.h
/// </summary>
internal sealed class ArrayListBase : IData<ArrayListBase>
{
    static ArrayListBase IData<ArrayListBase>.Create(Target target, TargetPointer address) => new ArrayListBase(target, address);
    public ArrayListBase(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ArrayListBase);

        Count = target.ReadField<uint>(address, type, nameof(Count));
        FirstBlock = address + (ulong)type.Fields[nameof(FirstBlock)].Offset;

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

    public uint Count { get; init; }
    public TargetPointer FirstBlock { get; init; }

    public List<ArrayListBlock> Blocks { get; init; } = [];
    public List<TargetPointer> Elements { get; init; } = [];

    internal sealed class ArrayListBlock : IData<ArrayListBlock>
    {
        static ArrayListBlock IData<ArrayListBlock>.Create(Target target, TargetPointer address) => new ArrayListBlock(target, address);
        public ArrayListBlock(Target target, TargetPointer address)
        {
            Target.TypeInfo type = target.GetTypeInfo(DataType.ArrayListBlock);

            Next = target.ReadPointerField(address, type, nameof(Next));
            Size = target.ReadField<uint>(address, type, nameof(Size));
            ArrayStart = address + (ulong)type.Fields[nameof(ArrayStart)].Offset;

            for (ulong i = 0; i < Size; i++)
            {
                Elements.Add(target.ReadPointer(ArrayStart + (i * (ulong)target.PointerSize)));
            }
        }

        public TargetPointer Next { get; init; }
        public uint Size { get; init; }
        public TargetPointer ArrayStart { get; init; }

        public List<TargetPointer> Elements { get; init; } = [];
    }
}
