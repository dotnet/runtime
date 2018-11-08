// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.TypeLoading.Ecma
{
    //
    // Accessing MetadataReaders after their containing PEReaders have been disposed fails in very messy ways
    // (as in accessing native memory after free.) This also crashes debugged processes if Reflection objects
    // created by MetadataLoadContext objects are sitting in the watch window after disposal.
    //
    // To avoid this unfortunate scenario, we'll gate all access to PEReaders and MetadataReaders 
    // through this dispose guard. This does not make the api entirely safe (if a Reflection api is in flight 
    // during a Dispose(), all bets are off) but it does make the overall experience less evil.
    //
    internal readonly struct GuardedPEReader
    {
        private readonly MetadataLoadContext _loader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // Don't want debugger watch windows triggering AV's if one is still around after disposal.
        private readonly PEReader _peReader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // Don't want debugger watch windows triggering AV's if one is still around after disposal.
        private readonly MetadataReader _reader;

        public GuardedPEReader(MetadataLoadContext loader, PEReader peReader, MetadataReader reader)
        {
            Debug.Assert(loader != null);
            Debug.Assert(peReader != null);
            Debug.Assert(reader != null);

            _loader = loader;
            _peReader = peReader;
            _reader = reader;
        }

        public PEReader PEReader { get { _loader.DisposeCheck(); return _peReader; } }
        public MetadataReader Reader { get { _loader.DisposeCheck(); return _reader; } }
    }
}
