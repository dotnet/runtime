// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Server.Contract;

[ComVisible(true)]
[Guid(Server.Contract.Guids.DispatchTesting)]
public class DispatchTesting : Server.Contract.IDispatchTesting
{
    public void DoubleNumeric_ReturnByRef (
        byte b1,
        ref byte b2,
        short s1,
        ref short s2,
        ushort us1,
        ref ushort us2,
        int i1,
        ref int i2,
        uint ui1,
        ref uint ui2,
        long l1,
        ref long l2,
        ulong ul1,
        ref ulong ul2)
    {
        b2 = (byte)(b1 * 2);
        s2 = (short)(s1 * 2);
        us2 = (ushort)(us1 * 2);
        i2 = i1 * 2;
        ui2 = ui1 * 2;
        l2 = l1 * 2;
        ul2 = ul1 * 2;
    }

    public float Add_Float_ReturnAndUpdateByRef(float a, ref float b)
    {
        float sum = a + b;
        b = sum;
        return sum;
    }
    public double Add_Double_ReturnAndUpdateByRef(double a, ref double b)
    {
        double sum = a + b;
        b = sum;
        return sum;
    }

    public void TriggerException(IDispatchTesting_Exception excep, int errorCode)
    {
        switch (excep)
        {
        case IDispatchTesting_Exception.Disp:
            throw new Exception();
        case IDispatchTesting_Exception.HResult:
        case IDispatchTesting_Exception.Int:
            throw new System.ComponentModel.Win32Exception(errorCode);
        }
    }

    // Special cases
    public HFA_4 DoubleHVAValues(ref HFA_4 input)
    {
        input.x *= 2;
        input.y *= 2;
        input.z *= 2;
        input.w *= 2;
        return input;
    }

    [LCIDConversion(0)]
    public int PassThroughLCID()
    {
        return CultureInfo.CurrentCulture.LCID;
    }

    public System.Collections.IEnumerator ExplicitGetEnumerator()
    {
        return Enumerable.Range(0, 10).ToList().GetEnumerator();
    }

    [DispId(/*DISPID_NEWENUM*/-4)]
    public System.Collections.IEnumerator GetEnumerator()
    {
        return Enumerable.Range(0, 10).ToList().GetEnumerator();
    }
}
