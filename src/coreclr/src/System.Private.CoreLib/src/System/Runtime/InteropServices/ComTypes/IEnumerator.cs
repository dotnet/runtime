// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.ComTypes
{
    [Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
    internal interface IEnumerator
    {
        bool MoveNext();
        object Current { get; }
        void Reset();
    }
}
