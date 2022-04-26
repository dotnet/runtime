
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    internal sealed unsafe class ILCompilerComWrappers : ComWrappers
    {
        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            // passing the managed object to COM is not currently supported
            throw new NotImplementedException();
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            // Assert use of the UniqueInstance flag since the returned Native Object Wrapper always
            // supports IDisposable and its Dispose will always release and suppress finalization.
            // If the wrapper doesn't always support IDisposable the assert can be relaxed.
            Debug.Assert(flags.HasFlag(CreateObjectFlags.UniqueInstance));

            // Throw an exception if the type is not supported by the implementation.
            // Null can be returned as well, but an ArgumentNullException will be thrown for
            // the consumer of this ComWrappers instance.
            return SymNgenWriterWrapper.CreateIfSupported(externalComObject) ?? throw new NotSupportedException();
        }

        protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
    }
}