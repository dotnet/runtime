// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.DiaSymReader
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D682FD12-43dE-411C-811B-BE8404CEA126"), SuppressUnmanagedCodeSecurity]
    internal interface ISymNGenWriter
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


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B029E51B-4C55-4fe2-B993-9F7BC1F10DB4"), SuppressUnmanagedCodeSecurity]
    internal interface ISymNGenWriter2 : ISymNGenWriter
    {
        // Add a new public symbol to the NGEN PDB.
        new void AddSymbol([MarshalAs(UnmanagedType.BStr)] string pSymbol,
                        ushort iSection,
                        ulong rva);

        // Adds a new section to the NGEN PDB.
        new void AddSection(ushort iSection,
                        OMF flags,
                        int offset,
                        int cb);

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
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pdb,
            IntPtr cchMax);
    }
}
