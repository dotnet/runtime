// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Helper class for dealing with RF detours
/// </summary>
public class DetourHelpers
{
    ReliabilityTestSet _testSet;
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
    static extern void AddTestToNameMapping(int id, [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]string name);

    [DllImport("RFDetours")]
    static extern void InstallDetours();

    [DllImport("RFDetours")]
    static extern void RemoveDetours();

    [DllImport("RFDetours")]
    static extern void SetThreadTest(int testId);

    [DllImport("RFDetours")]
    static extern IntPtr GetThreadTest();
}

