// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    // Cross-assembly base interface mixing overloaded indexers and regular properties. Indexer
    // accessors all share the same IL name (get_Item / set_Item) regardless of parameter shape,
    // so this verifies the externally-defined-accessor path distinguishes vtable slots by full
    // signature rather than by accessor name. Regular property accessors are emitted alongside
    // the indexer slots in declaration order to confirm both surfaces participate in the same
    // cross-assembly inheritance path.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    public partial interface IExternalIndexersAndProperties
    {
        public const string IID = "4E3F2A57-2DD6-4F32-9B17-B0B6DEE6B7E6";

        int this[int i] { get; set; }
        int this[int i, int j] { get; set; }
        int this[long l] { get; }
        int this[short s] { set; }

        int Value { get; set; }
        string Name { get; }
        int WriteOnlyCounter { set; }
    }
}
