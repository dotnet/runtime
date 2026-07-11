// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    // Default-named indexer in the base; the derived interface shadows it with `new`.
    // All indexers in a type must share the same effective IndexerName, so this interface
    // contains only one indexer.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IDerivedIndexersBase
    {
        public const string IID = "7E5C3F1B-0E2D-4F7A-A38C-2BFD8D5C1E72";

        int this[int i] { get; set; }
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IDerivedIndexers : IDerivedIndexersBase
    {
        public new const string IID = "B0C8D7A4-3FF8-4F1B-9C29-6B7D2C6F1A21";

        new int this[int i] { get; set; }
    }

    // [IndexerName]-renamed base + derived shadow. The derived shadow MUST also carry
    // [IndexerName("Foo")] so its IL accessor names are get_Foo/set_Foo (not the default
    // get_Item/set_Item) and stay in sync with the base accessor identity.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IRenamedDerivedIndexersBase
    {
        public const string IID = "A48F37C2-0B6E-4B0A-9C77-7E1D3A19A4F6";

        [IndexerName("Foo")]
        int this[long l] { get; set; }
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IRenamedDerivedIndexers : IRenamedDerivedIndexersBase
    {
        public new const string IID = "D9B12C71-44E8-4F6A-AB55-39C8E7F0C5D2";

        [IndexerName("Foo")]
        new int this[long l] { get; set; }
    }
}

