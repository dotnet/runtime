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

    // Returns true only if each element has a higher priority than its children.
    bool verifyMaxHeap() const
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

public:
    PriorityQueue(const jitstd::allocator<T>& allocator, const Compare& compare)
        : data(allocator)
        , comp(compare)
    {
    }

    const T& top() const
    {
        assert(!data.empty());
        return data.front();
    }

    bool empty() const
    {
        return data.empty();
    }

    size_t size() const
    {
        return data.size();
    }

    // Insert new element at the back of the vector.
    // Then, while the new element has a higher priority than its parent, swap them.
    void push(const T& value)
    {
        size_t i = data.size();
        data.push_back(value);

        auto getParent = [](const size_t i) -> size_t {
            return (i - 1) / 2;
        };

        for (size_t parent = getParent(i); (i != 0) && comp(data[parent], data[i]); i = parent, parent = getParent(i))
        {
            std::swap(data[parent], data[i]);
        }

        assert(verifyMaxHeap());
    }

    // Swap the root and last element to facilitate removing the former.
    // Then, while the new root element has a lower priority than its children,
    // swap the element with its highest-priority child.
    void pop()
    {
        assert(!data.empty());
        std::swap(data.front(), data.back());
        data.pop_back();

        auto getLeftChild = [](const size_t i) -> size_t {
            return (2 * i) + 1;
        };

        for (size_t i = 0, maxChild = getLeftChild(i); maxChild < data.size(); i = maxChild, maxChild = getLeftChild(i))
        {
            const size_t rightChild = maxChild + 1;
            maxChild = ((rightChild < data.size()) && comp(data[maxChild], data[rightChild])) ? rightChild : maxChild;

            if (comp(data[i], data[maxChild]))
            {
                std::swap(data[i], data[maxChild]);
            }
            else
            {
                break;
            }
        }

        assert(verifyMaxHeap());
    }
};
