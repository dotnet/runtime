// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.InteropServices;

namespace TestLibrary
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EndianessChecker
    {
        [FieldOffset(3)]
        byte myByte;
        [FieldOffset(0)]
        int myInt;

        public static bool IsBigEndian()
        {
            EndianessChecker check = new EndianessChecker();

            check.myByte = 1;
            return (check.myInt == 1);
        }

        public static bool IsLittleEndian()
        {
            EndianessChecker check = new EndianessChecker();

            check.myByte = 1;
            return (check.myInt == 16777216);
        }
    }
}
