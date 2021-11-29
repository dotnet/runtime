// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Helper class for dealing with RF detours
/// </summary>
public class DetourHelpers
{
    private ReliabilityTestSet _testSet;
    public void Initialize(ReliabilityTestSet testSet)
    {
        _testSet = testSet;
        InstallDetours();
    }

    public void Uninitialize()
    {
        RemoveDetours();
    }

    public void SetThreadTestId(int index)
    {
        SetThreadTest(index);
    }

    public int GetThreadTestId()
    {
        return ((int)GetThreadTest());
    }

    public void SetTestIdName(int id, string name)
    {
        AddTestToNameMapping(id, name);
    }

    [DllImport("RFDetours")]
    private static extern void AddTestToNameMapping(int id, [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string name);

    [DllImport("RFDetours")]
    private static extern void InstallDetours();

    [DllImport("RFDetours")]
    private static extern void RemoveDetours();

    [DllImport("RFDetours")]
    private static extern void SetThreadTest(int testId);

    [DllImport("RFDetours")]
    private static extern IntPtr GetThreadTest();
}

