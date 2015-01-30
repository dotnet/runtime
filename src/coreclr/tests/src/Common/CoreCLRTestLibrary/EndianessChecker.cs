// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
