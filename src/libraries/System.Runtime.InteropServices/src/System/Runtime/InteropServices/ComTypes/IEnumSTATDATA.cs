// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    [CLSCompliant(false)]
    public interface IEnumSTATDATA
    {
        void Clone(out IEnumSTATDATA newEnum);
        int Next(int celt, STATDATA[] rgelt, int[] pceltFetched);
        int Reset();
        int Skip(int celt);
    }
}
