// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                list<T>                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "iterator.h"
#include "functional.h"
#include "clr_std/utility"

namespace jitstd
{

template <typename T, typename Allocator = jitstd::allocator<T>>
class list
{
public:
    typedef Allocator allocator_type;
    typedef T* pointer;
    typedef T& reference;
    typedef const T* const_pointer;
    typedef const T& const_reference;

    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    typedef T value_type;

    // Forward declaration
private:
    struct Node;

public:
    // nested classes
    class iterator;
    class const_iterator : public jitstd::iterator<bidirectional_iterator_tag, T>
    {
    private:
        const_iterator(Node* ptr);
    public:
        const_iterator();
        const_iterator(const const_iterator& it);
        const_iterator(const typename list<T, Allocator>::iterator& it);

        const_iterator& operator++();
        const_iterator& operator++(int);
        const_iterator& operator--();
        const_iterator& operator--(int);
        const_iterator operator+(difference_type n);
        const_iterator operator-(difference_type n);
        size_type operator-(const const_iterator& that);
        bool operator==(const const_iterator& it) const;
        bool operator!=(const const_iterator& it) const;
        const T& operator*() const;
        const T* operator&() const;
        const T* operator->() const;
        operator const T*() const;

    private:
        friend class list<T, Allocator>;
        Node* m_pNode;
    };

    class iterator : public jitstd::iterator<bidirectional_iterator_tag, T>
    {
        iterator(Node* ptr);
    public:
        iterator();
        iterator(const iterator& it);

        iterator& operator++();
        iterator& operator++(int);
        iterator& operator--();
        iterator& operator--(int);
        iterator operator+(difference_type n);
        iterator operator-(difference_type n);
        size_type operator-(const iterator& that);
        bool operator==(const iterator& it);
        bool operator!=(const iterator& it);
        T& operator*();
        T* operator&();
        T* operator->();
        operator T*();

    private:
        friend class list<T, Allocator>;
        friend class list<T, Allocator>::const_iterator;
        Node* m_pNode;
    };

    class reverse_iterator;
    class const_reverse_iterator : public jitstd::iterator<bidirectional_iterator_tag, T>
    {
    private:
        const_reverse_iterator(Node* ptr);
    public:
        const_reverse_iterator();
        const_reverse_iterator(const const_reverse_iterator& it);
        const_reverse_iterator(const reverse_iterator& it);

        const_reverse_iterator& operator++();
        const_reverse_iterator& operator++(int);
        const_reverse_iterator& operator--();
        const_reverse_iterator& operator--(int);
        const_reverse_iterator operator+(difference_type n);
        const_reverse_iterator operator-(difference_type n);
        size_type operator-(const const_reverse_iterator& that);
        bool operator==(const const_reverse_iterator& it) const;
        bool operator!=(const const_reverse_iterator& it) const;
        const T& operator*() const;
        const T* operator&() const;
        const T* operator->() const;
        operator const T*() const;

    private:
        friend class list<T, Allocator>;
        Node* m_pNode;
    };

    class reverse_iterator : public jitstd::iterator<bidirectional_iterator_tag, T>
    {
    private:
        reverse_iterator(Node* ptr);
    public:
        reverse_iterator();
        reverse_iterator(const reverse_iterator& it);

        reverse_iterator& operator++();
        reverse_iterator& operator++(int);
        reverse_iterator& operator--();
        reverse_iterator& operator--(int);
        reverse_iterator operator+(difference_type n);
        reverse_iterator operator-(difference_type n);
        size_type operator-(const reverse_iterator& that);
        bool operator==(const reverse_iterator& it);
        bool operator!=(const reverse_iterator& it);
        T& operator*();
        T* operator&();
        T* operator->();
        operator T*();
        friend class list<T, Allocator>::const_reverse_iterator;

    private:
        friend class list<T, Allocator>;
        Node* m_pNode;
    };

#ifdef DEBUG
    void init(const Allocator& a)
    {
        m_pHead = nullptr;
        m_pTail = nullptr;
        m_nSize = 0;
        m_allocator = a;
        m_nodeAllocator = a;
    }
#endif

    explicit list(const Allocator&);
    list(size_type n, const T& value, const Allocator&);

    template <typename InputIterator>
    list(InputIterator first, InputIterator last, const Allocator&);

    list(const list<T, Allocator>&);

    ~list();

    template <class InputIterator>
    void assign(InputIterator first, InputIterator last);

    void assign(size_type size, const T& val);

    reference back();
    const_reference back() const;
    iterator backPosition();
    const_iterator backPosition() const;

    iterator begin();
    const_iterator begin() const;

    void clear();
    bool empty() const;

    iterator end();
    const_iterator end() const;

    iterator erase(iterator position);

    reference front();
    const_reference front() const;

    allocator_type get_allocator() const;

    iterator insert(iterator position, const T& x);
    template <class... Args>
    iterator emplace(iterator position, Args&&... args);
    void insert(iterator position, size_type n, const T& x);
    template <class InputIterator>
    void insert(iterator position, InputIterator first, InputIterator last);

    size_type max_size() const;

    void merge(list<T, Allocator>& lst);
    template <class Compare>
    void merge (list<T, Allocator>& lst, Compare comp);

    list<T, Allocator>& operator=(const list<T, Allocator>& lst);

    void pop_back();
    void pop_front();

    void push_back(const T& val);
    template <class... Args>
    void emplace_back(Args&&... args);
    void push_front (const T& val);
    template <class... Args>
    void emplace_front(Args&&... args);

    reverse_iterator rbegin();
    const_reverse_iterator rbegin() const;

    void remove(const T& val);
    template <class Predicate>
    void remove_if(Predicate pred);

    reverse_iterator rend();
    const_reverse_iterator rend() const;

    void resize(size_type sz, const T& c);
    void reverse();

    size_type size() const;
    void sort();

    template <class Compare>
    void sort(Compare comp);

    void splice(iterator position, list& lst);
    void splice(iterator position, list& lst, iterator i);
    void splice(iterator position, list& x, iterator first, iterator last);

    void swap(list<T,Allocator>& lst);

    void unique();

    template <class BinaryPredicate>
    void unique(const BinaryPredicate& binary_pred);

private:
    struct Node
    {
        T m_value;
        Node* m_pNext;
        Node* m_pPrev;

        template <class... Args>
        Node(Args&&... args)
            : m_value(std::forward<Args>(args)...)
        {
        }
    };

    void destroy_helper();

    void construct_helper(size_type n, const T& value, int_not_an_iterator_tag);
    template <typename InputIterator>
    void construct_helper(InputIterator first, InputIterator last, forward_iterator_tag);

    void assign_helper(size_type n, const T& value, int_not_an_iterator_tag);
    template <typename InputIterator>
    void assign_helper(InputIterator first, InputIterator last, forward_iterator_tag);

    void insert_helper(iterator position, size_type n, const T& value, int_not_an_iterator_tag);
    template <typename InputIterator>
    void insert_helper(iterator position, InputIterator first, InputIterator last, forward_iterator_tag);

    void insert_new_node_helper(Node* pInsert, Node* pNewNode);

    Node* m_pHead;
    Node* m_pTail;
    size_type m_nSize;
    typename Allocator::template rebind<T>::allocator m_allocator;
    typename Allocator::template rebind<Node>::allocator m_nodeAllocator;
};

}

namespace jitstd
{
template <typename T, typename Allocator>
list<T, Allocator>::list(const Allocator& allocator)
    : m_pHead(nullptr)
    , m_pTail(nullptr)
    , m_nSize(0)
    , m_allocator(allocator)
    , m_nodeAllocator(allocator)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::list(size_type n, const T& value, const Allocator& allocator)
    : m_pHead(NULL)
    , m_pTail(NULL)
    , m_nSize(0)
    , m_allocator(allocator)
    , m_nodeAllocator(allocator)
{
    construct_helper(n, value, int_not_an_iterator_tag());
}

template <typename T, typename Allocator>
template <typename InputIterator>
list<T, Allocator>::list(InputIterator first, InputIterator last, const Allocator& allocator)
    : m_pHead(NULL)
    , m_pTail(NULL)
    , m_nSize(0)
    , m_allocator(allocator)
    , m_nodeAllocator(allocator)
{
    construct_helper(first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
list<T, Allocator>::list(const list<T, Allocator>& other)
    : m_pHead(NULL)
    , m_pTail(NULL)
    , m_nSize(0)
    , m_allocator(other.m_allocator)
    , m_nodeAllocator(other.m_nodeAllocator)
{
    construct_helper(other.begin(), other.end(), forward_iterator_tag());
}

template <typename T, typename Allocator>
list<T, Allocator>::~list()
{
    destroy_helper();
}

template <typename T, typename Allocator>
template <class InputIterator>
void list<T, Allocator>::assign(InputIterator first, InputIterator last)
{
    assign_helper(first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
void list<T, Allocator>::assign(size_type size, const T& val)
{
    assign_helper(size, val, int_not_an_iterator_tag());
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reference list<T, Allocator>::back()
{
    return m_pTail->m_value;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reference list<T, Allocator>::back() const
{
    return m_pTail->m_value;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator list<T, Allocator>::backPosition()
{
    return iterator(m_pTail);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator list<T, Allocator>::backPosition() const
{
    return const_iterator(m_pTail);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator list<T, Allocator>::begin()
{
    return iterator(m_pHead);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator list<T, Allocator>::begin() const
{
    return const_iterator(m_pHead);
}

template <typename T, typename Allocator>
void list<T, Allocator>::clear()
{
    destroy_helper();
}

template <typename T, typename Allocator>
bool list<T, Allocator>::empty() const
{
    return (m_nSize == 0);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator list<T, Allocator>::end()
{
    return iterator(nullptr);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator list<T, Allocator>::end() const
{
    return const_iterator(NULL);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator list<T, Allocator>::erase(iterator position)
{
    // Nothing to erase.
    assert(position.m_pNode != nullptr);

    --m_nSize;

    Node* pNode = position.m_pNode;
    Node* pPrev = pNode->m_pPrev;
    Node* pNext = pNode->m_pNext;

    if (pPrev != nullptr)
    {
        pPrev->m_pNext = pNext;
    }
    else
    {
        m_pHead = pNext;
    }

    if (pNext != nullptr)
    {
        pNext->m_pPrev = pPrev;
    }
    else
    {
        m_pTail = pPrev;
    }

    m_nodeAllocator.deallocate(pNode, 1);
    return iterator(pNext);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reference list<T, Allocator>::front()
{
    return m_pHead->m_value;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reference list<T, Allocator>::front() const
{
    return m_pHead->m_value;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::allocator_type list<T, Allocator>::get_allocator() const
{
    return m_allocator;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator
    list<T, Allocator>::insert(iterator position, const T& val)
{
    Node* pNewNode = new (m_nodeAllocator.allocate(1), placement_t()) Node(val);
    insert_new_node_helper(position.m_pNode, pNewNode);
    return iterator(pNewNode);
}

template <typename T, typename Allocator>
template <typename... Args>
typename list<T, Allocator>::iterator
    list<T, Allocator>::emplace(iterator position, Args&&... args)
{
    Node* pNewNode = new (m_nodeAllocator.allocate(1), placement_t()) Node(std::forward<Args>(args)...);
    insert_new_node_helper(position.m_pNode, pNewNode);
    return iterator(pNewNode);
}

template <typename T, typename Allocator>
void list<T, Allocator>::insert(iterator position, size_type n, const T& val)
{
    insert_helper(position, n, val, int_not_an_iterator_tag());
}

template <typename T, typename Allocator>
template <class InputIterator>
void list<T, Allocator>::insert(iterator position, InputIterator first, InputIterator last)
{
    insert_helper(position, first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
typename list<T, Allocator>::size_type list<T, Allocator>::max_size() const
{
    return (((size_type)-1) >> 1) / sizeof(Node);
}

template <typename T, typename Allocator>
void list<T, Allocator>::merge(list<T, Allocator>& lst)
{
    merge(lst, jitstd::greater<T>());
}

template <typename T, typename Allocator>
template <class Compare>
void list<T, Allocator>::merge(list<T, Allocator>& lst, Compare comp)
{
    int size = lst.m_nSize;

    iterator i = begin();
    iterator j = lst.begin();
    while (i != end() && j != lst.end())
    {
        if (comp(*i, *j))
        {
            i = insert(i, *j);
            ++j;
            --size;
        }
        else
        {
            ++i;
        }
    }

    if (j != lst.end())
    {
        if (m_pTail != NULL)
        {
            m_pTail->m_pNext = j.m_pNode;
        }
        else
        {
            m_pHead = j.m_pNode;
        }
        m_pTail = lst.m_pTail;
        m_nSize += size;
    }
}

template <typename T, typename Allocator>
list<T, Allocator>& list<T, Allocator>::operator=(const list<T, Allocator>& lst)
{
    destroy_helper();
    construct_helper(lst.begin(), lst.end(), forward_iterator_tag());
    return *this;
}

template <typename T, typename Allocator>
void list<T, Allocator>::pop_back()
{
    assert(m_nSize != 0);

    --m_nSize;

    Node* pDelete = m_pTail;
    if (m_pHead != m_pTail)
    {
        m_pTail = m_pTail->m_pPrev;
        m_pTail->m_pNext = nullptr;
    }
    else
    {
        m_pHead = nullptr;
        m_pTail = nullptr;
    }
    pDelete->~Node();
    m_nodeAllocator.deallocate(pDelete, 1);
}

template <typename T, typename Allocator>
void list<T, Allocator>::pop_front()
{
    assert(m_nSize != 0);

    --m_nSize;

    Node* pDelete = m_pHead;
    if (m_pHead != m_pTail)
    {
        m_pHead = m_pHead->m_pNext;
        m_pHead->m_pPrev = NULL;
    }
    else
    {
        m_pHead = NULL;
        m_pTail = NULL;
    }
    pDelete->~Node();
    m_nodeAllocator.deallocate(pDelete, 1);
}

template <typename T, typename Allocator>
void list<T, Allocator>::push_back(const T& val)
{
    insert(end(), val);
}

template <typename T, typename Allocator>
template <typename... Args>
void list<T, Allocator>::emplace_back(Args&&... args)
{
    emplace(end(), std::forward<Args>(args)...);
}

template <typename T, typename Allocator>
void list<T, Allocator>::push_front(const T& val)
{
    insert(begin(), val);
}

template <typename T, typename Allocator>
template <typename... Args>
void list<T, Allocator>::emplace_front(Args&&... args)
{
    emplace(begin(), std::forward<Args>(args)...);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator
    list<T, Allocator>::rbegin()
{
    return reverse_iterator(m_pTail);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator
    list<T, Allocator>::rbegin() const
{
    return const_reverse_iterator(m_pTail);
}

template <typename T, typename Allocator>
void list<T, Allocator>::remove(const T& val)
{
    for (iterator i = begin(); i != end();)
    {
        if (*i == val)
        {
            i = erase(i);
        }
        else
        {
            ++i;
        }
    }
}

template <typename T, typename Allocator>
template <class Predicate>
void list<T, Allocator>::remove_if(Predicate pred)
{
    for (iterator i = begin(); i != end();)
    {
        if (pred(*i))
        {
            i = erase(i);
        }
        else
        {
            ++i;
        }
    }
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator list<T, Allocator>::rend()
{
    return reverse_iterator(nullptr);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator list<T, Allocator>::rend() const
{
    return reverse_iterator(NULL);
}

template <typename T, typename Allocator>
void list<T, Allocator>::resize(size_type sz, const T& c)
{
    while (m_nSize < sz)
    {
        insert(end(), c);
    }

    while (m_nSize > sz)
    {
        erase(end());
    }
}

template <typename T, typename Allocator>
void list<T, Allocator>::reverse()
{
    for (Node* p = m_pHead; p != NULL; p = p->m_pNext)
    {
        std::swap(p->m_pPrev, p->m_pNext);
    }
    std::swap(m_pHead, m_pTail);
}

template <typename T, typename Allocator>
typename list<T, Allocator>::size_type list<T, Allocator>::size() const
{
    return m_nSize;
}

template <typename T, typename Allocator>
void list<T, Allocator>::sort()
{
    assert(false && !"template method not implemented.");
}

template <typename T, typename Allocator>
template <class Compare>
void list<T, Allocator>::sort(Compare comp)
{
    assert(false && !"template method not implemented.");
}

template <typename T, typename Allocator>
void list<T, Allocator>::splice(iterator position, list& lst)
{
    if (lst.m_nSize == 0)
    {
        return;
    }
    if (m_nSize == 0)
    {
        std::swap(lst.m_pHead, m_pHead);
        std::swap(lst.m_pTail, m_pTail);
        std::swap(lst.m_nSize, m_nSize);
    }
}

template <typename T, typename Allocator>
void list<T, Allocator>::splice(iterator position, list& lst, iterator i)
{
}

template <typename T, typename Allocator>
void list<T, Allocator>::splice(iterator position, list& x, iterator first, iterator last)
{
}

template <typename T, typename Allocator>
void list<T, Allocator>::swap(list<T, Allocator>& lst)
{
    std::swap(lst.m_pHead, m_pHead);
    std::swap(lst.m_pTail, m_pTail);
    std::swap(lst.m_nSize, m_nSize);
    std::swap(lst.m_allocator, m_allocator);
    std::swap(lst.m_nodeAllocator, m_nodeAllocator);
}

template <typename T, typename Allocator>
void list<T, Allocator>::unique()
{
    assert(false && !"template method not implemented.");
}

template <typename T, typename Allocator>
template <class BinaryPredicate>
void list<T, Allocator>::unique(const BinaryPredicate& binary_pred)
{
    assert(false && !"template method not implemented.");
}

// private
template <typename T, typename Allocator>
void list<T, Allocator>::destroy_helper()
{
    while (m_pTail != nullptr)
    {
        Node* prev = m_pTail->m_pPrev;
        m_pTail->~Node();
        m_nodeAllocator.deallocate(m_pTail, 1);
        m_pTail = prev;
    }
    m_pHead = nullptr;
    m_nSize = 0;
}


template <typename T, typename Allocator>
void list<T, Allocator>::construct_helper(size_type n, const T& value, int_not_an_iterator_tag)
{
    for (int i = 0; i < n; ++i)
    {
        insert(end(), value);
    }
    assert(m_nSize == n);
}

template <typename T, typename Allocator>
template <typename InputIterator>
void list<T, Allocator>::construct_helper(InputIterator first, InputIterator last, forward_iterator_tag)
{
    while (first != last)
    {
        insert(end(), *first);
        ++first;
    }
}

template <typename T, typename Allocator>
void list<T, Allocator>::assign_helper(size_type n, const T& value, int_not_an_iterator_tag)
{
    destroy_helper();
    for (int i = 0; i < n; ++i)
    {
        insert(end(), value);
    }
}

template <typename T, typename Allocator>
template <typename InputIterator>
void list<T, Allocator>::assign_helper(InputIterator first, InputIterator last, forward_iterator_tag)
{
    destroy_helper();
    while (first != last)
    {
        insert(end(), *first);
        ++first;
    }
}

template <typename T, typename Allocator>
void list<T, Allocator>::insert_helper(iterator position, size_type n, const T& value, int_not_an_iterator_tag)
{
    for (int i = 0; i < n; ++i)
    {
        insert(position, value);
    }
}

template <typename T, typename Allocator>
template <typename InputIterator>
void list<T, Allocator>::insert_helper(iterator position, InputIterator first, InputIterator last, forward_iterator_tag)
{
    while (first != last)
    {
        insert(position, *first);
        ++first;
    }
}

template <typename T, typename Allocator>
void list<T, Allocator>::insert_new_node_helper(Node* pInsert, Node* pNewNode)
{
    ++m_nSize;

    if (pInsert == nullptr)
    {
        pNewNode->m_pPrev = m_pTail;
        pNewNode->m_pNext = nullptr;
        if (m_pHead == nullptr)
        {
            m_pHead = pNewNode;
        }
        else
        {
            m_pTail->m_pNext = pNewNode;
        }
        m_pTail = pNewNode;
    }
    else
    {
        pNewNode->m_pPrev = pInsert->m_pPrev;
        pNewNode->m_pNext = pInsert;
        if (pInsert->m_pPrev == nullptr)
        {
            m_pHead = pNewNode;
        }
        else
        {
            pInsert->m_pPrev->m_pNext = pNewNode;
        }
        pInsert->m_pPrev = pNewNode;
    }
}

} // end of namespace jitstd.





// Implementation of list iterators

namespace jitstd
{

// iterator
template <typename T, typename Allocator>
list<T, Allocator>::iterator::iterator()
    : m_pNode(NULL)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::iterator::iterator(Node* pNode)
    : m_pNode(pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::iterator::iterator(const iterator& it)
    : m_pNode(it.m_pNode)
{
}


template <typename T, typename Allocator>
typename list<T, Allocator>::iterator& list<T, Allocator>::iterator::operator++()
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator& list<T, Allocator>::iterator::operator++(int)
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator& list<T, Allocator>::iterator::operator--()
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::iterator& list<T, Allocator>::iterator::operator--(int)
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
bool list<T, Allocator>::iterator::operator==(const iterator& it)
{
    return (m_pNode == it.m_pNode);
}

template <typename T, typename Allocator>
bool list<T, Allocator>::iterator::operator!=(const iterator& it)
{
    return !operator==(it);
}

template <typename T, typename Allocator>
T& list<T, Allocator>::iterator::operator*()
{
    return m_pNode->m_value;
}

template <typename T, typename Allocator>
T* list<T, Allocator>::iterator::operator&()
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
T* list<T, Allocator>::iterator::operator->()
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
list<T, Allocator>::iterator::operator T*()
{
    return &(m_pNode->m_value);
}




// const_iterator
template <typename T, typename Allocator>
list<T, Allocator>::const_iterator::const_iterator()
    : m_pNode(NULL)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_iterator::const_iterator(Node* pNode)
    : m_pNode(pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_iterator::const_iterator(const const_iterator& it)
    : m_pNode(it.m_pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_iterator::const_iterator(const typename list<T, Allocator>::iterator& it)
    : m_pNode(it.m_pNode)
{
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator& list<T, Allocator>::const_iterator::operator++()
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator& list<T, Allocator>::const_iterator::operator++(int)
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator& list<T, Allocator>::const_iterator::operator--()
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_iterator& list<T, Allocator>::const_iterator::operator--(int)
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
bool list<T, Allocator>::const_iterator::operator==(const const_iterator& it) const
{
    return (m_pNode == it.m_pNode);
}

template <typename T, typename Allocator>
bool list<T, Allocator>::const_iterator::operator!=(const const_iterator& it) const
{
    return !operator==(it);
}

template <typename T, typename Allocator>
const T& list<T, Allocator>::const_iterator::operator*() const
{
    return m_pNode->m_value;
}

template <typename T, typename Allocator>
const T* list<T, Allocator>::const_iterator::operator&() const
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
const T* list<T, Allocator>::const_iterator::operator->() const
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
list<T, Allocator>::const_iterator::operator const T*() const
{
    return &(m_pNode->m_value);
}


// reverse_iterator
template <typename T, typename Allocator>
list<T, Allocator>::reverse_iterator::reverse_iterator()
    : m_pNode(NULL)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::reverse_iterator::reverse_iterator(Node* pNode)
    : m_pNode(pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::reverse_iterator::reverse_iterator(const reverse_iterator& it)
    : m_pNode(it.m_pNode)
{
}


template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator& list<T, Allocator>::reverse_iterator::operator++()
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator& list<T, Allocator>::reverse_iterator::operator++(int)
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator& list<T, Allocator>::reverse_iterator::operator--()
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::reverse_iterator& list<T, Allocator>::reverse_iterator::operator--(int)
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
bool list<T, Allocator>::reverse_iterator::operator==(const reverse_iterator& it)
{
    return (m_pNode == it.m_pNode);
}

template <typename T, typename Allocator>
bool list<T, Allocator>::reverse_iterator::operator!=(const reverse_iterator& it)
{
    return !operator==(it);
}

template <typename T, typename Allocator>
T& list<T, Allocator>::reverse_iterator::operator*()
{
    return m_pNode->m_value;
}

template <typename T, typename Allocator>
T* list<T, Allocator>::reverse_iterator::operator&()
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
T* list<T, Allocator>::reverse_iterator::operator->()
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
list<T, Allocator>::reverse_iterator::operator T*()
{
    return &(m_pNode->m_value);
}

// const_reverse_iterator
template <typename T, typename Allocator>
list<T, Allocator>::const_reverse_iterator::const_reverse_iterator()
    : m_pNode(NULL)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_reverse_iterator::const_reverse_iterator(Node* pNode)
    : m_pNode(pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_reverse_iterator::const_reverse_iterator(const const_reverse_iterator& it)
    : m_pNode(it.m_pNode)
{
}

template <typename T, typename Allocator>
list<T, Allocator>::const_reverse_iterator::const_reverse_iterator(const reverse_iterator& it)
    : m_pNode(it.m_pNode)
{
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator& list<T, Allocator>::const_reverse_iterator::operator++()
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator& list<T, Allocator>::const_reverse_iterator::operator++(int)
{
    m_pNode = m_pNode->m_pPrev;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator& list<T, Allocator>::const_reverse_iterator::operator--()
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
typename list<T, Allocator>::const_reverse_iterator& list<T, Allocator>::const_reverse_iterator::operator--(int)
{
    m_pNode = m_pNode->m_pNext;
    return *this;
}

template <typename T, typename Allocator>
bool list<T, Allocator>::const_reverse_iterator::operator==(const const_reverse_iterator& it) const
{
    return (m_pNode == it.m_pNode);
}

template <typename T, typename Allocator>
bool list<T, Allocator>::const_reverse_iterator::operator!=(const const_reverse_iterator& it) const
{
    return !operator==(it);
}

template <typename T, typename Allocator>
const T& list<T, Allocator>::const_reverse_iterator::operator*() const
{
    return m_pNode->m_value;
}

template <typename T, typename Allocator>
const T* list<T, Allocator>::const_reverse_iterator::operator&() const
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
const T* list<T, Allocator>::const_reverse_iterator::operator->() const
{
    return &(m_pNode->m_value);
}

template <typename T, typename Allocator>
list<T, Allocator>::const_reverse_iterator::operator const T*() const
{
    return &(m_pNode->m_value);
}

}

