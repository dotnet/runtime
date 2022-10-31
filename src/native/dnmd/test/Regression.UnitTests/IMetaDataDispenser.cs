using System.Runtime.InteropServices;

namespace Regression.UnitTests
{
    [Flags]
    internal enum CorOpenFlags
    {
        CopyMemory        =   0x00000002,     // Open scope with memory. Ask metadata to maintain its own copy of memory.

        ReadOnly          =   0x00000010,     // Open scope for read. Will be unable to QI for a IMetadataEmit* interface
        TakeOwnership     =   0x00000020,     // The memory was allocated with CoTaskMemAlloc and will be freed by the metadata
    }

    [ComImport]
    [Guid("809C652E-7396-11D2-9771-00A0C9B4D50C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMetaDataDispenser
    {
        [PreserveSig]
        public int DefineScope();

        [PreserveSig]
        public int OpenScope();

        [PreserveSig]
        int OpenScopeOnMemory(
            void* pData,
            int cbData,
            CorOpenFlags dwOpenFlags,
            Guid* riid,
            void** ppIUnk);
    }
}
