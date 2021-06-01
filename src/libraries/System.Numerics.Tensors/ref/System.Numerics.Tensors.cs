// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Tensors
{
    public static partial class ArrayTensorExtensions
    {
        public static System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this System.Array array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[] array) { throw null; }
        public static System.Numerics.Tensors.SparseTensor<T> ToSparseTensor<T>(this System.Array array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.SparseTensor<T> ToSparseTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.SparseTensor<T> ToSparseTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.SparseTensor<T> ToSparseTensor<T>(this T[] array) { throw null; }
        public static System.Numerics.Tensors.DenseTensor<T> ToTensor<T>(this System.Array array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.DenseTensor<T> ToTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.DenseTensor<T> ToTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static System.Numerics.Tensors.DenseTensor<T> ToTensor<T>(this T[] array) { throw null; }
    }
    public partial class CompressedSparseTensor<T> : System.Numerics.Tensors.Tensor<T>
    {
        public CompressedSparseTensor(System.Memory<T> values, System.Memory<int> compressedCounts, System.Memory<int> indices, int nonZeroCount, System.ReadOnlySpan<int> dimensions, bool reverseStride = false) : base (default(System.Array), default(bool)) { }
        public CompressedSparseTensor(System.ReadOnlySpan<int> dimensions, bool reverseStride = false) : base (default(System.Array), default(bool)) { }
        public CompressedSparseTensor(System.ReadOnlySpan<int> dimensions, int capacity, bool reverseStride = false) : base (default(System.Array), default(bool)) { }
        public int Capacity { get { throw null; } }
        public System.Memory<int> CompressedCounts { get { throw null; } }
        public System.Memory<int> Indices { get { throw null; } }
        public override T this[System.ReadOnlySpan<int> indices] { get { throw null; } set { } }
        public int NonZeroCount { get { throw null; } }
        public System.Memory<T> Values { get { throw null; } }
        public override System.Numerics.Tensors.Tensor<T> Clone() { throw null; }
        public override System.Numerics.Tensors.Tensor<TResult> CloneEmpty<TResult>(System.ReadOnlySpan<int> dimensions) { throw null; }
        public override T GetValue(int index) { throw null; }
        public override System.Numerics.Tensors.Tensor<T> Reshape(System.ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { }
        public override System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
        public override System.Numerics.Tensors.DenseTensor<T> ToDenseTensor() { throw null; }
        public override System.Numerics.Tensors.SparseTensor<T> ToSparseTensor() { throw null; }
    }
    public partial class DenseTensor<T> : System.Numerics.Tensors.Tensor<T>
    {
        public DenseTensor(int length) : base (default(System.Array), default(bool)) { }
        public DenseTensor(System.Memory<T> memory, System.ReadOnlySpan<int> dimensions, bool reverseStride = false) : base (default(System.Array), default(bool)) { }
        public DenseTensor(System.ReadOnlySpan<int> dimensions, bool reverseStride = false) : base (default(System.Array), default(bool)) { }
        public System.Memory<T> Buffer { get { throw null; } }
        public override System.Numerics.Tensors.Tensor<T> Clone() { throw null; }
        public override System.Numerics.Tensors.Tensor<TResult> CloneEmpty<TResult>(System.ReadOnlySpan<int> dimensions) { throw null; }
        protected override void CopyTo(T[] array, int arrayIndex) { }
        public override T GetValue(int index) { throw null; }
        protected override int IndexOf(T item) { throw null; }
        public override System.Numerics.Tensors.Tensor<T> Reshape(System.ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { }
    }
    public partial class SparseTensor<T> : System.Numerics.Tensors.Tensor<T>
    {
        public SparseTensor(System.ReadOnlySpan<int> dimensions, bool reverseStride = false, int capacity = 0) : base (default(System.Array), default(bool)) { }
        public int NonZeroCount { get { throw null; } }
        public override System.Numerics.Tensors.Tensor<T> Clone() { throw null; }
        public override System.Numerics.Tensors.Tensor<TResult> CloneEmpty<TResult>(System.ReadOnlySpan<int> dimensions) { throw null; }
        public override T GetValue(int index) { throw null; }
        public override System.Numerics.Tensors.Tensor<T> Reshape(System.ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { }
        public override System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
        public override System.Numerics.Tensors.DenseTensor<T> ToDenseTensor() { throw null; }
        public override System.Numerics.Tensors.SparseTensor<T> ToSparseTensor() { throw null; }
    }
    public static partial class Tensor
    {
        public static System.Numerics.Tensors.Tensor<T> CreateFromDiagonal<T>(System.Numerics.Tensors.Tensor<T> diagonal) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateFromDiagonal<T>(System.Numerics.Tensors.Tensor<T> diagonal, int offset) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateIdentity<T>(int size) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateIdentity<T>(int size, bool columMajor) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateIdentity<T>(int size, bool columMajor, T oneValue) { throw null; }
    }
    public abstract partial class Tensor<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable
    {
        protected Tensor(System.Array fromArray, bool reverseStride) { }
        protected Tensor(int length) { }
        protected Tensor(System.ReadOnlySpan<int> dimensions, bool reverseStride) { }
        public System.ReadOnlySpan<int> Dimensions { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsReversedStride { get { throw null; } }
        public virtual T this[params int[] indices] { get { throw null; } set { } }
        public virtual T this[System.ReadOnlySpan<int> indices] { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.ReadOnlySpan<int> Strides { get { throw null; } }
        int System.Collections.Generic.ICollection<T>.Count { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        int System.Collections.Generic.IReadOnlyCollection<T>.Count { get { throw null; } }
        T System.Collections.Generic.IReadOnlyList<T>.this[int index] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public abstract System.Numerics.Tensors.Tensor<T> Clone();
        public virtual System.Numerics.Tensors.Tensor<T> CloneEmpty() { throw null; }
        public virtual System.Numerics.Tensors.Tensor<T> CloneEmpty(System.ReadOnlySpan<int> dimensions) { throw null; }
        public virtual System.Numerics.Tensors.Tensor<TResult> CloneEmpty<TResult>() { throw null; }
        public abstract System.Numerics.Tensors.Tensor<TResult> CloneEmpty<TResult>(System.ReadOnlySpan<int> dimensions);
        public static int Compare(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) { throw null; }
        protected virtual bool Contains(T item) { throw null; }
        protected virtual void CopyTo(T[] array, int arrayIndex) { }
        public static bool Equals(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) { throw null; }
        public virtual void Fill(T value) { }
        public string GetArrayString(bool includeWhitespace = true) { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetDiagonal() { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetDiagonal(int offset) { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetTriangle() { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetTriangle(int offset) { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetUpperTriangle() { throw null; }
        public System.Numerics.Tensors.Tensor<T> GetUpperTriangle(int offset) { throw null; }
        public abstract T GetValue(int index);
        protected virtual int IndexOf(T item) { throw null; }
        public abstract System.Numerics.Tensors.Tensor<T> Reshape(System.ReadOnlySpan<int> dimensions);
        public abstract void SetValue(int index, T value);
        public struct Enumerator : System.Collections.Generic.IEnumerator<T>
        {
            public T Current { get; private set; }
            object? System.Collections.IEnumerator.Current => throw null;
            public bool MoveNext() => throw null;
            public void Reset() { }
            public void Dispose() { }
        }
        public Enumerator GetEnumerator() => throw null;
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Contains(T item) { throw null; }
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<T>.IndexOf(T item) { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        public virtual System.Numerics.Tensors.CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
        public virtual System.Numerics.Tensors.DenseTensor<T> ToDenseTensor() { throw null; }
        public virtual System.Numerics.Tensors.SparseTensor<T> ToSparseTensor() { throw null; }
    }
}
