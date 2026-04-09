// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public interface IStaticTest
{
    void SetStatic(int val, int val2, int val3, int val4, int val5);
    void GetStatic(out int val1, out int val2, out int val3, out int val4, out int val5);
    void SetStaticObject(object val, object val2, object val3, object val4, object val5);
    void GetStaticObject(out object val1, out object val2, out object val3, out object val4, out object val5);
}