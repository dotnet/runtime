// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    [CLSCompliant(false)]
    public interface IDataObject
    {
        int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection);
        void DUnadvise(int connection);
        int EnumDAdvise(out IEnumSTATDATA enumAdvise);
        IEnumFORMATETC EnumFormatEtc(DATADIR direction);
        int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut);
        void GetData(ref FORMATETC format, out STGMEDIUM medium);
        void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium);
        int QueryGetData(ref FORMATETC format);
        void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release);
    }
}
