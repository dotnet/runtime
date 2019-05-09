// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System.Collections.Generic
{
    [TypeDependencyAttribute("System.Collections.Generic.ObjectEqualityComparer`1")]
    public abstract partial class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        // To minimize generic instantiation overhead of creating the comparer per type, we keep the generic portion of the code as small
        // as possible and define most of the creation logic in a non-generic class.
        public static EqualityComparer<T> Default { [Intrinsic] get; } = (EqualityComparer<T>)ComparerHelpers.CreateDefaultEqualityComparer(typeof(T));

        internal virtual int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }

        internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }
    }

    public sealed partial class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>
    {
        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (value == null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (value == null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }
    }

    public sealed partial class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
    {
        internal override int IndexOf(T?[] array, T? value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (!value.HasValue)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (!array[i].HasValue) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (!value.HasValue)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (!array[i].HasValue) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
        }
    }

    public sealed partial class ObjectEqualityComparer<T> : EqualityComparer<T>
    {
        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (value == null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] != null && array[i]!.Equals(value)) return i; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34644
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (value == null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] != null && array[i]!.Equals(value)) return i; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34644
                }
            }
            return -1;
        }
    }

    public sealed partial class ByteEqualityComparer : EqualityComparer<byte>
    {
#if DEBUG
        internal override int IndexOf(byte[] array, byte value, int startIndex, int count)
        {
             Debug.Fail("Should not get here.");
             return -1;
        }

        internal override int LastIndexOf(byte[] array, byte value, int startIndex, int count)
        {
             Debug.Fail("Should not get here.");
             return -1;
        }
#endif
    }

    public sealed partial class EnumEqualityComparer<T> : EqualityComparer<T> where T : struct, Enum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y)
        {
            return System.Runtime.CompilerServices.JitHelpers.EnumEquals(x, y);
        }

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (System.Runtime.CompilerServices.JitHelpers.EnumEquals(array[i], value)) return i;
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (System.Runtime.CompilerServices.JitHelpers.EnumEquals(array[i], value)) return i;
            }
            return -1;
        }
    }
}
