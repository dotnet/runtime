// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    public delegate bool CheckMethod<T>(T x, T y, T z, ref T c);

    public delegate bool CheckMethodTwo<T, U>(T x, T y, U z, ref U c);

    public delegate bool CheckMethodTwo<T, U, V>(T x, V y, U z, ref U c);

    public delegate bool CheckMethodThree<T, U>(T x1, T x2, T y1, T y2, U z, ref U c);

    public delegate bool CheckMethodFour<T, U>(T x1, T x2, U z1, U z2, ref U c1, ref U c2);

    public delegate bool CheckMethodFive<T, U>(T x1, T x2, T y1, T y2, U z1, U z2, ref U c1, ref U c2);

    public delegate bool CheckMethodFourTFourU<T, U>(
        ValueTuple<T, T, T, T> x,
        ValueTuple<T, T, T, T> y,
        ValueTuple<U, U, U, U> z,
        ref U a1, ref U a2, ref U a3, ref U a4);

    public delegate bool CheckMethodSix<T, U>(
        ValueTuple<T, T, T, T> x,
        ValueTuple<T, T, T, T> y,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z,
        ref U a1, ref U a2, ref U a3, ref U a4, ref U a5, ref U a6, ref U a7, ref U a8);

    public delegate bool CheckMethodEightOne<T, U>(
        Span<T> x, Span<T> y, U z, ref U a);

    public delegate bool CheckMethodEightOfTEightOfU<T, U>(
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> x,
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> y,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z,
        ref U a1, ref U a2, ref U a3, ref U a4, ref U a5, ref U a6, ref U a7, ref U a8);

    public delegate bool CheckMethodEightImm<T, U, V>(
        Span<T> x, T value, V i, Span<U> z, Span<U> a);

    public delegate bool CheckMethodSixteen<T, U>(
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> x,
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> y,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z1,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z2,
        ref U a1, ref U a2, ref U a3, ref U a4, ref U a5, ref U a6, ref U a7, ref U a8,
        ref U a9, ref U a10, ref U a11, ref U a12, ref U a13, ref U a14, ref U a15, ref U a16);

    public delegate bool CheckMethodSixteenOfAll<T, U>(
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> x1,
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> x2,
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> y1,
        ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>> y2,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z1,
        ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>> z2,
        ref U a1, ref U a2, ref U a3, ref U a4, ref U a5, ref U a6, ref U a7, ref U a8,
        ref U a9, ref U a10, ref U a11, ref U a12, ref U a13, ref U a14, ref U a15, ref U a16);

    [Flags]
    public enum InitMode
    {
        Undefined = 0,
        NumberFirstVectors = 0b00000001
    }

    public unsafe struct TestTableSse2<T> : IDisposable where T : struct
    {
        private const int _stepSize = 16;
        private int _scalarStepSize;

        private GCHandle _inHandle1;
        private GCHandle _inHandle2;
        private GCHandle _outHandle;
        private GCHandle _checkHandle;

        private int _index;

        public T[] inArray1;
        public T[] inArray2;
        public T[] outArray;
        public T[] checkArray;

        public void* InArray1Ptr => _inHandle1.AddrOfPinnedObject().ToPointer();
        public void* InArray2Ptr => _inHandle2.AddrOfPinnedObject().ToPointer();
        public void* OutArrayPtr => _outHandle.AddrOfPinnedObject().ToPointer();
        public void* CheckArrayPtr => _checkHandle.AddrOfPinnedObject().ToPointer();

        public Vector128<T> Vector1 => Unsafe.Read<Vector128<T>>((byte*)InArray1Ptr + (_index * _stepSize));
        public Vector128<T> Vector2 => Unsafe.Read<Vector128<T>>((byte*)InArray2Ptr + (_index * _stepSize));
        public Vector128<T> Vector3 => Unsafe.Read<Vector128<T>>((byte*)OutArrayPtr + (_index * _stepSize));
        public int Index { get => _index; set => _index = value; }

        public void SetOutArray(Vector128<T> value, int index = -1)
        {
            index = index < 0 ? _index : index;
            Unsafe.Write((byte*)OutArrayPtr + (index * _stepSize), value);
        }

        public (Vector128<T>, Vector128<T>, Vector128<T>) this[int index]
        {
            get
            {
                _index = index;
                return new ValueTuple<Vector128<T>, Vector128<T>, Vector128<T>>(Vector1, Vector2, Vector3);
            }
        }

        public ValueTuple<T, T, T, T> GetDataPoint(int index)
        {
            return (inArray1[index], inArray2[index], outArray[index], checkArray[index]);
        }

        public static TestTableSse2<T> Create(int lengthInVectors)
        {
            int length = _stepSize / Marshal.SizeOf<T>() * lengthInVectors;
            var table = new TestTableSse2<T>(new T[length], new T[length], new T[length], new T[length]);
            table.Initialize();
            return table;
        }

        public TestTableSse2(T[] a, T[] b, T[] c, T[] d)
        {
            inArray1 = a;
            inArray2 = b;
            outArray = c;
            checkArray = d;
            _scalarStepSize = Marshal.SizeOf<T>();
            _index = 0;
            _inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            _inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            _outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            _checkHandle = GCHandle.Alloc(checkArray, GCHandleType.Pinned);
            Initialize();
        }

        public void Initialize()
        {
            Random random = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x00000000ffffffffl)));
            if (inArray1 is double[])
            {
                var array1 = inArray1 as double[];
                var array2 = inArray2 as double[];
                for (int i = 0; i < inArray1.Length; i++)
                {
                    array1[i] = random.NextDouble() * random.Next();
                    array2[i] = random.NextDouble() * random.Next();
                }
            }
            else if (inArray1 is float[])
            {
                var arrayFloat1 = inArray1 as float[];
                var arrayFloat2 = inArray2 as float[];
                for (int i = 0; i < inArray1.Length; i++)
                {
                    arrayFloat1[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                    arrayFloat2[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                }
            }
            else
            {
                random.NextBytes(new Span<byte>(InArray1Ptr, inArray1.Length * _scalarStepSize));
                random.NextBytes(new Span<byte>(InArray2Ptr, inArray1.Length * _scalarStepSize));
            }
        }

        public bool CheckResult(CheckMethod<T> check)
        {
            bool result = true;
            for (int i = 0; i < inArray1.Length; i++)
            {
                if (!check(inArray1[i], inArray2[i], outArray[i], ref checkArray[i]))
                {
                    result = false;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _inHandle1.Free();
            _inHandle2.Free();
            _outHandle.Free();
            _checkHandle.Free();
        }
    }

    public unsafe struct TestTableSse2<T, U> : IDisposable where T : struct where U : struct
    {
        private const int _stepSize = 16;
        private int _tSize;

        private GCHandle _inHandle1;
        private GCHandle _inHandle2;
        private GCHandle _outHandle;
        private GCHandle _checkHandle;

        private int _index;

        public T[] inArray1;
        public T[] inArray2;
        public U[] outArray;
        public U[] checkArray;

        public void* InArray1Ptr => _inHandle1.AddrOfPinnedObject().ToPointer();
        public void* InArray2Ptr => _inHandle2.AddrOfPinnedObject().ToPointer();
        public void* OutArrayPtr => _outHandle.AddrOfPinnedObject().ToPointer();
        public void* CheckArrayPtr => _checkHandle.AddrOfPinnedObject().ToPointer();

        public Vector128<T> Vector1 => Unsafe.Read<Vector128<T>>((byte*)InArray1Ptr + (_index * _stepSize));
        public Vector128<T> Vector2 => Unsafe.Read<Vector128<T>>((byte*)InArray2Ptr + (_index * _stepSize));
        public Vector128<U> Vector3 => Unsafe.Read<Vector128<U>>((byte*)OutArrayPtr + (_index * _stepSize));
        public Vector128<U> Vector4 => Unsafe.Read<Vector128<U>>((byte*)CheckArrayPtr + (_index * _stepSize));

        public int Index { get => _index; set => _index = value; }

        public void SetOutArray(Vector128<T> value, int index = -1)
        {
            index = index < 0 ? _index : index;
            Unsafe.Write((byte*)OutArrayPtr + (index * _stepSize), value);
        }

        public void SetOutArrayU(Vector128<U> value, int index = -1)
        {
            index = index < 0 ? _index : index;
            Unsafe.Write((byte*)OutArrayPtr + (_index * _stepSize), value);
        }

        public (Vector128<T>, Vector128<T>) this[int index]
        {
            get
            {
                _index = index;
                return (Vector1, Vector2);
            }
        }

        public ValueTuple<T, T, T, T> GetQuad4DataPoint(int index)
        {
            var value3 = Unsafe.Read<T>((byte*)OutArrayPtr + (_index * _stepSize));
            var value4 = Unsafe.Read<T>((byte*)CheckArrayPtr + (_index * _stepSize));
            return (inArray1[index], inArray2[index], value3, value4);
        }

        public unsafe ValueTuple<T, T, U, U> GetQuad22DataPoint(int index)
        {
            return (inArray1[index], inArray2[index], outArray[index], checkArray[index]);
        }

        public ValueTuple<T, T, T, T, U, U> GetHexa42DataPoint(int index)
        {
            return (inArray1[index], inArray1[index + 1], inArray2[index], inArray2[index + 1], outArray[index], checkArray[index]);
        }

        public ValueTuple<T, T, U, U, U, U> GetHexa24DataPoint(int index)
        {
            return (inArray1[index], inArray1[index + 1], outArray[index], outArray[index + 1], checkArray[index], checkArray[index + 1]);
        }

        public ValueTuple<T, T, T, T, U, U, U, ValueTuple<U>> GetOcta44DataPoint(int index)
        {
            return new ValueTuple<T, T, T, T, U, U, U, ValueTuple<U>>(inArray1[index], inArray1[index + 2], inArray2[index], inArray2[index + 2],
                outArray[index], outArray[index + 1], checkArray[index], new ValueTuple<U>(checkArray[index + 1]));
        }

        public ValueTuple<ValueTuple<T, T, T, T>, ValueTuple<T, T, T, T>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>> GetCheckMethodSix4DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3]),
                (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3]),
                (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3],
                outArray[index + 4], outArray[index + 5], outArray[index + 6], (outArray[index + 7])),
                (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3],
                checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], (checkArray[index + 7])));
        }

        public ValueTuple<ValueTuple<T, T, T, T, T, T, T , ValueTuple<T>>, ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>> GetCheckMethodSixteen4DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (outArray[index + 8], outArray[index + 9], outArray[index + 10], outArray[index + 11], outArray[index + 12], outArray[index + 13], outArray[index + 14], outArray[index + 15]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]),
                    (checkArray[index + 8], checkArray[index + 9], checkArray[index + 10], checkArray[index + 11], checkArray[index + 12], checkArray[index + 13], checkArray[index + 14], checkArray[index + 15]));
        }

        public ValueTuple<ValueTuple<T, T, T, T>, ValueTuple<T, T, T, T>, ValueTuple<U, U, U, U>, ValueTuple<U, U, U, U>> GetQuad44DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3]));
        }

        public ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), U, U) GetEightOneDataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    outArray[index], checkArray[index]);
        }

        public ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U)) GetOcta88DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]));
        }

        public ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U)) GetHexadecaDataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray1[index + 8], inArray1[index + 9], inArray1[index + 10], inArray1[index + 11], inArray1[index + 12], inArray1[index + 13], inArray1[index + 14], inArray1[index + 15]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (inArray2[index + 8], inArray2[index + 9], inArray2[index + 10], inArray2[index + 11], inArray2[index + 12], inArray2[index + 13], inArray2[index + 14], inArray2[index + 15]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (outArray[index + 8], outArray[index + 9], outArray[index + 10], outArray[index + 11], outArray[index + 12], outArray[index + 13], outArray[index + 14], outArray[index + 15]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]),
                    (checkArray[index + 8], checkArray[index + 9], checkArray[index + 10], checkArray[index + 11], checkArray[index + 12], checkArray[index + 13], checkArray[index + 14], checkArray[index + 15]));
        }

        public static TestTableSse2<T, U> Create(int lengthInVectors, double tSizeMultiplier = 1.0)
        {
           return new TestTableSse2<T, U>(lengthInVectors, tSizeMultiplier);
        }

        public TestTableSse2(int lengthInVectors, double tSizeMultiplier = 1.0, double uSizeMultiplier = 1.0, bool initialize = true)
        {
            _tSize = Marshal.SizeOf<T>();
            int length = _stepSize / _tSize * lengthInVectors;
            inArray1 = new T[(int)(length * (1 / uSizeMultiplier))];
            inArray2 = new T[(int)(length * (1 / uSizeMultiplier))];
            outArray = new U[(int)(length * (1/ tSizeMultiplier))];
            checkArray = new U[(int)(length * (1 / tSizeMultiplier))];
            _index = 0;
            _inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            _inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            _outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            _checkHandle = GCHandle.Alloc(checkArray, GCHandleType.Pinned);
            if (initialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            Initialize(InitMode.Undefined);
        }

        public void Initialize(InitMode mode = InitMode.Undefined)
        {
            if (mode == InitMode.Undefined)
            {
                Random random = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x00000000ffffffffl)));
                if (inArray1 is double[])
                {
                    var array1 = inArray1 as double[];
                    var array2 = inArray2 as double[];
                    for (int i = 0; i < array1.Length; i++)
                    {
                        array1[i] = random.NextDouble() * random.Next();
                        array2[i] = random.NextDouble() * random.Next();
                    }
                }
                else if (inArray1 is float[])
                {
                    var arrayFloat1 = inArray1 as float[];
                    var arrayFloat2 = inArray2 as float[];
                    for (int i = 0; i < arrayFloat1.Length; i++)
                    {
                        arrayFloat1[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                        arrayFloat2[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                    }
                }
                else
                {
                    random.NextBytes(new Span<byte>(((byte*)InArray1Ptr), inArray1.Length * _tSize));
                    random.NextBytes(new Span<byte>(((byte*)InArray2Ptr), inArray2.Length * _tSize));
                }
            }
            else if (mode == InitMode.NumberFirstVectors)
            {
                InitializeWithVectorNumbering();
            }
        }

        private void InitializeWithVectorNumbering()
        {
            Type baseType = typeof(T);
            if (inArray1 is double[] doubleArray1)
            {
                double[] doubleArray2 = inArray2 as double[];
                for (double i = 0.0, j = 10000.0; i < doubleArray1.Length; i++, j++)
                {
                    doubleArray1[(int)i] = i;
                    doubleArray2[(int)i] = j;
                }
            }
            else if (inArray1 is float[] floatArray1)
            {
                float[] floatArray2 = inArray2 as float[];
                for (float i = 0.0f, j = 10000.0f; i < floatArray1.Length; i++, j++)
                {
                    floatArray1[(int)i] = i;
                    floatArray2[(int)i] = j;
                }
            }
            else if (inArray1 is byte[] byteArray1)
            {
                byte[] byteArray2 = inArray2 as byte[];
                for (byte i = 0, j = 100; i < byteArray1.Length; i++, j++)
                {
                    byteArray1[i] = i;
                    byteArray2[i] = j;
                }
            }
            else if (inArray1 is sbyte[] sbyteArray1)
            {
                sbyte[] sbyteArray2 = inArray2 as sbyte[];
                for (sbyte i = 0, j = 100; i < sbyteArray1.Length; i++, j++)
                {
                    sbyteArray1[i] = i;
                    sbyteArray2[i] = j;
                }
            }
            else if (inArray1 is short[] shortArray1)
            {
                short[] shortArray2 = inArray2 as short[];
                for (short i = 0, j = 10000; i < shortArray1.Length; i++, j++)
                {
                    shortArray1[i] = i;
                    shortArray2[i] = j;
                }

            }
            else if (inArray1 is ushort[] ushortArray1)
            {
                ushort[] ushortArray2 = inArray2 as ushort[];
                for (ushort i = 0, j = 10000; i < ushortArray1.Length; i++, j++)
                {
                    ushortArray1[i] = i;
                    ushortArray2[i] = j;
                }
            }
            else if (inArray1 is int[] intArray1)
            {
                int[] intArray2 = inArray2 as int[];
                for (int i = 0, j = 10000; i < intArray1.Length; i++, j++)
                {
                    intArray1[i] = i;
                    intArray2[i] = j;
                }
            }
            else if (inArray1 is uint[] uintArray1)
            {
                uint[] uintArray2 = inArray2 as uint[];
                for (uint i = 0, j = 10000; i < uintArray1.Length; i++, j++)
                {
                    uintArray1[i] = i;
                    uintArray2[i] = j;
                }
            }
            else if (inArray1 is long[] longArray1)
            {
                long[] longArray2 = inArray2 as long[];
                for (long i = 0, j = 10000; i < longArray1.Length; i++, j++)
                {
                    longArray1[i] = i;
                    longArray2[i] = j;
                }
            }
            else if (inArray1 is ulong[] ulongArray1)
            {
                ulong[] ulongArray2 = inArray2 as ulong[];
                for (uint i = 0, j = 10000; i < ulongArray1.Length; i++, j++)
                {
                    ulongArray1[i] = i;
                    ulongArray2[i] = j;
                }
            }
        }

        public bool CheckResult(CheckMethodTwo<T, U> check)
        {
            bool result = true;
            for (int i = 0; i < inArray1.Length; i++)
            {
                if (!check(inArray1[i], inArray2[i], outArray[i], ref checkArray[i]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckResult(CheckMethodThree<T, U> check)
        {
            bool result = true;
            for (int i = 0; i < inArray1.Length - 1; i+=2)
            {
                if (!check(inArray1[i], inArray1[i+1], inArray2[i], inArray2[i+1], outArray[i], ref checkArray[i]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckPackSaturate(CheckMethodFour<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; j < outArray.Length - 1 && i < inArray1.Length; i++, j += 2)
            {
                if (!check(inArray1[i], inArray2[i], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }


        public bool CheckConvertDoubleToVector128Single(CheckMethodFour<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; j < outArray.Length - 3 && i < inArray1.Length - 1; i += 2, j += 4)
            {
                if (!check(inArray1[i], inArray1[i + 1], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckConvertInt32ToVector128Single(CheckMethodFour<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 3; i += 2, j += 2)
            {
                if (!check(inArray1[i], inArray1[i + 1], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckConvertDoubleToVector128Int32(CheckMethodFour<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 1 && j < outArray.Length - 3; i += 2, j += 4)
            {
                if (!check(inArray1[i], inArray1[i + 1], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckConvertToVector128Double(CheckMethodFour<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 3; i += 4, j += 2)
            {
                if (!check(inArray1[i], inArray1[i + 1], outArray[j], outArray[j+1], ref checkArray[j], ref checkArray[j+1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckUnpack(CheckMethodFourTFourU<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; j < outArray.Length - 3 && i < inArray1.Length - 3; i += 4, j += 4)
            {
                if (!check(
                    (inArray1[i], inArray1[i+1], inArray1[i+2], inArray1[i+3]),
                    (inArray2[i], inArray2[i + 1], inArray2[i + 2], inArray2[i + 3]),
                    (outArray[j], outArray[j + 1], outArray[j + 2], outArray[j + 3]),
                    ref checkArray[j], ref checkArray[j + 1], ref checkArray[j + 2], ref checkArray[j + 3]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckMultiplyUInt32ToUInt64(CheckMethodFive<T, U> check)
        {
            bool result = true;
            int i = 0, j = 0;
            for (; i < inArray1.Length - 3 && j < outArray.Length - 1; i += 4, j += 2)
            {
                if (!check(inArray1[i], inArray1[i + 2], inArray2[i], inArray2[i + 2], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckUnpackHiDouble(CheckMethodFive<T, U> check)
        {
            bool result = true;
            int i = 0, j = 0;
            for (; i < inArray1.Length - 1 && j < outArray.Length - 1; i += 2, j += 2)
            {
                if (!check(inArray1[i], inArray1[i + 1], inArray2[i], inArray2[i + 1], outArray[j], outArray[j + 1], ref checkArray[j], ref checkArray[j + 1]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckMultiplyHorizontalAdd(CheckMethodThree<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 1; i += 2, j++)
            {
                if (!check(inArray1[i], inArray1[i + 1], inArray2[i], inArray2[i + 1], outArray[j], ref checkArray[j]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckPackSaturate(CheckMethodSix<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 3 & j < outArray.Length - 7; i += 4, j += 8)
            {
                bool test = check(
                    (inArray1[i], inArray1[i+1], inArray1[i+2], inArray1[i+3]),
                    (inArray2[i], inArray2[i+1], inArray2[i+2], inArray2[i+3]),
                    (outArray[j], outArray[j+1], outArray[j+2], outArray[j+3],
                    outArray[j+4], outArray[j+5], outArray[j+6], outArray[j+7]),
                    ref checkArray[j], ref checkArray[j+1], ref checkArray[j+2], ref checkArray[j+3],
                    ref checkArray[j+4], ref checkArray[j+5], ref checkArray[j+6], ref checkArray[j+7]
                );

                if (!test)
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckResult(CheckMethodEightOne<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 7 & j < outArray.Length; i += 8, j += 1)
            {
                Span<T> x = new Span<T>(inArray1, i, 8);
                Span<T> y = new Span<T>(inArray2, i, 8);

                if (!check(x, y, outArray[j], ref checkArray[j]))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckUnpack(CheckMethodEightOfTEightOfU<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 7 & j < outArray.Length - 7; i += 8, j += 8)
            {
                bool test = check(
                    (inArray1[i], inArray1[i + 1], inArray1[i + 2], inArray1[i + 3], inArray1[i + 4], inArray1[i + 5], inArray1[i + 6], inArray1[i + 7]),
                    (inArray2[i], inArray2[i + 1], inArray2[i + 2], inArray2[i + 3], inArray2[i + 4], inArray2[i + 5], inArray2[i + 6], inArray2[i + 7]),
                    (outArray[j], outArray[j + 1], outArray[j + 2], outArray[j + 3], outArray[j + 4], outArray[j + 5], outArray[j + 6], outArray[j + 7]),
                    ref checkArray[j], ref checkArray[j + 1], ref checkArray[j + 2], ref checkArray[j + 3],
                    ref checkArray[j + 4], ref checkArray[j + 5], ref checkArray[j + 6], ref checkArray[j + 7]
                );

                if (!test)
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckPackSaturate(CheckMethodSixteen<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 7 & j < outArray.Length - 15; i += 8, j += 16)
            {
                bool test = check(
                    (inArray1[i], inArray1[i + 1], inArray1[i + 2], inArray1[i + 3], inArray1[i + 4], inArray1[i + 5], inArray1[i + 6], inArray1[i + 7]),
                    (inArray2[i], inArray2[i + 1], inArray2[i + 2], inArray2[i + 3], inArray2[i + 4], inArray2[i + 5], inArray2[i + 6], inArray2[i + 7]),
                    (outArray[j], outArray[j + 1], outArray[j + 2], outArray[j + 3], outArray[j + 4], outArray[j + 5], outArray[j + 6], outArray[j + 7]),
                    (outArray[j + 8], outArray[j + 9], outArray[j + 10], outArray[j + 11], outArray[j + 12], outArray[j + 13], outArray[j + 14], outArray[j + 15]),
                    ref checkArray[j], ref checkArray[j + 1], ref checkArray[j + 2], ref checkArray[j + 3],
                    ref checkArray[j + 4], ref checkArray[j + 5], ref checkArray[j + 6], ref checkArray[j + 7],
                    ref checkArray[j + 8], ref checkArray[j + 9], ref checkArray[j + 10], ref checkArray[j + 11],
                    ref checkArray[j + 12], ref checkArray[j + 13], ref checkArray[j + 14], ref checkArray[j + 15]
                );

                if (!test)
                {
                    result = false;
                }
            }
            return result;
        }

        public bool CheckUnpack(CheckMethodSixteenOfAll<T, U> check)
        {
            bool result = true;
            for (int i = 0, j = 0; i < inArray1.Length - 15 & j < outArray.Length - 15; i += 16, j += 16)
            {
                bool test = check(
                    (inArray1[i], inArray1[i + 1], inArray1[i + 2], inArray1[i + 3], inArray1[i + 4], inArray1[i + 5], inArray1[i + 6], inArray1[i + 7]),
                    (inArray1[i + 8], inArray1[i + 9], inArray1[i + 10], inArray1[i + 11], inArray1[i + 12], inArray1[i + 13], inArray1[i + 14], inArray1[i + 15]),
                    (inArray2[i], inArray2[i + 1], inArray2[i + 2], inArray2[i + 3], inArray2[i + 4], inArray2[i + 5], inArray2[i + 6], inArray2[i + 7]),
                    (inArray2[i + 8], inArray2[i + 9], inArray2[i + 10], inArray2[i + 11], inArray2[i + 12], inArray2[i + 13], inArray2[i + 14], inArray2[i + 15]),
                    (outArray[j], outArray[j + 1], outArray[j + 2], outArray[j + 3], outArray[j + 4], outArray[j + 5], outArray[j + 6], outArray[j + 7]),
                    (outArray[j + 8], outArray[j + 9], outArray[j + 10], outArray[j + 11], outArray[j + 12], outArray[j + 13], outArray[j + 14], outArray[j + 15]),
                    ref checkArray[j], ref checkArray[j + 1], ref checkArray[j + 2], ref checkArray[j + 3],
                    ref checkArray[j + 4], ref checkArray[j + 5], ref checkArray[j + 6], ref checkArray[j + 7],
                    ref checkArray[j + 8], ref checkArray[j + 9], ref checkArray[j + 10], ref checkArray[j + 11],
                    ref checkArray[j + 12], ref checkArray[j + 13], ref checkArray[j + 14], ref checkArray[j + 15]
                );

                if (!test)
                {
                    result = false;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _inHandle1.Free();
            _inHandle2.Free();
            _outHandle.Free();
            _checkHandle.Free();
        }
    }

    public unsafe struct TestTableTuvSse2<T, U, V> : IDisposable where T : struct where U : struct where V : struct
    {
        private const int _stepSize = 16;
        private int _tSize;

        private GCHandle _inHandle1;
        private GCHandle _inHandle2;
        private GCHandle _outHandle;
        private GCHandle _checkHandle;

        private int _index;

        public T[] inArray1;
        public V[] inArray2;
        public U[] outArray;
        public U[] checkArray;

        public void* InArray1Ptr => _inHandle1.AddrOfPinnedObject().ToPointer();
        public void* InArray2Ptr => _inHandle2.AddrOfPinnedObject().ToPointer();
        public void* OutArrayPtr => _outHandle.AddrOfPinnedObject().ToPointer();
        public void* CheckArrayPtr => _checkHandle.AddrOfPinnedObject().ToPointer();

        public Vector128<T> Vector1 => Unsafe.Read<Vector128<T>>((byte*)InArray1Ptr + (_index * _stepSize));
        public Vector128<V> Vector2 => Unsafe.Read<Vector128<V>>((byte*)InArray2Ptr + (_index * _stepSize));
        public Vector128<U> Vector3 => Unsafe.Read<Vector128<U>>((byte*)OutArrayPtr + (_index * _stepSize));
        public Vector128<U> Vector4 => Unsafe.Read<Vector128<U>>((byte*)CheckArrayPtr + (_index * _stepSize));

        public int Index { get => _index; set => _index = value; }

        public void SetOutArray(Vector128<T> value, int index = -1)
        {
            index = index < 0 ? _index : index;
            Unsafe.Write((byte*)OutArrayPtr + (_index * _stepSize), value);
        }

        public Vector128<T> this[int index]
        {
            get
            {
                _index = index;
                return Vector1;
            }
        }

        public ValueTuple<T, V, U, U> GetQuad4DataPoint(int index)
        {
            var value3 = Unsafe.Read<U>((byte*)OutArrayPtr + (_index * _stepSize));
            var value4 = Unsafe.Read<U>((byte*)CheckArrayPtr + (_index * _stepSize));
            return (inArray1[index], inArray2[index], value3, value4);
        }

        public unsafe ValueTuple<T, V, U, U> GetQuad22DataPoint(int index)
        {
            return (inArray1[index], inArray2[index], outArray[index], checkArray[index]);
        }

        public ValueTuple<T, T, V, V, U, U> GetHexa42DataPoint(int index)
        {
            return (inArray1[index], inArray1[index + 1], inArray2[index], inArray2[index + 1], outArray[index], checkArray[index]);
        }

        public ValueTuple<T, T, U, U, U, U> GetHexa24DataPoint(int index)
        {
            return (inArray1[index], inArray1[index + 1], outArray[index], outArray[index + 1], checkArray[index], checkArray[index + 1]);
        }

        public ValueTuple<T, T, V, V, U, U, U, ValueTuple<U>> GetOcta44DataPoint(int index)
        {
            return new ValueTuple<T, T, V, V, U, U, U, ValueTuple<U>>(inArray1[index], inArray1[index + 2], inArray2[index], inArray2[index + 2],
                outArray[index], outArray[index + 1], checkArray[index], new ValueTuple<U>(checkArray[index + 1]));
        }

        public ValueTuple<ValueTuple<T, T, T, T>, ValueTuple<V, V, V, V>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>> GetCheckMethodSix4DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3]),
                (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3]),
                (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3],
                outArray[index + 4], outArray[index + 5], outArray[index + 6], (outArray[index + 7])),
                (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3],
                checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], (checkArray[index + 7])));
        }

        public ValueTuple<ValueTuple<T, T, T, T, T, T, T, ValueTuple<T>>, ValueTuple<V, V, V, V, V, V, V, ValueTuple<V>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>, ValueTuple<U, U, U, U, U, U, U, ValueTuple<U>>> GetCheckMethodSixteen4DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (outArray[index + 8], outArray[index + 9], outArray[index + 10], outArray[index + 11], outArray[index + 12], outArray[index + 13], outArray[index + 14], outArray[index + 15]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]),
                    (checkArray[index + 8], checkArray[index + 9], checkArray[index + 10], checkArray[index + 11], checkArray[index + 12], checkArray[index + 13], checkArray[index + 14], checkArray[index + 15]));
        }

        public ValueTuple<ValueTuple<T, T, T, T>, ValueTuple<V, V, V, V>, ValueTuple<U, U, U, U>, ValueTuple<U, U, U, U>> GetQuad44DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3]));
        }

        public ((T, T, T, T, T, T, T, T), (V, V, V, V, V, V, V, V), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U)) GetOcta88DataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]));
        }

        public ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (V, V, V, V, V, V, V, V), (V, V, V, V, V, V, V, V), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U)) GetHexadecaDataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    (inArray1[index + 8], inArray1[index + 9], inArray1[index + 10], inArray1[index + 11], inArray1[index + 12], inArray1[index + 13], inArray1[index + 14], inArray1[index + 15]),
                    (inArray2[index], inArray2[index + 1], inArray2[index + 2], inArray2[index + 3], inArray2[index + 4], inArray2[index + 5], inArray2[index + 6], inArray2[index + 7]),
                    (inArray2[index + 8], inArray2[index + 9], inArray2[index + 10], inArray2[index + 11], inArray2[index + 12], inArray2[index + 13], inArray2[index + 14], inArray2[index + 15]),
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (outArray[index + 8], outArray[index + 9], outArray[index + 10], outArray[index + 11], outArray[index + 12], outArray[index + 13], outArray[index + 14], outArray[index + 15]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]),
                    (checkArray[index + 8], checkArray[index + 9], checkArray[index + 10], checkArray[index + 11], checkArray[index + 12], checkArray[index + 13], checkArray[index + 14], checkArray[index + 15]));
        }

        public static TestTableTuvSse2<T, U, V> Create(int lengthInVectors, double tSizeMultiplier = 1.0)
        {
            return new TestTableTuvSse2<T, U, V>(lengthInVectors, tSizeMultiplier);
        }

        public TestTableTuvSse2(int lengthInVectors, double tSizeMultiplier = 1.0, double uSizeMultiplier = 1.0, bool initialize = true)
        {
            _tSize = Marshal.SizeOf<T>();
            int length = _stepSize / _tSize * lengthInVectors;
            inArray1 = new T[(int)(length * (1 / uSizeMultiplier))];
            inArray2 = new V[(int)(length * (1 / uSizeMultiplier))];
            outArray = new U[(int)(length * (1 / tSizeMultiplier))];
            checkArray = new U[(int)(length * (1 / tSizeMultiplier))];
            _index = 0;
            _inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            _inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            _outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            _checkHandle = GCHandle.Alloc(checkArray, GCHandleType.Pinned);
            if (initialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            Initialize(InitMode.Undefined);
        }

        public void Initialize(InitMode mode = InitMode.Undefined)
        {
            if (mode == InitMode.Undefined)
            {
                Random random = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x00000000ffffffffl)));
                if (inArray1 is double[])
                {
                    var array1 = inArray1 as double[];
                    for (int i = 0; i < array1.Length; i++)
                    {
                        array1[i] = random.NextDouble() * random.Next();
                    }
                }
                else if (inArray1 is float[])
                {
                    var arrayFloat1 = inArray1 as float[];
                    for (int i = 0; i < arrayFloat1.Length; i++)
                    {
                        arrayFloat1[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                    }
                }
                else
                {
                    random.NextBytes(new Span<byte>(((byte*)InArray1Ptr), inArray1.Length * _tSize));
                }
            }
            else if (mode == InitMode.NumberFirstVectors)
            {
                InitializeWithVectorNumbering();
            }
        }

        private void InitializeWithVectorNumbering()
        {
            Type baseType = typeof(T);
            if (inArray1 is double[] doubleArray1)
            {
                for (double i = 0.0, j = 10000.0; i < doubleArray1.Length; i++, j++)
                {
                    doubleArray1[(int)i] = i;
                }
            }
            else if (inArray1 is float[] floatArray1)
            {
                for (float i = 0.0f, j = 10000.0f; i < floatArray1.Length; i++, j++)
                {
                    floatArray1[(int)i] = i;
                }
            }
            else if (inArray1 is byte[] byteArray1)
            {
                for (byte i = 0, j = 100; i < byteArray1.Length; i++, j++)
                {
                    byteArray1[i] = i;
                }
            }
            else if (inArray1 is sbyte[] sbyteArray1)
            {
                for (sbyte i = 0, j = 100; i < sbyteArray1.Length; i++, j++)
                {
                    sbyteArray1[i] = i;
                }
            }
            else if (inArray1 is short[] shortArray1)
            {
                for (short i = 0, j = 10000; i < shortArray1.Length; i++, j++)
                {
                    shortArray1[i] = i;
                }

            }
            else if (inArray1 is ushort[] ushortArray1)
            {
                for (ushort i = 0, j = 10000; i < ushortArray1.Length; i++, j++)
                {
                    ushortArray1[i] = i;
                }
            }
            else if (inArray1 is int[] intArray1)
            {
                for (int i = 0, j = 10000; i < intArray1.Length; i++, j++)
                {
                    intArray1[i] = i;
                }
            }
            else if (inArray1 is uint[] uintArray1)
            {
                for (uint i = 0, j = 10000; i < uintArray1.Length; i++, j++)
                {
                    uintArray1[i] = i;
                }
            }
            else if (inArray1 is long[] longArray1)
            {
                for (long i = 0, j = 10000; i < longArray1.Length; i++, j++)
                {
                    longArray1[i] = i;
                }
            }
            else if (inArray1 is ulong[] ulongArray1)
            {
                for (uint i = 0, j = 10000; i < ulongArray1.Length; i++, j++)
                {
                    ulongArray1[i] = i;
                }
            }
        }

        public bool CheckResult(CheckMethodTwo<T, U, V> check)
        {
            bool result = true;
            for (int i = 0; i < inArray1.Length; i++)
            {
                if (!check(inArray1[i], inArray2[i], outArray[i], ref checkArray[i]))
                {
                    result = false;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _inHandle1.Free();
            _inHandle2.Free();
            _outHandle.Free();
            _checkHandle.Free();
        }
    }

    public unsafe struct TestTableImmSse2<T, U, V> : IDisposable where T : struct where U : struct where V : struct
    {
        private const int _stepSize = 16;
        private int _tSize;

        private GCHandle _inHandle1;
        private GCHandle _inHandle2;
        private GCHandle _immHandle;
        private GCHandle _outHandle;
        private GCHandle _checkHandle;

        private int _index;

        public T[] inArray1;
        public T[] inArray2;
        public V[] immArray;
        public U[] outArray;
        public U[] checkArray;

        public void* InArray1Ptr => _inHandle1.AddrOfPinnedObject().ToPointer();
        public void* InArray2Ptr => _inHandle2.AddrOfPinnedObject().ToPointer();
        public void* ImmArrayPtr => _inHandle2.AddrOfPinnedObject().ToPointer();
        public void* OutArrayPtr => _outHandle.AddrOfPinnedObject().ToPointer();
        public void* CheckArrayPtr => _checkHandle.AddrOfPinnedObject().ToPointer();

        public Vector128<T> Vector1 => Unsafe.Read<Vector128<T>>((byte*)InArray1Ptr + (_index * _stepSize));
        public T Value => Unsafe.Read<T>((byte*)InArray2Ptr + (_index));
        public V Immediate => Unsafe.Read<V>((byte*)ImmArrayPtr + (_index));
        public Vector128<U> Vector3 => Unsafe.Read<Vector128<U>>((byte*)OutArrayPtr + (_index * _stepSize));
        public Vector128<U> Vector4 => Unsafe.Read<Vector128<U>>((byte*)CheckArrayPtr + (_index * _stepSize));

        public int Index { get => _index; set => _index = value; }

        public void SetOutArray(Vector128<T> value, int index = -1)
        {
            index = index < 0 ? _index : index;
            Unsafe.Write((byte*)OutArrayPtr + (_index * _stepSize), value);
        }

        public (Vector128<T>, T) this[int index]
        {
            get
            {
                _index = index;
                return (Vector1, Value);
            }
        }

        public ((T, T, T, T, T, T, T, T), T, V, (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U)) GetOctaImmDataPoint(int index)
        {
            return ((inArray1[index], inArray1[index + 1], inArray1[index + 2], inArray1[index + 3], inArray1[index + 4], inArray1[index + 5], inArray1[index + 6], inArray1[index + 7]),
                    inArray2[index/8], immArray[index/8],
                    (outArray[index], outArray[index + 1], outArray[index + 2], outArray[index + 3], outArray[index + 4], outArray[index + 5], outArray[index + 6], outArray[index + 7]),
                    (checkArray[index], checkArray[index + 1], checkArray[index + 2], checkArray[index + 3], checkArray[index + 4], checkArray[index + 5], checkArray[index + 6], checkArray[index + 7]));
        }

        public static TestTableImmSse2<T, U, V> Create(int lengthInVectors, double tSizeMultiplier = 1.0)
        {
            return new TestTableImmSse2<T, U, V>(lengthInVectors, tSizeMultiplier);
        }

        public TestTableImmSse2(int lengthInVectors, double tSizeMultiplier = 1.0, bool initialize = true)
        {
            _tSize = Marshal.SizeOf<T>();
            int length = _stepSize / _tSize * lengthInVectors;
            inArray1 = new T[length];
            inArray2 = new T[lengthInVectors];
            immArray = new V[lengthInVectors];
            outArray = new U[(int)(length * (1 / tSizeMultiplier))];
            checkArray = new U[(int)(length * (1 / tSizeMultiplier))];
            _index = 0;
            _inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            _inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            _immHandle = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            _outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            _checkHandle = GCHandle.Alloc(checkArray, GCHandleType.Pinned);
            if (initialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            Initialize(InitMode.Undefined);
        }

        public void Initialize(InitMode mode = InitMode.Undefined)
        {
            if (mode == InitMode.Undefined)
            {
                Random random = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x00000000ffffffffl)));
                if (inArray1 is double[])
                {
                    var array1 = inArray1 as double[];
                    for (int i = 0; i < array1.Length; i++)
                    {
                        array1[i] = random.NextDouble() * random.Next();
                    }
                }
                else if (inArray1 is float[])
                {
                    var arrayFloat1 = inArray1 as float[];
                    for (int i = 0; i < arrayFloat1.Length; i++)
                    {
                        arrayFloat1[i] = (float)(random.NextDouble() * random.Next(ushort.MaxValue));
                    }
                }
                else
                {
                    random.NextBytes(new Span<byte>(((byte*)InArray1Ptr), inArray1.Length * _tSize));
                }
            }
            else if (mode == InitMode.NumberFirstVectors)
            {
                InitializeWithVectorNumbering();
            }
        }

        private void InitializeWithVectorNumbering()
        {
            Type baseType = typeof(T);
            if (inArray1 is double[] doubleArray1)
            {
                for (double i = 0.0, j = 10000.0; i < doubleArray1.Length; i++, j++)
                {
                    doubleArray1[(int)i] = i;
                }
            }
            else if (inArray1 is float[] floatArray1)
            {
                for (float i = 0.0f, j = 10000.0f; i < floatArray1.Length; i++, j++)
                {
                    floatArray1[(int)i] = i;
                }
            }
            else if (inArray1 is byte[] byteArray1)
            {
                for (byte i = 0, j = 100; i < byteArray1.Length; i++, j++)
                {
                    byteArray1[i] = i;
                }
            }
            else if (inArray1 is sbyte[] sbyteArray1)
            {
                for (sbyte i = 0, j = 100; i < sbyteArray1.Length; i++, j++)
                {
                    sbyteArray1[i] = i;
                }
            }
            else if (inArray1 is short[] shortArray1)
            {
                for (short i = 0, j = 10000; i < shortArray1.Length; i++, j++)
                {
                    shortArray1[i] = i;
                }

            }
            else if (inArray1 is ushort[] ushortArray1)
            {
                for (ushort i = 0, j = 10000; i < ushortArray1.Length; i++, j++)
                {
                    ushortArray1[i] = i;
                }
            }
            else if (inArray1 is int[] intArray1)
            {
                for (int i = 0, j = 10000; i < intArray1.Length; i++, j++)
                {
                    intArray1[i] = i;
                }
            }
            else if (inArray1 is uint[] uintArray1)
            {
                for (uint i = 0, j = 10000; i < uintArray1.Length; i++, j++)
                {
                    uintArray1[i] = i;
                }
            }
            else if (inArray1 is long[] longArray1)
            {
                for (long i = 0, j = 10000; i < longArray1.Length; i++, j++)
                {
                    longArray1[i] = i;
                }
            }
            else if (inArray1 is ulong[] ulongArray1)
            {
                for (uint i = 0, j = 10000; i < ulongArray1.Length; i++, j++)
                {
                    ulongArray1[i] = i;
                }
            }
        }

        public bool CheckResultImm(CheckMethodEightImm<T, U, V> check)
        {
            bool result = true;
            for (int i = 0; i < inArray1.Length; i++)
            {
                int elNo = _stepSize / _tSize;
                if (!check(
                    new Span<T>(inArray1, Index * elNo, elNo),
                    inArray2[i], immArray[i],
                    new Span<U>(outArray, Index * elNo, elNo),
                    new Span<U>(checkArray, Index * elNo, elNo)))
                {
                    result = false;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _inHandle1.Free();
            _inHandle2.Free();
            _immHandle.Free();
            _outHandle.Free();
            _checkHandle.Free();
        }
    }

    public enum SpecialCheck
    {
        Undefined = 0,
        Sse2MultiplyHorizontalAdd = 1,
    }

    internal static partial class Program
    {
        private static void PrintErrorHeaderTu<T>(string functionName, string testFuncString) where T : struct
        {
            Console.WriteLine($"{typeof(Sse2)}.{functionName} failed on {typeof(T)}:");
            Console.WriteLine($"Test function: {testFuncString}");
            Console.WriteLine($"{ typeof(Sse2)}.{functionName} test tuples:");
        }

        private static void PrintErrorHeaderTuv<T, V>(string functionName, string testFuncString) where T : struct where V : struct
        {
            Console.WriteLine($"{typeof(Sse2)}.{functionName} failed on {typeof(T)}.{typeof(V)}:");
            Console.WriteLine($"Test function: {testFuncString}");
            Console.WriteLine($"{ typeof(Sse2)}.{functionName} test tuples:");
        }

        private static void PrintError<T>(TestTableSse2<T> testTable, string functionName = "", string testFuncString = "",
            CheckMethod<T> check = null) where T : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0; i < testTable.outArray.Length; i++)
            {
                (T, T, T, T) item = testTable.GetDataPoint(i);
                Console.Write(
                    $"({(item)})" +
                    (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, ref item.Item4)}, " : ", "));
            }
            Console.WriteLine("\n");
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodTwo<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0; i < testTable.outArray.Length; i++)
            {
                (T, T, U, U) item = testTable.GetQuad22DataPoint(i);
                Console.Write($"({(item)})" + (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, ref item.Item4)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U, V>(TestTableTuvSse2<T, U, V> testTable, string functionName = "", string testFuncString = "",
            CheckMethodTwo<T, U, V> check = null) where T : struct where U : struct where V : struct
        {
            PrintErrorHeaderTuv<T,V>(functionName, testFuncString);
            for (int i = 0; i < testTable.outArray.Length; i++)
            {
                (T, V, U, U) item = testTable.GetQuad22DataPoint(i);
                Console.Write( $"({item})" + (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, ref item.Item4)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodThree<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0; i < testTable.inArray1.Length - 1; i += 2)
            {
                (T, T, T, T, U, U) item = testTable.GetHexa42DataPoint(i);
                Console.Write(
                    $"({(item)})" +
                    (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, item.Item4, item.Item5, ref item.Item6)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodFour<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0; i < testTable.inArray1.Length - 1; i += 2)
            {
                (T, T, U, U, U, U) item = testTable.GetHexa24DataPoint(i);
                Console.Write(
                    $"({(item)})" +
                    (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, item.Item4, ref item.Item5, ref item.Item6)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodFourTFourU<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0; i < testTable.inArray1.Length - 1; i += 2)
            {
                // ((T, T, T, T), (T, T, T, T), (U, U, U, U), (U, U, U, U))
                var item = testTable.GetQuad44DataPoint(i);
                Console.Write(
                    $"(x{item.Item1}, y{item.Item2}, z{item.Item3}, a{item.Item4} " +
                    (check != null ? (string) $"->{check(item.Item1, item.Item2, item.Item3, ref item.Item4.Item1, ref item.Item4.Item1, ref item.Item4.Item1, ref item.Item4.Item1)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodFive<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // (T, T, T, T, U, U, U, U)
                var item = testTable.GetOcta44DataPoint(i);
                Console.Write(
                    $"({(item)})" +
                    (check != null ? $"->{check(item.Item1, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6, ref item.Item7, ref item.Item8)}, " : ", "));
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodSix<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // ((T, T, T, T), (T, T, T, T), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U))
                var item = testTable.GetCheckMethodSix4DataPoint(i);
                Console.Write($"( x({item.Item1}), y({item.Item2}), z({item.Item3}), a({item.Item4}))");
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodEightOne<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), U, U)
                var item = testTable.GetCheckMethodSix4DataPoint(i);
                Console.Write($"( x({item.Item1}), y({item.Item2}), z({item.Item3}), a({item.Item4}))");
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U, V>(TestTableImmSse2<T, U, V> testTable, string functionName = "", string testFuncString = "",
            CheckMethodEightImm<T, U, V> check = null) where T : struct where U : struct where V : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 7 && j < testTable.inArray2.Length; i += 8, j += 1)
            {
                // ((T, T, T, T, T, T, T, T), T, V, (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U))
                var item = testTable.GetOctaImmDataPoint(i);
                Console.Write($"({item})");
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodEightOfTEightOfU<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U))
                var item = testTable.GetOcta88DataPoint(i);
                Console.Write($"( x({item.Item1}), y({item.Item2}), z({item.Item3}), a({item.Item4})))");
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodSixteen<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // ((T, T, T, T), (T, T, T, T), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U))
                var item = testTable.GetCheckMethodSixteen4DataPoint(i);
                Console.Write($"( x({item.Item1}), y({item.Item2}), z1({item.Item3}), z2({item.Item4}), a1({item.Item5}), a2({item.Item6}))");
            }
            Console.WriteLine();
        }

        private static void PrintError<T, U>(TestTableSse2<T, U> testTable, string functionName = "", string testFuncString = "",
            CheckMethodSixteenOfAll<T, U> check = null) where T : struct where U : struct
        {
            PrintErrorHeaderTu<T>(functionName, testFuncString);
            for (int i = 0, j = 0; i < testTable.inArray1.Length - 4 && j < testTable.outArray.Length - 2; i += 4, j += 2)
            {
                // ((T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T), (T, T, T, T, T, T, T, T),
                // (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U), (U, U, U, U, U, U, U, U))
                var item = testTable.GetHexadecaDataPoint(i);
                Console.Write($"( x({item.Item1}), y({item.Item2}), z({item.Item3}), a({item.Item4})))");
            }
            Console.WriteLine();
        }
    }
}
