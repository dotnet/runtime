// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace System.Numerics.Tensors
{
    public static class ArrayTensorExtensions
    {
        public static DenseTensor<T> ToTensor<T>(this T[] array) { throw null; }
        public static DenseTensor<T> ToTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static DenseTensor<T> ToTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static DenseTensor<T> ToTensor<T>(this Array array, bool reverseStride = false) { throw null; }
        public static SparseTensor<T> ToSparseTensor<T>(this T[] array) { throw null; }
        public static SparseTensor<T> ToSparseTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static SparseTensor<T> ToSparseTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static SparseTensor<T> ToSparseTensor<T>(this Array array, bool reverseStride = false) { throw null; }
        public static CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[] array) { throw null; }
        public static CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[,] array, bool reverseStride = false) { throw null; }
        public static CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this T[,,] array, bool reverseStride = false) { throw null; }
        public static CompressedSparseTensor<T> ToCompressedSparseTensor<T>(this Array array, bool reverseStride = false) { throw null; }
    }
    public class CompressedSparseTensor<T> : Tensor<T>
    {
        public CompressedSparseTensor(ReadOnlySpan<int> dimensions, bool reverseStride = false) : this(default(ReadOnlySpan<int>), default(int), default(bool)) { throw null; }
        public CompressedSparseTensor(ReadOnlySpan<int> dimensions, int capacity, bool reverseStride = false) : base(default(ReadOnlySpan<int>), default(bool)) { throw null; }
        public CompressedSparseTensor(Memory<T> values, Memory<int> compressedCounts, Memory<int> indices, int nonZeroCount, ReadOnlySpan<int> dimensions, bool reverseStride = false) : base(default(ReadOnlySpan<int>), default(bool)) { throw null; }
        public int Capacity { get { throw null; } }
        public Memory<int> CompressedCounts { get { throw null; } }
        public Memory<int> Indices { get { throw null; } }
        public override T this[ReadOnlySpan<int> indices] { get { throw null; } set { throw null; } }
        public int NonZeroCount { get { throw null; } }
        public Memory<T> Values { get { throw null; } }
        public override Tensor<T> Clone() { throw null; }
        public override Tensor<TResult> CloneEmpty<TResult>(ReadOnlySpan<int> dimensions) { throw null; }
        public override T GetValue(int index) { throw null; }
        public override Tensor<T> Reshape(ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { throw null; }
        public override CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
        public override DenseTensor<T> ToDenseTensor() { throw null; }
        public override SparseTensor<T> ToSparseTensor() { throw null; }
    }
    public class DenseTensor<T> : Tensor<T>
    {
        public DenseTensor(int length) : base(default(int)) { throw null; }
        public DenseTensor(ReadOnlySpan<int> dimensions, bool reverseStride = false) : base(default(ReadOnlySpan<int>), default(bool)) { throw null; }
        public DenseTensor(Memory<T> memory, ReadOnlySpan<int> dimensions, bool reverseStride = false) : base(default(ReadOnlySpan<int>), default(bool)) { throw null; }
        public Memory<T> Buffer { get { throw null; } }
        public override Tensor<T> Clone() { throw null; }
        public override Tensor<TResult> CloneEmpty<TResult>(ReadOnlySpan<int> dimensions) { throw null; }
        public override T GetValue(int index) { throw null; }
        public override Tensor<T> Reshape(ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { throw null; }
        protected override void CopyTo(T[] array, int arrayIndex) { throw null; }
        protected override int IndexOf(T item) { throw null; }
    }
    public class SparseTensor<T> : Tensor<T>
    {
        public SparseTensor(ReadOnlySpan<int> dimensions, bool reverseStride = false, int capacity = 0) : base(default(ReadOnlySpan<int>), default(bool)) { throw null; }
        public int NonZeroCount { get { throw null; } }
        public override Tensor<T> Clone() { throw null; }
        public override Tensor<TResult> CloneEmpty<TResult>(ReadOnlySpan<int> dimensions) { throw null; }
        public override T GetValue(int index) { throw null; }
        public override Tensor<T> Reshape(ReadOnlySpan<int> dimensions) { throw null; }
        public override void SetValue(int index, T value) { throw null; }
        public override DenseTensor<T> ToDenseTensor() { throw null; }
        public override SparseTensor<T> ToSparseTensor() { throw null; }
        public override CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
    }
    public static class Tensor
    {
        public static Tensor<T> CreateIdentity<T>(int size) { throw null; }
        public static Tensor<T> CreateIdentity<T>(int size, bool columMajor) { throw null; }
        public static Tensor<T> CreateIdentity<T>(int size, bool columMajor, T oneValue) { throw null; }
        public static Tensor<T> CreateFromDiagonal<T>(Tensor<T> diagonal) { throw null; }
        public static Tensor<T> CreateFromDiagonal<T>(Tensor<T> diagonal, int offset) { throw null; }
    }
    public abstract class Tensor<T> : IList, IList<T>, IReadOnlyList<T>, IStructuralComparable, IStructuralEquatable
    {
        protected Tensor(int length) { throw null; }
        protected Tensor(ReadOnlySpan<int> dimensions, bool reverseStride) { throw null; }
        protected Tensor(Array fromArray, bool reverseStride) { throw null; }
        public ReadOnlySpan<int> Dimensions { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsReversedStride { get { throw null; } }
        public virtual T this[params int[] indices] { get { throw null; } set { throw null; } }
        public virtual T this[ReadOnlySpan<int> indices] { get { throw null; } set { throw null; } }
        public long Length { get { throw null; } }
        public int Rank { get { throw null; } }
        public ReadOnlySpan<int> Strides { get { throw null; } }
        public static int Compare(Tensor<T> left, Tensor<T> right) { throw null; }
        public static bool Equals(Tensor<T> left, Tensor<T> right) { throw null; }
        public abstract Tensor<T> Clone();
        public virtual Tensor<T> CloneEmpty() { throw null; }
        public virtual Tensor<T> CloneEmpty(ReadOnlySpan<int> dimensions) { throw null; }
        public virtual Tensor<TResult> CloneEmpty<TResult>() { throw null; }
        public abstract Tensor<TResult> CloneEmpty<TResult>(ReadOnlySpan<int> dimensions);
        public virtual void Fill(T value) { throw null; }
        public string GetArrayString(bool includeWhitespace = true) { throw null; }
        public Tensor<T> GetDiagonal() { throw null; }
        public Tensor<T> GetDiagonal(int offset) { throw null; }
        public Tensor<T> GetTriangle() { throw null; }
        public Tensor<T> GetTriangle(int offset) { throw null; }
        public Tensor<T> GetUpperTriangle() { throw null; }
        public Tensor<T> GetUpperTriangle(int offset) { throw null; }
        public abstract T GetValue(int index);
        public abstract Tensor<T> Reshape(ReadOnlySpan<int> dimensions);
        public abstract void SetValue(int index, T value);
        public virtual DenseTensor<T> ToDenseTensor() { throw null; }
        public virtual SparseTensor<T> ToSparseTensor() { throw null; }
        public virtual CompressedSparseTensor<T> ToCompressedSparseTensor() { throw null; }
        protected virtual bool Contains(T item) { throw null; }
        protected virtual void CopyTo(T[] array, int arrayIndex) { throw null; }
        protected virtual int IndexOf(T item) { throw null; }
        IEnumerator IEnumerable.GetEnumerator() { throw null; }
        int ICollection.Count { get { throw null; } }
        bool ICollection.IsSynchronized { get { throw null; } }
        object ICollection.SyncRoot { get { throw null; } }
        void ICollection.CopyTo(Array array, int index) { throw null; }
        object IList.this[int index] { get { throw null; } set { throw null; } }
        int IList.Add(object value) { throw null; }
        void IList.Clear() { throw null; }
        bool IList.Contains(object value) { throw null; }
        int IList.IndexOf(object value) { throw null; }
        void IList.Insert(int index, object value) { throw null; }
        void IList.Remove(object value) { throw null; }
        void IList.RemoveAt(int index) { throw null; }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw null; }
        int ICollection<T>.Count { get { throw null; } }
        void ICollection<T>.Add(T item) { throw null; }
        void ICollection<T>.Clear() { throw null; }
        bool ICollection<T>.Contains(T item) { throw null; }
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) { throw null; }
        bool ICollection<T>.Remove(T item) { throw null; }
        int IReadOnlyCollection<T>.Count { get { throw null; } }
        T IList<T>.this[int index] { get { throw null; } set { throw null; } }
        int IList<T>.IndexOf(T item) { throw null; }
        void IList<T>.Insert(int index, T item) { throw null; }
        void IList<T>.RemoveAt(int index) { throw null; }
        T IReadOnlyList<T>.this[int index] { get { throw null; } }
        int IStructuralComparable.CompareTo(object other, IComparer comparer) { throw null; }
        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer) { throw null; }
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) { throw null; }
    }
}
