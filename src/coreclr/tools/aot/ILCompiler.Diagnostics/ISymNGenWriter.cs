// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security;
using System.Text;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// IUnknown COM type for writing NGen PDBs
    /// </summary>
    /// <remarks>
    /// <code>
    /// [
    ///  object,
    ///  uuid(d682fd12-43de-411c-811b-be8404cea126),
    ///  pointer_default(unique)
    /// ]
    /// interface ISymNGenWriter : IUnknown
    /// {
    ///     /*
    ///      * Add a new public symbol to the NGEN PDB.
    ///      */
    ///     HRESULT AddSymbol([in] BSTR pSymbol,
    ///                       [in] USHORT iSection,
    ///                       [in] ULONGLONG rva);
    ///
    ///     /*
    ///      * Adds a new section to the NGEN PDB.
    ///      */
    ///     HRESULT AddSection([in] USHORT iSection,
    ///                        [in] USHORT flags,
    ///                        [in] long offset,
    ///                        [in] long cb);
    /// };
    /// </code>
    /// </remarks>
    [GeneratedComInterface]
    [Guid("D682FD12-43dE-411C-811B-BE8404CEA126")]
    internal partial interface ISymNGenWriter
    {
        // Add a new public symbol to the NGEN PDB.
        void AddSymbol([MarshalAs(UnmanagedType.BStr)] string pSymbol,
                        ushort iSection,
                        ulong rva);

        // Adds a new section to the NGEN PDB.
        void AddSection(ushort iSection,
                        OMF flags,
                        int offset,
                        int cb);
    }

    [Flags]
    internal enum OMF : ushort
    {
        Const_Read =            0x0001,
        Const_Write =          0x0002,
        Const_Exec =           0x0004,
        Const_F32Bit =         0x0008,
        Const_ReservedBits1 =  0x00f0,
        Const_FSel =           0x0100,
        Const_FAbs =           0x0200,
        Const_ReservedBits2 =  0x0C00,
        Const_FGroup =         0x1000,
        Const_ReservedBits3 =  0xE000,


        StandardText = (Const_FSel|Const_F32Bit|Const_Exec|Const_Read), // 0x10D
        SentinelType = (Const_FAbs|Const_F32Bit) // 0x208
    }


    /// <summary>
    /// IUnknown COM type for writing NGen PDBs
    /// </summary>
    /// <remarks>
    /// <code>
    /// [
    ///  object,
    ///  local,
    ///  uuid(B029E51B-4C55-4fe2-B993-9F7BC1F10DB4),
    ///  pointer_default(unique)
    /// ]
    /// interface ISymNGenWriter2 : ISymNGenWriter
    /// {
    ///     HRESULT OpenModW([in] const wchar_t* wszModule,
    ///                      [in] const wchar_t* wszObjFile,
    ///                      [out] BYTE** ppmod);
    ///
    ///     HRESULT CloseMod([in] BYTE* pmod);
    ///
    ///     HRESULT ModAddSymbols([in] BYTE* pmod, [in] BYTE* pbSym, [in] long cb);
    ///
    ///     HRESULT ModAddSecContribEx(
    ///         [in] BYTE* pmod,
    ///         [in] USHORT isect,
    ///         [in] long off,
    ///         [in] long cb,
    ///         [in] ULONG dwCharacteristics,
    ///         [in] DWORD dwDataCrc,
    ///         [in] DWORD dwRelocCrc);
    ///
    ///     HRESULT QueryPDBNameExW(
    ///         [out, size_is(cchMax)] wchar_t wszPDB[],
    ///         [in] SIZE_T cchMax);
    /// };
    /// </remarks>
    /// </code>
    [GeneratedComInterface]
    [Guid("B029E51B-4C55-4fe2-B993-9F7BC1F10DB4")]
    internal partial interface ISymNGenWriter2 : ISymNGenWriter
    {
        void OpenModW([MarshalAs(UnmanagedType.LPWStr)] string wszModule,
                      [MarshalAs(UnmanagedType.LPWStr)] string wszObjFile,
                      out UIntPtr ppmod);

        void CloseMod(UIntPtr pmod);

        void ModAddSymbols(UIntPtr pmod, [MarshalAs(UnmanagedType.LPArray)] byte[] pbSym, int cb);

        void ModAddSecContribEx(
            UIntPtr pmod,
            ushort isect,
            int off,
            int cb,
            uint dwCharacteristics,
            uint dwDataCrc,
            uint dwRelocCrc);

        void QueryPDBNameExW(
            [MarshalUsing(CountElementName = nameof(cchMax))]
            char[] pdb,
            IntPtr cchMax);
    }
}
