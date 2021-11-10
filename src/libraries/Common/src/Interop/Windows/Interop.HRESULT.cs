// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    // https://msdn.microsoft.com/en-us/library/cc231198.aspx
    internal enum HRESULT : int
    {
        S_OK = 0,
        S_FALSE = 1,
        E_NOTIMPL = unchecked((int)0x80004001),
        E_ABORT = unchecked((int)0x80004004),
        E_FAIL = unchecked((int)0x80004005),
        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001L),
        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
        E_ACCESSDENIED = unchecked((int)0x80070005),
        E_INVALIDARG = unchecked((int)0x80070057),
    }
}
