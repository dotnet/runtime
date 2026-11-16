// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SuperPmi;

// What a compilation produced: code, GC info, relocations, and so on.
public sealed partial class CompileResult
{
    internal static readonly MapInfo[] s_mapByPacket;

    static CompileResult()
    {
        s_mapByPacket = MethodContext.BuildPacketTable(s_maps);
    }

    internal LightWeightMapBuffer[] Maps => _maps;

    internal void SetMap(int index, LightWeightMapBuffer map) => _maps[index] = map;

    public bool IsEmpty() => Array.TrueForAll(_maps, map => map == null);

    public string repProcessName()
    {
        if (ProcessName == null)
            return "hackishProcessName";

        if (ProcessName.GetCount() > 0)
            return ProcessName.GetString(ProcessName.Get(0));
        return null;
    }
}
