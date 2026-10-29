// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILAssembler;

public sealed class SourceText(string text, string path)
{
    public string Text { get; } = text;

    public string Path { get; } = path;

    private int[] LineStarts => field ??= ParseLineStarts(Text);

    internal (int Line, int Column) GetLinePosition(int position)
    {
        position = Math.Clamp(position, 0, Text.Length);
        int[] lineStarts = LineStarts;
        int line = Array.BinarySearch(lineStarts, position);
        if (line < 0)
        {
            line = ~line - 1;
        }

        return (line, position - lineStarts[line]);
    }

    private static int[] ParseLineStarts(string text)
    {
        List<int> starts = [0];
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }
}
