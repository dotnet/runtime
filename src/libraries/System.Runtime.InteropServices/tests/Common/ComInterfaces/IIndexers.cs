// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IIndexers
    {
        public const string IID = "7C0E5C9D-5F3E-49E0-8C9A-1A8A2A2C9B1A";

        // Default-named single-parameter indexer.
        int this[int i] { get; set; }

        // Two-parameter indexer overload. With the same default name ("Item") this exercises
        // overloaded vtable slot placement (each get/set pair gets its own adjacent slot pair).
        int this[int i, int j] { get; set; }

        // Read-only indexer overload by parameter type. Single vtable slot.
        int this[long l] { get; }

        // Write-only indexer overload by parameter type. Single vtable slot.
        int this[short s] { set; }

        // String-keyed, string-valued indexer overload. Exercises the UTF-16 string marshalling
        // pipeline on both the index parameter and the value parameter / return type — distinct
        // from the blittable primitive marshalling paths above.
        string this[string key] { get; set; }
    }

    [GeneratedComClass]
    internal partial class Indexers : IIndexers
    {
        // Backing store keyed by index — small dictionary kept inline to avoid allocator churn in tests.
        private int _singleValue;
        private int _twoParamValue;
        private int _writeOnlyShortSink;
        private readonly Dictionary<string, string> _stringMap = new();

        public int this[int i]
        {
            get => _singleValue + i;
            set => _singleValue = value - i;
        }

        public int this[int i, int j]
        {
            get => _twoParamValue + (i * 100) + j;
            set => _twoParamValue = value - (i * 100) - j;
        }

        public int this[long l] => unchecked((int)(l * 7));

        public int this[short s]
        {
            set => _writeOnlyShortSink = value + s;
        }

        public string this[string key]
        {
            get => _stringMap.TryGetValue(key, out string? v) ? v : string.Empty;
            set => _stringMap[key] = value;
        }

        // Non-interface accessor so the WriteOnly setter's side effect is observable from tests.
        internal int WriteOnlyShortSink => _writeOnlyShortSink;
    }

    // A second indexer interface that uses [IndexerName] so the generated IL accessor names are
    // get_Element / set_Element instead of the default get_Item / set_Item. This exercises the
    // [IndexerName]-propagation path in the derived-interface shadow emitter as well.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IRenamedIndexer
    {
        public const string IID = "9F5A6D1E-1F4B-4E22-9A7C-1E0E33D5A2E1";

        [IndexerName("Element")]
        int this[int i] { get; set; }
    }

    [GeneratedComClass]
    internal partial class RenamedIndexer : IRenamedIndexer
    {
        private int _value;

        public int this[int i]
        {
            get => _value + i;
            set => _value = value - i;
        }
    }
}
