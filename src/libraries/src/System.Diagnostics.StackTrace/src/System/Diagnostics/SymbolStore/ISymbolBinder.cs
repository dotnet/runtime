// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolBinder
    {
        // The importer parameter should be an IntPtr, not an int. This interface can not be modified without
        // a breaking change, and so ISymbolBinderEx.GetReader() has been added with the correct marshalling layout.
        [Obsolete("The recommended alternative is ISymbolBinder1.GetReader. ISymbolBinder1.GetReader takes the importer interface pointer as an IntPtr instead of an Int32, and thus works on both 32-bit and 64-bit architectures. https://go.microsoft.com/fwlink/?linkid=14202=14202")]
        ISymbolReader GetReader(int importer, string filename, string searchPath);
    }
 
    public interface ISymbolBinder1
    {
        ISymbolReader GetReader(IntPtr importer, string filename, string searchPath);
    }
}
