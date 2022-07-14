// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: These types are forwarded from the mscorlib.dll shim, but are not publicly
//       exposed in System.Runtime.dll so we need to manually declare them here.
//       We only need the simple type definition here instead of the full members list
//       since we don't want these to be actually used by libraries code.

namespace System
{
    public sealed class CultureAwareComparer { private CultureAwareComparer() { } }
    public sealed class OrdinalComparer { private OrdinalComparer() { } }
    public sealed class UnitySerializationHolder { private UnitySerializationHolder() { } }
}
namespace System.Collections
{
    public class ListDictionaryInternal { private ListDictionaryInternal() { } }
}
namespace System.Collections.Generic
{
    public sealed class ByteEqualityComparer { private ByteEqualityComparer() { } }
    public sealed class EnumEqualityComparer<T> { private EnumEqualityComparer() { } }
    public sealed class GenericComparer<T> { private GenericComparer() { } }
    public sealed class GenericEqualityComparer<T> { private GenericEqualityComparer() { } }
    public sealed class NonRandomizedStringEqualityComparer { private NonRandomizedStringEqualityComparer() { } }
    public sealed class NullableComparer<T> { private NullableComparer() { } }
    public sealed class NullableEqualityComparer<T> { private NullableEqualityComparer() { } }
    public sealed class ObjectComparer<T> { private ObjectComparer() { } }
    public sealed class ObjectEqualityComparer<T> { private ObjectEqualityComparer() { } }
}
namespace System.Diagnostics.Contracts
{
    public sealed class ContractException { private ContractException() { } }
}
namespace System.Reflection.Emit
{
    public enum PEFileKinds { }
}
