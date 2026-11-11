// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime;
using System.Text.Unicode;

namespace System
{
    public static partial class AppContext
    {
        private static unsafe Dictionary<string, object?> InitializeDataStore()
        {
            uint count = RuntimeImports.RhGetKnobValues(out byte** keys, out byte** values);

            var dataStore = new Dictionary<string, object?>((int)count);
            for (int i = 0; i < count; i++)
            {
                dataStore.Add(Utf8ToString(keys[i]), Utf8ToString(values[i]));
            }

            return dataStore;
        }

        // This must not use Encoding.UTF8: AppContext can be initialized from Lock while a class constructor
        // (including the one for UTF8Encoding) is running on the current thread, in which case the encoding
        // static would be observed as null. Utf8.ToUtf16 has no static dependencies.
        private static unsafe string Utf8ToString(byte* utf8)
        {
            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(utf8, string.strlen(utf8));
            char[] chars = new char[bytes.Length];
            Utf8.ToUtf16(bytes, chars, out _, out int charsWritten);
            return new string(chars, 0, charsWritten);
        }

        [RuntimeExport("OnFirstChanceException")]
        internal static void OnFirstChanceException(object e)
            => OnFirstChanceException((Exception)e, sender: null);
    }
}
