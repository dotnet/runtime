using System.Runtime.CompilerServices;

namespace StackArrayBuilder;

/// <summary>
/// Helper type for avoiding allocations while building arrays.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Throws <code>InvalidOperationException</code>, if the right size is not selected.
/// Be sure to select the right size to not over- or under-allocate expected size on stack.
/// </remarks>
internal ref struct StackArrayBuilder8<T>
{
    private InlineArray8<T> _stackAllocatedBuffer = default;
    public const int StackAllocatedCapacity = 8;

    private int _count = 0; // Number of items added.

    public StackArrayBuilder8()
    {
    }

    /// <summary>Adds an item</summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        if (_count == StackAllocatedCapacity)
        {
            throw new InvalidOperationException("Stack allocated capacity exceeded");
        }
        _stackAllocatedBuffer[_count++] = item;
    }

    /// <summary>Creates an array from the contents of this builder.</summary>
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
        return result;
    }
}
