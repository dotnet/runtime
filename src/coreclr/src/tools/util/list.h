//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifndef __GENERIC_LIST_H__
#define __GENERIC_LIST_H__

// Simple parameterized linked list
// with some good ctors
template <typename _T>
struct list
{
    _T arg;
    list<_T> *next;

    list(_T t, list<_T> *n)
    {
        arg = t, next = n;
    }
    list() : arg(), next(NULL)
    {
    }
};

#endif // __GENERIC_LIST_H__
