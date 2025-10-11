using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StackArrayBuilder;

/// <summary>
/// Helper type for avoiding allocations while building arrays.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Will grow heap allocated size, if you need it.
/// Only use grow in rare cases, as it needs to grow the array, if over already allocated size.
/// If you are certain of the max size needed, you can use e.g. <code>StackArrayBuilder8</code>
/// </remarks>
internal ref struct StackArrayBuilder<T>
{
    private InlineArray16<T> _stackAllocatedBuffer = default;
    public const int StackAllocatedCapacity = 16;
    private const int DefaultHeapCapacity = 4;

    private T[]? _heapArrayBuffer; // Starts out null, initialized if capacity is over stack allocated size when constructing or on Add.
    private int _count; // Number of items added.

    /// <summary>
    /// Initializes the <see cref="StackArrayBuilder{T}"/> with a specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity of the array to allocate.</param>
    public StackArrayBuilder(int capacity) : this()
    {
        Debug.Assert(capacity >= 0);
        if (capacity > StackAllocatedCapacity)
        {
            _heapArrayBuffer = new T[capacity - StackAllocatedCapacity];
        }
    }

    /// <summary>
    /// Gets the number of items this instance can store without re-allocating.
    /// <c>StackAllocatedCapacity</c> if the backing heap array is not needed, all up to that is already stack allocated
    /// </summary>
    /// <remarks>Only for unit testing, checking that overallocation does not happen</remarks>
    public int Capacity => _heapArrayBuffer?.Length + StackAllocatedCapacity ?? StackAllocatedCapacity;

    /// <summary>
    /// Adds an item, resizing heap allocated array if necessary.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        if (_count == Capacity)
        {
            EnsureCapacity(_count + 1);
        }

        UncheckedAdd(item);
    }

    /// <summary>
    /// Creates an array from the contents of this builder.
    /// </summary>
    public T[] ToArray()
    {
        if (_count == 0)
        {
            return [];
        }

        T[] result = new T[_count];
        int index = 0;
        foreach (T stackAllocatedValue in _stackAllocatedBuffer)
        {
            result[index++] = stackAllocatedValue;
            if (index >= _count)
            {
                return result;
            }
        }

        _heapArrayBuffer.AsSpan(0, _count - StackAllocatedCapacity).CopyTo(result.AsSpan(start: StackAllocatedCapacity));

        return result;
    }

    /// <summary>
    /// Adds an item, without checking if there is room.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <remarks>
    /// Use this method if you know there is enough space in the <see cref="StackArrayBuilder{T}"/>
    /// for another item, and you are writing performance-sensitive code.
    /// </remarks>
    public void UncheckedAdd(T item)
    {
        Debug.Assert(_count < Capacity);
        if (_count < StackAllocatedCapacity)
        {
            _stackAllocatedBuffer[_count++] = item;
        }
        else
        {
            _heapArrayBuffer![_count++ - StackAllocatedCapacity] = item;
        }
    }

    private void EnsureCapacity(int minimum)
    {
        Debug.Assert(minimum > Capacity);

        if (minimum < StackAllocatedCapacity)
        {
            return; // There is still room on the stack
        }

        if (_heapArrayBuffer == null)
        {
            // Initial capacity has not been not set or too low, we will allocate the default heap array size
            _heapArrayBuffer = new T[DefaultHeapCapacity];
            return;
        }

        // Check if allocated heap capacity was enough
        int defaultCapacityWithHeap = _heapArrayBuffer.Length + StackAllocatedCapacity;
        if (defaultCapacityWithHeap >= minimum)
        {
            return; // current allocated stack+heap is large enough
        }

        // We need to allocate more heap capacity, by increasing the size of the array
        int nextHeapCapacity = 2 * _heapArrayBuffer.Length;

        if ((uint)nextHeapCapacity > (uint)Array.MaxLength)
        {
            nextHeapCapacity = Math.Max(_heapArrayBuffer.Length + 1, Array.MaxLength);
        }

        nextHeapCapacity = Math.Max(nextHeapCapacity, minimum);

        Array.Resize(ref _heapArrayBuffer, nextHeapCapacity);
    }
}
