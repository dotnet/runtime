// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// PriorityQueue: A priority queue implemented as a max-heap

template <typename T, typename Compare>
class PriorityQueue
{
private:
    jitstd::vector<T> data;
    Compare           comp;

#ifdef DEBUG
    // Returns true only if each element has a higher priority than its children.
    bool VerifyMaxHeap() const
    {
        for (size_t i = 0; i < data.size(); i++)
        {
            const size_t leftChild  = (2 * i) + 1;
            const size_t rightChild = leftChild + 1;

            if (rightChild < data.size())
            {
                if (comp(data[i], data[leftChild]) || comp(data[i], data[rightChild]))
                {
                    return false;
                }
            }
            else if (leftChild < data.size())
            {
                if (comp(data[i], data[leftChild]))
                {
                    return false;
                }
            }
        }

        return true;
    }
#endif // DEBUG

public:
    PriorityQueue(const jitstd::allocator<T>& allocator, const Compare& compare)
        : data(allocator)
        , comp(compare)
    {
    }

    const T& Top() const
    {
        assert(!data.empty());
        return data.front();
    }

    bool Empty() const
    {
        return data.empty();
    }

    void Clear()
    {
        data.clear();
    }

    // Insert new element at the back of the vector.
    // Then, while the new element has a higher priority than its parent, move the element up.
    void Push(const T& value)
    {
        size_t i = data.size();
        data.push_back(value);

        auto getParent = [](const size_t i) -> size_t {
            return (i - 1) / 2;
        };

        // Instead of swapping the new value with its parent, copy the parent value to the correct location,
        // and write the new value once.
        for (size_t parent = getParent(i); (i != 0) && comp(data[parent], value); i = parent, parent = getParent(i))
        {
            data[i] = data[parent];
        }

        data[i] = value;
        // assert(VerifyMaxHeap());
    }

    // Remove and return the root element.
    // To efficiently pop from the back of the vector, we will look for a new position for the last element.
    T Pop()
    {
        assert(!data.empty());

        auto getLeftChild = [](const size_t i) -> size_t {
            return (2 * i) + 1;
        };

        const T      root     = data.front();
        const T&     lastElem = data.back();
        const size_t size     = data.size() - 1;
        size_t       i        = 0;

        // Instead of swapping 'lastElem' with its highest-priority child, copy the child to the parent position.
        // Once we've found a position for 'lastElem', we will write to it once.
        for (size_t maxChild = getLeftChild(i); maxChild < size; i = maxChild, maxChild = getLeftChild(i))
        {
            const size_t rightChild = maxChild + 1;
            maxChild = ((rightChild < size) && comp(data[maxChild], data[rightChild])) ? rightChild : maxChild;

            if (comp(lastElem, data[maxChild]))
            {
                data[i] = data[maxChild];
            }
            else
            {
                break;
            }
        }

        data[i] = lastElem;
        data.pop_back();

        // assert(VerifyMaxHeap());
        return root;
    }
};
