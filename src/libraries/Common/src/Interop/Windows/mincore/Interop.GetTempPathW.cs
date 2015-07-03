// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1_2, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern uint GetTempPathW(int bufferLen, [Out]StringBuilder buffer);
    }
}
