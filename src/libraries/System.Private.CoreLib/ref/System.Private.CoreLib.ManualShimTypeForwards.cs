// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: These types are forwarded from the mscorlib.dll shim, but are not publicly
//       exposed in System.Runtime.dll so we need to manually declare them here.
//       We only need the simple type definition here instead of the full members list.

namespace System
{
    public class CultureAwareComparer { }
    public class OrdinalComparer { }
    public class UnitySerializationHolder { }
}
namespace System.Collections
{
    public class ListDictionaryInternal { }
}
namespace System.Collections.Generic
{
    public class ByteEqualityComparer { }
    public class EnumEqualityComparer<T> { }
    public class GenericComparer<T> { }
    public class GenericEqualityComparer<T> { }
    public class NonRandomizedStringEqualityComparer { }
    public class NullableComparer<T> { }
    public class NullableEqualityComparer<T> { }
    public class ObjectComparer<T> { }
    public class ObjectEqualityComparer<T> { }
}
namespace System.Diagnostics.Contracts
{
    public class ContractException { }
}
namespace System.Reflection.Emit
{
    public enum PEFileKinds { }
}
