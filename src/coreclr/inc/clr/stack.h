// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This header provides a basic stack implementation

#ifndef _clr_Stack_h_
#define _clr_Stack_h_

namespace clr
{
    //-------------------------------------------------------------------------------------------------
    // A basic stack class.
    //
    template < typename T >
    class Stack
    {
    private:
        //---------------------------------------------------------------------------------------------
        struct Link
        {
            template < typename A1 >
            Link(A1 && a1, Link * next = nullptr)
                : _value(std::forward<A1>(a1))
                , _next(next)
            {}

            T       _value;
            Link *  _next;
        };

    public:
        //---------------------------------------------------------------------------------------------
        // Empty stack constructor.
        Stack()
            : _top(nullptr)
            , _size(0)
        {}

        //---------------------------------------------------------------------------------------------
        // Move constructor.
        Stack(Stack && stack)
            : _top(nullptr)
            , _size(0)
        { *this = std::move(stack); }

        //---------------------------------------------------------------------------------------------
        ~Stack()
        {
            while (!empty())
            {
                pop();
            }
        }

        //---------------------------------------------------------------------------------------------
        // Move assignment.
        Stack& operator=(Stack && stack)
        { std::swap(_top, stack._top); std::swap(_size, stack._size); }

        //---------------------------------------------------------------------------------------------
        bool empty() const
        { return _top == nullptr; }

        //---------------------------------------------------------------------------------------------
        size_t size() const
        { return _size; }

        //---------------------------------------------------------------------------------------------
        T & top()
        { return _top->_value; }

        //---------------------------------------------------------------------------------------------
        T const & top() const
        { return _top->_value; }

        //---------------------------------------------------------------------------------------------
        template < typename A1 > inline
        void push(A1 && value)
        {
            STATIC_CONTRACT_THROWS;
            _top = new Link(std::forward<A1>(value), _top);
            ++_size;
        }

        //---------------------------------------------------------------------------------------------
        void pop()
        { Link * del = _top; _top = _top->_next; --_size; delete del; }

    private:
        //---------------------------------------------------------------------------------------------
        Link * _top;
        size_t _size;
    };
} // namespace clr

#endif // _clr_Stack_h_
