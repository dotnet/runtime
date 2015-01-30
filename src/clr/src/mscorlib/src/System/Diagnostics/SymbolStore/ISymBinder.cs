// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


/*============================================================
**
**
**
** Represents a symbol binder for managed code.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    
    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    
    // Interface does not need to be marked with the serializable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolBinder
    {
        // The importer parameter should be an IntPtr, not an int. This interface can not be modified without
        // a breaking change, and so ISymbolBinderEx.GetReader() has been added with the correct marshalling layout.
        [Obsolete("The recommended alternative is ISymbolBinder1.GetReader. ISymbolBinder1.GetReader takes the importer interface pointer as an IntPtr instead of an Int32, and thus works on both 32-bit and 64-bit architectures. http://go.microsoft.com/fwlink/?linkid=14202=14202")]
        ISymbolReader GetReader(int importer, String filename,
                                String searchPath);
    }

    // This interface has a revised ISymbolBinder.GetReader() with the proper signature.
    // It is not called ISymbolBinder2 because it maps to the IUnmanagedSymbolBinder interfaces, and 
    // does not wrap the IUnmanagedSymbolBinder2 interfaces declared in CorSym.idl.
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolBinder1
    {
    
        ISymbolReader GetReader(IntPtr importer, String filename,
                                String searchPath);
    }

}
