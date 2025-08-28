// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sample;

namespace System.Runtime.InteropServices.Tests
{
    static public class GCHandleTests
    {
        
        static public void Ctor_Default()
        {
            //var handle = new GCHandle();

            var handle = new System.Runtime.InteropServices.GCHandle();
            var t = handle.Target;
            System.Console.WriteLine(handle.GetType().FullName);
            //Assert.Throws<InvalidOperationException>(() => handle.Target);
            Assert.False(handle.IsAllocated);

            Assert.Equal(IntPtr.Zero, GCHandle.ToIntPtr(handle));
            Assert.Equal(IntPtr.Zero, (IntPtr)handle);
        }

        
        //static public void Ctor_Default_Generic()
        //{
        //    var handle = new GCHandle<object>();
        //    Assert.Throws<NullReferenceException>(() => handle.Target);
        //    Assert.False(handle.IsAllocated);

        //    Assert.Equal(IntPtr.Zero, GCHandle<object>.ToIntPtr(handle));
        //}

        
        //static public void Ctor_Default_Weak()
        //{
        //    var handle = new WeakGCHandle<object>();
        //    Assert.Throws<NullReferenceException>(() => handle.TryGetTarget(out _));
        //    Assert.False(handle.IsAllocated);

        //    Assert.Equal(IntPtr.Zero, WeakGCHandle<object>.ToIntPtr(handle));
        //}

        
        //static public void Ctor_Default_Pinned()
        //{
        //    var handle = new PinnedGCHandle<object>();
        //    Assert.Throws<NullReferenceException>(() => handle.Target);
        //    Assert.False(handle.IsAllocated);

        //    Assert.Equal(IntPtr.Zero, PinnedGCHandle<object>.ToIntPtr(handle));
        //}

        //static public IEnumerable<object[]> Alloc_TestData()
        //{
        //    yield return new object[] { null };
        //    yield return new object[] { "String" };
        //    yield return new object[] { 123 };
        //    yield return new object[] { new int[1] };
        //    yield return new object[] { new NonBlittable[1] };
        //    yield return new object[] { new object[1] };
        //    yield return new object[] { new NonBlittable() };
        //}

        ////[Theory]
        ////[MemberData(nameof(Alloc_TestData))]
        //static public void Alloc_Value_ReturnsExpected(object value)
        //{
        //    GCHandle handle = GCHandle.Alloc(value);
        //    ValidateGCHandle(handle, GCHandleType.Normal, value);
        //}

        
        //static public void Alloc_Value_ReturnsExpected_Genenic()
        //{
        //    RunTest<object>(null);
        //    RunTest("String");
        //    RunTest(new int[1]);
        //    RunTest(new object[1]);
        //    RunTest(new NonBlittable[1]);
        //    RunTest<object>(new Blittable());
        //    RunTest<object>(new NonBlittable());
        //    RunTest<object>(new UnmanagedNonBlittable());
        //    RunTest(new ClassWithReferences());
        //    RunTest(new ClassWithoutReferences());

        //    void RunTest<T>(T value) where T : class
        //    {
        //        ValidateGCHandle(new GCHandle<T>(value), value);
        //        ValidateWeakGCHandle(new WeakGCHandle<T>(value), value);
        //        ValidateWeakGCHandle(new WeakGCHandle<T>(value, trackResurrection: true), value);
        //        ValidatePinnedGCHandle(new PinnedGCHandle<T>(value), value);
        //    }
        //}

        //static public IEnumerable<object[]> Alloc_Type_TestData()
        //{
        //    foreach (object[] data in Alloc_TestData())
        //    {
        //        yield return new object[] { data[0], GCHandleType.Normal };
        //        yield return new object[] { data[0], GCHandleType.Weak };
        //        yield return new object[] { data[0], GCHandleType.WeakTrackResurrection };
        //    }

        //    yield return new object[] { null, GCHandleType.Pinned };
        //    yield return new object[] { "", GCHandleType.Pinned };
        //    yield return new object[] { 1, GCHandleType.Pinned };
        //    yield return new object[] { new object(), GCHandleType.Pinned };
        //    yield return new object[] { new Blittable(), GCHandleType.Pinned };
        //    yield return new object[] { new Blittable(), GCHandleType.Pinned };
        //    yield return new object[] { new Blittable[0], GCHandleType.Pinned };
        //    yield return new object[] { new UnmanagedNonBlittable(), GCHandleType.Pinned };
        //    yield return new object[] { new UnmanagedNonBlittable[0], GCHandleType.Pinned };
        //    yield return new object[] { new ClassWithoutReferences(), GCHandleType.Pinned };
        //}

        ////[Theory]
        ////[MemberData(nameof(Alloc_Type_TestData))]
        //static public void Alloc_Type_ReturnsExpected(object value, GCHandleType type)
        //{
        //    GCHandle handle = GCHandle.Alloc(value, type);
        //    ValidateGCHandle(handle, type, value);
        //}

        //static public IEnumerable<object[]> InvalidPinnedObject_TestData()
        //{
        //    yield return new object[] { new NonBlittable() };
        //    yield return new object[] { new ClassWithReferences() };
        //    yield return new object[] { new object[0] };
        //    yield return new object[] { new NonBlittable[0] };
        //    yield return new object[] { new ClassWithoutReferences[0] };
        //}

        ////[Theory]
        ////[MemberData(nameof(InvalidPinnedObject_TestData))]
        //static public void Alloc_InvalidPinnedObject_ThrowsArgumentException(object value)
        //{
        //    Assert.Throws<ArgumentException>(() => GCHandle.Alloc(value, GCHandleType.Pinned));
        //}

        ////[Theory]
        ////[InlineData(GCHandleType.Weak - 1)]
        ////[InlineData(GCHandleType.Pinned + 1)]
        //static public void Alloc_InvalidGCHandleType_ThrowsArgumentOutOfRangeException(GCHandleType type)
        //{
        //    AssertExtensions.Throws<ArgumentOutOfRangeException>("type", () => GCHandle.Alloc(new object(), type));
        //}

        
        //static public void FromIntPtr_Zero_ThrowsInvalidOperationException()
        //{
        //    Assert.Throws<InvalidOperationException>(() => GCHandle.FromIntPtr(IntPtr.Zero));
        //}

        
        //static public void FromIntPtr_Generic_Zero_NoCheck()
        //{
        //    var handle = GCHandle<object>.FromIntPtr(IntPtr.Zero);
        //    Assert.False(handle.IsAllocated);
        //    var weakHandle = WeakGCHandle<object>.FromIntPtr(IntPtr.Zero);
        //    Assert.False(weakHandle.IsAllocated);
        //    var pinnedHandle = PinnedGCHandle<object>.FromIntPtr(IntPtr.Zero);
        //    Assert.False(pinnedHandle.IsAllocated);
        //}

        
        //static public unsafe void AddrOfPinnedObject_NotInitialized_ThrowsException()
        //{
        //    var handle = new GCHandle();
        //    Assert.Throws<InvalidOperationException>(() => handle.AddrOfPinnedObject());
        //    var handleOfObject = new PinnedGCHandle<object>();
        //    Assert.Throws<NullReferenceException>(() => handleOfObject.GetAddressOfObjectData());
        //    var handleOfString = new PinnedGCHandle<string>();
        //    Assert.Throws<NullReferenceException>(() => handleOfString.GetAddressOfStringData());
        //    var handleOfArray = new PinnedGCHandle<int[]>();
        //    Assert.Throws<NullReferenceException>(() => handleOfArray.GetAddressOfArrayData());
        //}

        
        //static public unsafe void AddrOfPinnedObject_ReturnsStringData()
        //{
        //    string str = "String";
        //    fixed (char* ptr = str)
        //    {
        //        var handle = GCHandle.Alloc(str, GCHandleType.Pinned);
        //        try
        //        {
        //            Assert.Equal((IntPtr)ptr, handle.AddrOfPinnedObject());
        //            using var handleOfString = new PinnedGCHandle<string>(str);
        //            Assert.NotEqual((IntPtr)ptr, (IntPtr)handleOfString.GetAddressOfObjectData());
        //            Assert.Equal((IntPtr)ptr, (IntPtr)handleOfString.GetAddressOfStringData());
        //        }
        //        finally
        //        {
        //            handle.Free();
        //        }
        //    }
        //}

        
        //static public unsafe void AddrOfPinnedObject_ReturnsArrayData()
        //{
        //    int[] array = new int[1];
        //    fixed (int* ptr = array)
        //    {
        //        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        //        try
        //        {
        //            Assert.Equal((IntPtr)ptr, handle.AddrOfPinnedObject());
        //            using var handleOfArray = new PinnedGCHandle<int[]>(array);
        //            Assert.NotEqual((IntPtr)ptr, (IntPtr)handleOfArray.GetAddressOfObjectData());
        //            Assert.Equal((IntPtr)ptr, (IntPtr)handleOfArray.GetAddressOfArrayData());
        //        }
        //        finally
        //        {
        //            handle.Free();
        //        }
        //    }
        //}

        
        //static public void AddrOfPinnedObject_NotPinned_ThrowsInvalidOperationException()
        //{
        //    GCHandle handle = GCHandle.Alloc(new object());
        //    try
        //    {
        //        Assert.Throws<InvalidOperationException>(() => handle.AddrOfPinnedObject());
        //    }
        //    finally
        //    {
        //        handle.Free();
        //    }
        //}

        
        //static public void Free_NotInitialized_ThrowsInvalidOperationException()
        //{
        //    var handle = new GCHandle();
        //    Assert.Throws<InvalidOperationException>(() => handle.Free());
        //}

        
        //static public void Dispose_NotInitialized_NoThrow()
        //{
        //    var handleOfObject = new GCHandle<object>();
        //    handleOfObject.Dispose();
        //    var weakHandle = new WeakGCHandle<object>();
        //    weakHandle.Dispose();
        //    var pinnedHandle = new PinnedGCHandle<object>();
        //    pinnedHandle.Dispose();
        //}

        //static public IEnumerable<object[]> Equals_TestData()
        //{
        //    GCHandle handle = GCHandle.Alloc(new object());
        //    yield return new object[] { handle, handle, true };
        //    yield return new object[] { GCHandle.Alloc(new object()), GCHandle.Alloc(new object()), false };

        //    yield return new object[] { GCHandle.Alloc(new object()), new object(), false };
        //    yield return new object[] { GCHandle.Alloc(new object()), null, false };
        //}

        ////[Theory]
        ////[MemberData(nameof(Equals_TestData))]
        //static public void Equals_Object_ReturnsExpected(GCHandle handle, object other, bool expected)
        //{
        //    try
        //    {
        //        Assert.Equal(expected, handle.Equals(other));
        //        if (other is GCHandle otherHandle)
        //        {
        //            Assert.Equal(expected, handle.Equals(otherHandle));
        //            Assert.Equal(expected, handle == otherHandle);
        //            Assert.Equal(!expected, handle != otherHandle);
        //        }
        //    }
        //    finally
        //    {
        //        handle.Free();
        //        if (other is GCHandle otherHandle && !expected)
        //        {
        //            otherHandle.Free();
        //        }
        //    }
        //}

        //private static void ValidateGCHandle(GCHandle handle, GCHandleType type, object target)
        //{
        //    try
        //    {
        //        Assert.Equal(target, handle.Target);
        //        Assert.True(handle.IsAllocated);

        //        Assert.NotEqual(IntPtr.Zero, GCHandle.ToIntPtr(handle));
        //        Assert.Equal(GCHandle.ToIntPtr(handle), (IntPtr)handle);
        //        Assert.Equal(((IntPtr)handle).GetHashCode(), handle.GetHashCode());

        //        if (type == GCHandleType.Pinned)
        //        {
        //            if (target == null)
        //            {
        //                Assert.Equal(IntPtr.Zero, handle.AddrOfPinnedObject());
        //            }
        //            else
        //            {
        //                Assert.NotEqual(IntPtr.Zero, handle.AddrOfPinnedObject());
        //            }
        //        }

        //    }
        //    finally
        //    {
        //        handle.Free();
        //        Assert.False(handle.IsAllocated);
        //    }
        //}

        //private static void ValidateGCHandle<T>(GCHandle<T> handle, T target)
        //    where T : class
        //{
        //    try
        //    {
        //        Assert.Equal(target, handle.Target);
        //        Assert.True(handle.IsAllocated);

        //        Assert.NotEqual(IntPtr.Zero, GCHandle<T>.ToIntPtr(handle));
        //    }
        //    finally
        //    {
        //        handle.Dispose();
        //        Assert.False(handle.IsAllocated);
        //    }
        //}

        //private static void ValidateWeakGCHandle<T>(WeakGCHandle<T> handle, T target) where T : class
        //{
        //    try
        //    {
        //        if (target != null)
        //        {
        //            Assert.True(handle.TryGetTarget(out T outTarget));
        //            Assert.Equal(target, outTarget);
        //        }
        //        else
        //        {
        //            Assert.False(handle.TryGetTarget(out T outTarget));
        //            Assert.Null(outTarget);
        //        }
        //        Assert.True(handle.IsAllocated);

        //        Assert.NotEqual(IntPtr.Zero, WeakGCHandle<T>.ToIntPtr(handle));
        //    }
        //    finally
        //    {
        //        handle.Dispose();
        //        Assert.False(handle.IsAllocated);
        //    }
        //}

        //private static unsafe void ValidatePinnedGCHandle<T>(PinnedGCHandle<T> handle, T target) where T : class
        //{
        //    try
        //    {
        //        Assert.Equal(target, handle.Target);
        //        Assert.True(handle.IsAllocated);

        //        Assert.NotEqual(IntPtr.Zero, PinnedGCHandle<T>.ToIntPtr(handle));

        //        if (target == null)
        //        {
        //            Assert.Equal(IntPtr.Zero, (IntPtr)handle.GetAddressOfObjectData());
        //        }
        //        else
        //        {
        //            Assert.NotEqual(IntPtr.Zero, (IntPtr)handle.GetAddressOfObjectData());
        //        }
        //    }
        //    finally
        //    {
        //        handle.Dispose();
        //        Assert.False(handle.IsAllocated);
        //    }
        //}

        //public struct Blittable
        //{
        //    static public int _field;
        //}

        //public class ClassWithoutReferences
        //{
        //    static public int _field;
        //}

        //public struct UnmanagedNonBlittable
        //{
        //    static public char _field1;
        //    static public bool _field2;
        //}

        //public struct NonBlittable
        //{
        //    static public string _field;
        //}

        //public class ClassWithReferences
        //{
        //    static public string _field;
        //}
    }
}
