// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IPropertyMarshalling
    {
        public const string IID = "21DD41F1-7B6E-4F75-8C50-2D8E68C5C0BD";

        int TargetScoped
        {
            [return: MarshalUsing(typeof(TrackedIntMarshaller))]
            get;
            [param: MarshalUsing(typeof(TrackedIntMarshaller))]
            set;
        }

        int ReadOnlyMarshalled
        {
            [return: MarshalUsing(typeof(TrackedIntMarshaller))]
            get;
        }

        int WriteOnlyMarshalled
        {
            [param: MarshalUsing(typeof(TrackedIntMarshaller))]
            set;
        }

        [MarshalUsing(typeof(TrackedIntMarshaller))]
        int BareMarshalled { get; set; }

        [MarshalUsing(typeof(AlternateIntMarshaller))]
        int AccessorOverridesProperty
        {
            [return: MarshalUsing(typeof(TrackedIntMarshaller))]
            get;
            [param: MarshalUsing(typeof(TrackedIntMarshaller))]
            set;
        }

        [MarshalUsing(typeof(AlternateIntMarshaller))]
        int MixedPropertyAndAccessor
        {
            [return: MarshalUsing(typeof(TrackedIntMarshaller))]
            get;
            set;
        }

        public const int ElementIndirectionArrayLength = 4;

        // The property-level [MarshalUsing] supplies the per-element marshaller at depth 1
        // (one indirection past the int[] value itself). Each accessor-level [MarshalUsing]
        // supplies the depth-0 marshaller for the array and the constant collection size for
        // the COM ABI. The accessor-level depth-0 attribute must NOT displace the property-
        // level depth-1 attribute: dedup of property-vs-accessor [MarshalUsing] is partitioned
        // by ElementIndirectionDepth.
        [MarshalUsing(typeof(TrackedIntMarshaller), ElementIndirectionDepth = 1)]
        int[] ElementIndirectionArray
        {
            [return: MarshalUsing(typeof(ArrayMarshaller<int, int>), ConstantElementCount = ElementIndirectionArrayLength)]
            get;
            [param: MarshalUsing(typeof(ArrayMarshaller<int, int>), ConstantElementCount = ElementIndirectionArrayLength)]
            set;
        }
    }

    [GeneratedComClass]
    internal partial class PropertyMarshalling : IPropertyMarshalling
    {
        private int _writeOnly;

        public int TargetScoped { get; set; }
        public int ReadOnlyMarshalled { get; set; } = 99;
        public int WriteOnlyMarshalled { get => _writeOnly; set => _writeOnly = value; }
        public int BareMarshalled { get; set; }
        public int AccessorOverridesProperty { get; set; }
        public int MixedPropertyAndAccessor { get; set; }
        public int[] ElementIndirectionArray { get; set; } = new int[IPropertyMarshalling.ElementIndirectionArrayLength];

        public int WriteOnlySink => _writeOnly;
    }

    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IIndexerMarshalling
    {
        public const string IID = "9F8E7D6C-5B4A-3210-FEDC-BA9876543210";

        [MarshalUsing(typeof(TrackedIntMarshaller))]
        int this[int index] { get; set; }
    }

    [GeneratedComClass]
    internal partial class IndexerMarshalling : IIndexerMarshalling
    {
        private int _value;

        public int this[int index]
        {
            get => _value + index;
            set => _value = value - index;
        }
    }

    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedIn, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedOut, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ElementIn, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ElementOut, typeof(TrackedIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ElementRef, typeof(TrackedIntMarshaller))]
    internal static class TrackedIntMarshaller
    {
        private static int s_managedToUnmanagedCount;
        private static int s_unmanagedToManagedCount;

        public static int ManagedToUnmanagedCount => s_managedToUnmanagedCount;
        public static int UnmanagedToManagedCount => s_unmanagedToManagedCount;

        public static void Reset()
        {
            s_managedToUnmanagedCount = 0;
            s_unmanagedToManagedCount = 0;
        }

        public static int ConvertToUnmanaged(int managed)
        {
            Interlocked.Increment(ref s_managedToUnmanagedCount);
            return managed;
        }

        public static int ConvertToManaged(int unmanaged)
        {
            Interlocked.Increment(ref s_unmanagedToManagedCount);
            return unmanaged;
        }
    }

    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(AlternateIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedIn, typeof(AlternateIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(AlternateIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedOut, typeof(AlternateIntMarshaller))]
    internal static class AlternateIntMarshaller
    {
        private static int s_managedToUnmanagedCount;
        private static int s_unmanagedToManagedCount;

        public static int ManagedToUnmanagedCount => s_managedToUnmanagedCount;
        public static int UnmanagedToManagedCount => s_unmanagedToManagedCount;

        public static void Reset()
        {
            s_managedToUnmanagedCount = 0;
            s_unmanagedToManagedCount = 0;
        }

        public static int ConvertToUnmanaged(int managed)
        {
            Interlocked.Increment(ref s_managedToUnmanagedCount);
            return managed;
        }

        public static int ConvertToManaged(int unmanaged)
        {
            Interlocked.Increment(ref s_unmanagedToManagedCount);
            return unmanaged;
        }
    }
}
