// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private static int Main()
    {
        int returnVal = Pass;
        
        if (!CheckVector(Vector.Ceiling(new Vector<float>(4.6f)), 5)) returnVal = Fail;
        if (!CheckVector(Vector.Ceiling(new Vector<float>(-4.6f)), -4)) returnVal = Fail;
        if (!CheckVector(Vector.Floor(new Vector<float>(4.6f)), 4)) returnVal = Fail;
        if (!CheckVector(Vector.Floor(new Vector<float>(-4.6f)), -5)) returnVal = Fail;

        if (!CheckVector(Vector.Ceiling(new Vector<double>(4.6)), 5)) returnVal = Fail;
        if (!CheckVector(Vector.Ceiling(new Vector<double>(-4.6)), -4)) returnVal = Fail;
        if (!CheckVector(Vector.Floor(new Vector<double>(4.6)), 4)) returnVal = Fail;
        if (!CheckVector(Vector.Floor(new Vector<double>(-4.6)), -5)) returnVal = Fail;

        return returnVal;
    }
}
