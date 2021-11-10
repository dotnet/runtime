// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static int _global = 0;
    private static int _counter = 100;

    public static int Main()
    {
        TestCastingSingleToSByte();
        TestCastingSingleToByte();
        TestCastingSingleToInt16();
        TestCastingSingleToUInt16();
        TestCastingSingleToInt32();
        TestCastingSingleToUInt32();
        TestCastingSingleToInt64();
        TestCastingSingleToUInt64();

        TestCastingDoubleToSByte();
        TestCastingDoubleToByte();
        TestCastingDoubleToInt16();
        TestCastingDoubleToUInt16();
        TestCastingDoubleToInt32();
        TestCastingDoubleToUInt32();
        TestCastingDoubleToInt64();
        TestCastingDoubleToUInt64();

        TestCastingSByteToSingle();
        TestCastingSByteToDouble();
        TestCastingSByteToSByte();
        TestCastingSByteToByte();
        TestCastingSByteToInt16();
        TestCastingSByteToUInt16();
        TestCastingSByteToInt32();
        TestCastingSByteToUInt32();
        TestCastingSByteToInt64();
        TestCastingSByteToUInt64();
        TestCastingByteToSingle();
        TestCastingByteToDouble();
        TestCastingByteToSByte();
        TestCastingByteToByte();
        TestCastingByteToInt16();
        TestCastingByteToUInt16();
        TestCastingByteToInt32();
        TestCastingByteToUInt32();
        TestCastingByteToInt64();
        TestCastingByteToUInt64();

        TestCastingInt16ToSingle();
        TestCastingInt16ToDouble();
        TestCastingInt16ToSByte();
        TestCastingInt16ToByte();
        TestCastingInt16ToInt16();
        TestCastingInt16ToUInt16();
        TestCastingInt16ToInt32();
        TestCastingInt16ToUInt32();
        TestCastingInt16ToInt64();
        TestCastingInt16ToUInt64();
        TestCastingUInt16ToSingle();
        TestCastingUInt16ToDouble();
        TestCastingUInt16ToSByte();
        TestCastingUInt16ToByte();
        TestCastingUInt16ToInt16();
        TestCastingUInt16ToUInt16();
        TestCastingUInt16ToInt32();
        TestCastingUInt16ToUInt32();
        TestCastingUInt16ToInt64();
        TestCastingUInt16ToUInt64();

        TestCastingInt32ToSingle();
        TestCastingInt32ToDouble();
        TestCastingInt32ToSByte();
        TestCastingInt32ToByte();
        TestCastingInt32ToInt16();
        TestCastingInt32ToUInt16();
        TestCastingInt32ToInt32();
        TestCastingInt32ToUInt32();
        TestCastingInt32ToInt64();
        TestCastingInt32ToUInt64();
        TestCastingUInt32ToSingle();
        TestCastingUInt32ToDouble();
        TestCastingUInt32ToSByte();
        TestCastingUInt32ToByte();
        TestCastingUInt32ToInt16();
        TestCastingUInt32ToUInt16();
        TestCastingUInt32ToInt32();
        TestCastingUInt32ToUInt32();
        TestCastingUInt32ToInt64();
        TestCastingUInt32ToUInt64();

        TestCastingInt64ToSingle();
        TestCastingInt64ToDouble();
        TestCastingInt64ToSByte();
        TestCastingInt64ToByte();
        TestCastingInt64ToInt16();
        TestCastingInt64ToUInt16();
        TestCastingInt64ToInt32();
        TestCastingInt64ToUInt32();
        TestCastingInt64ToInt64();
        TestCastingInt64ToUInt64();
        TestCastingUInt64ToSingle();
        TestCastingUInt64ToDouble();
        TestCastingUInt64ToSByte();
        TestCastingUInt64ToByte();
        TestCastingUInt64ToInt16();
        TestCastingUInt64ToUInt16();
        TestCastingUInt64ToInt32();
        TestCastingUInt64ToUInt32();
        TestCastingUInt64ToInt64();
        TestCastingUInt64ToUInt64();

        return _counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BreakUpFlow() => false;
}
