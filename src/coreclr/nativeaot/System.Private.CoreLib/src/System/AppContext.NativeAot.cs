// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System
{
    public static partial class AppContext
    {
        private static unsafe Dictionary<string, object?> InitializeDataStore()
        {
            uint count = RuntimeImports.RhGetKnobValues(out byte** keys, out byte** values);

            var dataStore = new Dictionary<string, object?>((int)count);

            // Reuse a single buffer across all knobs. We can't use ArrayPool<char>.Shared here, as it relies on
            // static state that could itself be mid-initialization (see the comment on Utf8ToString below).
            char[]? buffer = null;

            for (int i = 0; i < count; i++)
            {
                dataStore.Add(Utf8ToString(keys[i], ref buffer), Utf8ToString(values[i], ref buffer));
            }

            return dataStore;
        }

        // This must not use Encoding.UTF8: AppContext can be initialized from Lock while a class constructor
        // (including the one for UTF8Encoding) is running on the current thread, in which case the encoding
        // static would be observed as null. Utf8.ToUtf16 has no static dependencies.
        private static unsafe string Utf8ToString(byte* utf8, ref char[]? buffer)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(utf8);

            if (buffer is null || buffer.Length < bytes.Length)
            {
                buffer = new char[bytes.Length];
            }

            Utf8.ToUtf16(bytes, buffer, out _, out int charsWritten);

            return new(buffer, 0, charsWritten);
        }

        [RuntimeExport("OnFirstChanceException")]
        internal static void OnFirstChanceException(object e)
            => OnFirstChanceException((Exception)e, sender: null);
    }
}
