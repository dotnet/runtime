// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static SharedTypes.IntWrapperWithNotificationMarshaller;

namespace SharedTypes
{
    [NativeMarshalling(typeof(StringContainerMarshaller))]
    public struct StringContainer
    {
        public string str1;
        public string str2;
    }

    [CustomMarshaller(typeof(StringContainer), Scenario.ManagedToUnmanagedIn, typeof(In))]
    [CustomMarshaller(typeof(StringContainer), Scenario.ManagedToUnmanagedRef, typeof(Ref))]
    [CustomMarshaller(typeof(StringContainer), Scenario.ManagedToUnmanagedOut, typeof(Out))]
    public static class StringContainerMarshaller
    {
        public struct StringContainerNative
        {
            public IntPtr str1;
            public IntPtr str2;
        }

        public static class In
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
                => Ref.ConvertToUnmanaged(managed);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }

        public static class Ref
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
            {
                return new StringContainerNative
                {
                    str1 = Marshal.StringToCoTaskMemUTF8(managed.str1),
                    str2 = Marshal.StringToCoTaskMemUTF8(managed.str2)
                };
            }

            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
            {
                return new StringContainer
                {
                    str1 = Marshal.PtrToStringUTF8(unmanaged.str1),
                    str2 = Marshal.PtrToStringUTF8(unmanaged.str2)
                };
            }

            public static void Free(StringContainerNative unmanaged)
            {
                Marshal.FreeCoTaskMem(unmanaged.str1);
                Marshal.FreeCoTaskMem(unmanaged.str2);
            }
        }

        public static class Out
        {
            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
                => Ref.ConvertToManaged(unmanaged);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }
    }

    [CustomMarshaller(typeof(double), Scenario.ManagedToUnmanagedIn, typeof(DoubleToBytesBigEndianMarshaller))]
    public static unsafe class DoubleToBytesBigEndianMarshaller
    {
        public const int BufferSize = 8;

        public static byte* ConvertToUnmanaged(double managed, Span<byte> buffer)
        {
            BinaryPrimitives.WriteDoubleBigEndian(buffer, managed);
            return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
        }
    }

    [CustomMarshaller(typeof(double), Scenario.ManagedToUnmanagedIn, typeof(DoubleToLongMarshaller))]
    public static class DoubleToLongMarshaller
    {
        public static long ConvertToUnmanaged(double managed)
        {
            return MemoryMarshal.Cast<double, long>(MemoryMarshal.CreateSpan(ref managed, 1))[0];
        }
    }

    [NativeMarshalling(typeof(BoolStructMarshaller))]
    public struct BoolStruct
    {
        public bool b1;
        public bool b2;
        public bool b3;
    }

    [CustomMarshaller(typeof(BoolStruct), Scenario.Default, typeof(BoolStructMarshaller))]
    public static class BoolStructMarshaller
    {
        public struct BoolStructNative
        {
            public byte b1;
            public byte b2;
            public byte b3;
        }

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed)
        {
            return new BoolStructNative
            {
                b1 = (byte)(managed.b1 ? 1 : 0),
                b2 = (byte)(managed.b2 ? 1 : 0),
                b3 = (byte)(managed.b3 ? 1 : 0)
            };
        }

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged)
        {
            return new BoolStruct
            {
                b1 = unmanaged.b1 != 0,
                b2 = unmanaged.b2 != 0,
                b3 = unmanaged.b3 != 0
            };
        }
    }

    [NativeMarshalling(typeof(IntWrapperMarshaller))]
    public class IntWrapper
    {
        public int i;

        public ref int GetPinnableReference() => ref i;
    }

    [CustomMarshaller(typeof(IntWrapper), Scenario.Default, typeof(IntWrapperMarshaller))]
    public static unsafe class IntWrapperMarshaller
    {
        public static int* ConvertToUnmanaged(IntWrapper managed)
        {
            int* ret = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *ret = managed.i;
            return ret;
        }

        public static IntWrapper ConvertToManaged(int* unmanaged)
        {
            return new IntWrapper { i = *unmanaged };
        }

        public static void Free(int* unmanaged)
        {
            Marshal.FreeCoTaskMem((IntPtr)unmanaged);
        }
    }


    [CustomMarshaller(typeof(IntWrapper), Scenario.Default, typeof(Marshaller))]
    public static unsafe class IntWrapperMarshallerStateful
    {
        public struct Marshaller
        {
            private IntWrapper managed;
            private int* native;
            public void FromManaged(IntWrapper wrapper)
            {
                managed = wrapper;
            }

            public int* ToUnmanaged()
            {
                native = (int*)Marshal.AllocCoTaskMem(sizeof(int));
                *native = managed.i;
                return native;
            }

            public void FromUnmanaged(int* value)
            {
                native = value;
            }

            public IntWrapper ToManaged() => managed = new IntWrapper() { i = *native };

            public void Free()
            {
                Marshal.FreeCoTaskMem((IntPtr)native);
            }
        }
    }

    [NativeMarshalling(typeof(IntWrapperWithNotificationMarshaller))]
    public struct IntWrapperWithNotification
    {
        [ThreadStatic]
        public static int NumInvokeSucceededOnUninitialized = 0;

        private bool initialized;
        public int Value;
        public event EventHandler InvokeSucceeded;

        public IntWrapperWithNotification()
        {
            initialized = true;
        }

        public void RaiseInvokeSucceeded()
        {
            if (!initialized)
            {
                NumInvokeSucceededOnUninitialized++;
            }
            InvokeSucceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    [CustomMarshaller(typeof(IntWrapperWithNotification), Scenario.Default, typeof(Marshaller))]
    public static class IntWrapperWithNotificationMarshaller
    {
        public struct Marshaller
        {
            private IntWrapperWithNotification _managed;

            public void FromManaged(IntWrapperWithNotification managed) =>_managed = managed;

            public int ToUnmanaged() => _managed.Value;

            public void FromUnmanaged(int i) => _managed.Value = i;

            public IntWrapperWithNotification ToManaged() => _managed;

            public void NotifyInvokeSucceeded() => _managed.RaiseInvokeSucceeded();
        }
    }

    [CustomMarshaller(typeof(BoolStruct), Scenario.Default, typeof(Marshaller))]
    public static class BoolStructMarshallerStateful
    {
        public struct BoolStructNative
        {
            public byte b1;
            public byte b2;
            public byte b3;
        }

        public struct Marshaller
        {
            private BoolStructNative _boolStructNative;
            public void FromManaged(BoolStruct managed)
            {
                _boolStructNative = new BoolStructNative
                {
                    b1 = (byte)(managed.b1 ? 1 : 0),
                    b2 = (byte)(managed.b2 ? 1 : 0),
                    b3 = (byte)(managed.b3 ? 1 : 0)
                };
            }

            public BoolStructNative ToUnmanaged() => _boolStructNative;

            public void FromUnmanaged(BoolStructNative value) => _boolStructNative = value;

            public BoolStruct ToManaged()
            {
                return new BoolStruct
                {
                    b1 = _boolStructNative.b1 != 0,
                    b2 = _boolStructNative.b2 != 0,
                    b3 = _boolStructNative.b3 != 0
                };
            }
        }
    }
}
