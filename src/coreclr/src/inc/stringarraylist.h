//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef STRINGARRAYLIST_H_
#define STRINGARRAYLIST_H_


//
// StringArrayList is a simple class which is used to contain a growable
// list of Strings, stored in chunks.  Based on top of ArrayList
#include "arraylist.h"


class StringArrayList
{
    ArrayList m_Elements;
public:
    DWORD GetCount() const;
    SString& operator[] (DWORD idx) const; 
    SString& Get (DWORD idx) const; 
#ifndef DACCESS_COMPILE    
    void Append(const SString& string);
    void AppendIfNotThere(const SString& string);
#endif
    ~StringArrayList();
};


#include "stringarraylist.inl"
#endif
