// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolBinder
    {
        // The importer parameter should be an IntPtr, not an int. This interface can not be modified without
        // a breaking change, and so ISymbolBinderEx.GetReader() has been added with the correct marshalling layout.
        [Obsolete("ISymbolBinder.GetReader has been deprecated because it is not 64-bit compatible. Use ISymbolBinder1.GetReader instead. ISymbolBinder1.GetReader takes the importer interface pointer as an IntPtr instead of an Int32, and thus works on both 32-bit and 64-bit architectures.")]
        ISymbolReader? GetReader(int importer, string filename, string searchPath);
    }

    public interface ISymbolBinder1
    {
        ISymbolReader? GetReader(IntPtr importer, string filename, string searchPath);
    }
}
