// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A readonly array with O(1) indexable lookup time.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the array.</typeparam>
    /// <devremarks>
    /// This type has a documented contract of being exactly one reference-type field in size.
    /// Our own <see cref="System.Collections.Immutable.ImmutableInterlocked"/> class depends on it, as well as others externally.
    /// IMPORTANT NOTICE FOR MAINTAINERS AND REVIEWERS:
    /// This type should be thread-safe. As a struct, it cannot protect its own fields
    /// from being changed from one thread while its members are executing on other threads
    /// because structs can change *in place* simply by reassigning the field containing
    /// this struct. Therefore it is extremely important that
    /// ** Every member should only dereference <c>this</c> ONCE. **
    /// If a member needs to reference the array field, that counts as a dereference of <c>this</c>.
    /// Calling other instance members (properties or methods) also counts as dereferencing <c>this</c>.
    /// Any member that needs to use <c>this</c> more than once must instead
    /// assign <c>this</c> to a local variable and use that for the rest of the code instead.
    /// This effectively copies the one field in the struct to a local variable so that
    /// it is insulated from other threads.
    /// </devremarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [NonVersionable] // Applies to field layout
    public partial struct ImmutableArray<T> : IEnumerable<T>, IEquatable<ImmutableArray<T>>, IImmutableArray
    {
        /// <summary>
        /// An empty (initialized) instance of <see cref="ImmutableArray{T}"/>.
        /// </summary>
#pragma warning disable CA1825
        // Array.Empty<T>() doesn't exist in all configurations
        // Switching to Array.Empty also has a non-negligible impact on the working set memory
        public static readonly ImmutableArray<T> Empty = new ImmutableArray<T>(new T[0]);
#pragma warning restore CA1825

        /// <summary>
        /// The backing field for this instance. References to this value should never be shared with outside code.
        /// </summary>
        /// <remarks>
        /// This would be private, but we make it internal so that our own extension methods can access it.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal readonly T[]? array;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct
        /// *without making a defensive copy*.
        /// </summary>
        /// <param name="items">The array to use. May be null for "default" arrays.</param>
        internal ImmutableArray(T[]? items)
        {
            this.array = items;
        }

        #region Operators

        /// <summary>
        /// Checks equality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference equal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool operator ==(ImmutableArray<T> left, ImmutableArray<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks inequality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference not equal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool operator !=(ImmutableArray<T> left, ImmutableArray<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks equality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(ImmutableArray<T>? left, ImmutableArray<T>? right)
        {
            return left.GetValueOrDefault().Equals(right.GetValueOrDefault());
        }

        /// <summary>
        /// Checks inequality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(ImmutableArray<T>? left, ImmutableArray<T>? right)
        {
            return !left.GetValueOrDefault().Equals(right.GetValueOrDefault());
        }

        #endregion

        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        public T this[int index]
        {
            [NonVersionable]
            get
            {
                // We intentionally do not check this.array != null, and throw NullReferenceException
                // if this is called while uninitialized.
                // The reason for this is perf.
                // Length and the indexer must be absolutely trivially implemented for the JIT optimization
                // of removing array bounds checking to work.
                return this.array![index];
            }
        }

        /// <summary>
        /// Gets a read-only reference to the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get a reference to.</param>
        /// <returns>A read-only reference to the element at the specified index in the read-only list.</returns>
        public ref readonly T ItemRef(int index)
        {
            // We intentionally do not check this.array != null, and throw NullReferenceException
            // if this is called while uninitialized.
            // The reason for this is perf.
            // Length and the indexer must be absolutely trivially implemented for the JIT optimization
            // of removing array bounds checking to work.
            return ref this.array![index];
        }

        /// <summary>
        /// Gets a value indicating whether this collection is empty.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsEmpty
        {
            [NonVersionable]
            get { return this.array!.Length == 0; }
        }

        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Length
        {
            [NonVersionable]
            get
            {
                // We intentionally do not check this.array != null, and throw NullReferenceException
                // if this is called while uninitialized.
                // The reason for this is perf.
                // Length and the indexer must be absolutely trivially implemented for the JIT optimization
                // of removing array bounds checking to work.
                return this.array!.Length;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this struct was initialized without an actual array instance.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDefault
        {
            get { return this.array == null; }
        }

        /// <summary>
        /// Gets a value indicating whether this struct is empty or uninitialized.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDefaultOrEmpty
        {
            get
            {
                var self = this;
                return self.array == null || self.array.Length == 0;
            }
        }

        /// <summary>
        /// Gets an untyped reference to the array.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Array? IImmutableArray.Array
        {
            get { return this.array; }
        }

        /// <summary>
        /// Gets the string to display in the debugger watches window for this instance.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var self = this;
                return self.IsDefault ? "Uninitialized" : $"Length = {self.Length}";
            }
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="destination">The array to copy to.</param>
        public void CopyTo(T[] destination)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array!, destination, self.Length);
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="destination">The array to copy to.</param>
        /// <param name="destinationIndex">The index into the destination array to which the first copied element is written.</param>
        public void CopyTo(T[] destination, int destinationIndex)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array!, 0, destination, destinationIndex, self.Length);
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="sourceIndex">The index into this collection of the first element to copy.</param>
        /// <param name="destination">The array to copy to.</param>
        /// <param name="destinationIndex">The index into the destination array to which the first copied element is written.</param>
        /// <param name="length">The number of elements to copy.</param>
        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array!, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Returns a builder that is populated with the same contents as this array.
        /// </summary>
        /// <returns>The new builder.</returns>
        public ImmutableArray<T>.Builder ToBuilder()
        {
            var self = this;
            if (self.Length == 0)
            {
                return new Builder(); // allow the builder to create itself with a reasonable default capacity
            }

            var builder = new Builder(self.Length);
            builder.AddRange(self);
            return builder;
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            return new Enumerator(self.array!);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            var self = this;
            return self.array == null ? 0 : self.array.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is IImmutableArray other && this.array == other.Array;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        [NonVersionable]
        public bool Equals(ImmutableArray<T> other)
        {
            return this.array == other.array;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct based on the contents
        /// of an existing instance, allowing a covariant static cast to efficiently reuse the existing array.
        /// </summary>
        /// <param name="items">The array to initialize the array with. No copy is made.</param>
        /// <remarks>
        /// Covariant upcasts from this method may be reversed by calling the
        /// <see cref="ImmutableArray{T}.As{TOther}"/>  or <see cref="ImmutableArray{T}.CastArray{TOther}"/>method.
        /// </remarks>
        public static ImmutableArray<
#nullable disable
            T
#nullable restore
            > CastUp<TDerived>(ImmutableArray<TDerived> items)
            where TDerived : class?, T
        {
            return new ImmutableArray<T>(items.array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct by casting the underlying
        /// array to an array of type <typeparam name="TOther"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if the cast is illegal.</exception>
        public ImmutableArray<
#nullable disable
            TOther
#nullable restore
            > CastArray<TOther>() where TOther : class?
        {
            return new ImmutableArray<TOther>((TOther[])(object)array!);
        }

        /// <summary>
        /// Creates an immutable array for this array, cast to a different element type.
        /// </summary>
        /// <typeparam name="TOther">The type of array element to return.</typeparam>
        /// <returns>
        /// A struct typed for the base element type. If the cast fails, an instance
        /// is returned whose <see cref="IsDefault"/> property returns <c>true</c>.
        /// </returns>
        /// <remarks>
        /// Arrays of derived elements types can be cast to arrays of base element types
        /// without reallocating the array.
        /// These upcasts can be reversed via this same method, casting an array of base
        /// element types to their derived types. However, downcasting is only successful
        /// when it reverses a prior upcasting operation.
        /// </remarks>
        public ImmutableArray<
#nullable disable
            TOther
#nullable restore
            > As<TOther>() where TOther : class?
        {
            return new ImmutableArray<TOther>((this.array as TOther[]));
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(self.array!);
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(self.array!);
        }

        /// <summary>
        /// Throws a null reference exception if the array field is null.
        /// </summary>
        internal void ThrowNullRefIfNotInitialized()
        {
            // Force NullReferenceException if array is null by touching its Length.
            // This way of checking has a nice property of requiring very little code
            // and not having any conditions/branches.
            // In a faulting scenario we are relying on hardware to generate the fault.
            // And in the non-faulting scenario (most common) the check is virtually free since
            // if we are going to do anything with the array, we will need Length anyways
            // so touching it, and potentially causing a cache miss, is not going to be an
            // extra expense.
            _ = this.array!.Length;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the <see cref="array"/> field is null, i.e. the
        /// <see cref="IsDefault"/> property returns true.  The
        /// <see cref="InvalidOperationException"/> message specifies that the operation cannot be performed
        /// on a default instance of <see cref="ImmutableArray{T}"/>.
        ///
        /// This is intended for explicitly implemented interface method and property implementations.
        /// </summary>
        private void ThrowInvalidOperationIfNotInitialized()
        {
            if (this.IsDefault)
            {
                throw new InvalidOperationException(SR.InvalidOperationOnDefaultArray);
            }
        }
    }
}
