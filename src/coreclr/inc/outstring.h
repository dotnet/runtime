// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*****************************************************************/
/*                         OutString.h                           */
/*****************************************************************/
/* A simple, lightweight, character output stream, with very few
   external dependancies (like sprintf ... ) */

/*
   Date :  2/1/99               */
/*****************************************************************/

#ifndef _OutString_h
#define _OutString_h 1

#include "utilcode.h"   // for overloaded new
#include <string.h>     // for strlen, strcpy

/*****************************************************************/
    // a light weight character 'output' stream
class OutString {
public:
    enum FormatFlags {          // used to control printing of numbers
        none        = 0,
        put0x       = 1,        // put leading 0x on hexidecimal
        zeroFill    = 2,        // zero fill (instead of space fill)
    };

    OutString() : start(0), end(0), cur(0) {}

    OutString(unsigned initialAlloc) {
        cur = start = new char[initialAlloc+1]; // for null termination
        end = &start[initialAlloc];
    }

    ~OutString() { delete [] start; }

    // shortcut for printing decimal
    OutString& operator<<(int i) { return(dec(i)); }

    OutString& operator<<(double d);

    // FIX make this really unsigned
    OutString& operator<<(unsigned i) { return(dec(i)); }

    // prints out the hexidecimal representation
    OutString& dec(int i, size_t minWidth = 0);

    // prints out the hexidecimal representation
    OutString& hex(unsigned i, int minWidth = 0, unsigned flags = none);

    OutString& hex(unsigned __int64 i, int minWidth = 0, unsigned flags = none);

    OutString& hex(int i, int minWidth = 0, unsigned flags = none) {
        return hex(unsigned(i), minWidth, flags);
    }

    OutString& hex(__int64 i, int minWidth = 0, unsigned flags = none) {
        return hex((unsigned __int64) i, minWidth, flags);
    }

    //  print out 'count' instances of the character 'c'
    OutString& pad(size_t count, char c);

    OutString& operator<<(char c) {
        if (cur >= end)
            Realloc(1);
        *cur++ = c;
        _ASSERTE(start <= cur && cur <= end);
        return(*this);
    }

    OutString& operator<<(const WCHAR* str) {
        size_t len = wcslen(str);
        if (cur+len > end)
            Realloc(len);
        while(str != 0)
            *cur++ = (char) *str++;
        _ASSERTE(start <= cur && cur <= end);
        return(*this);
    }

    OutString& prepend(const char c) {
        char buff[2]; buff[0] = c; buff[1] = 0;
        return(prepend(buff));
    }

    OutString& prepend(const char* str) {
        size_t len = strlen(str);
        if (cur+len > end)
            Realloc(len);
        memmove(start+len, start, cur-start);
        memcpy(start, str, len);
        cur = cur + len;
        _ASSERTE(start <= cur && cur <= end);
        return(*this);
        }

    OutString& operator=(const OutString& str) {
        clear();
        *this << str;
        return(*this);
    }

    OutString& operator<<(const OutString& str) {
        write(str.start, str.cur-str.start);
        return(*this);
    }

    OutString& operator<<(const char* str) {
        write(str, strlen(str));
        return(*this);
    }

    void write(const char* str, size_t len) {
        if (cur+len > end)
            Realloc(len);
        memcpy(cur, str, len);
        cur = cur + len;
        _ASSERTE(start <= cur && cur <= end);
    }

    void swap(OutString& str) {
        char* tmp = start;
        start = str.start;
        str.start = tmp;
        tmp = end;
        end = str.end;
        str.end = tmp;
        tmp = cur;
        cur = str.cur;
        str.cur = tmp;
        _ASSERTE(start <= cur && cur <= end);
    }

    void clear()                { cur = start; }
    size_t length() const       { return(cur-start); }

    // return the null terminated string, OutString keeps ownership
    const char* val() const     { *cur = 0; return(start); }

    // grab string (caller must now delete) OutString is cleared
    char* grab()        { char* ret = start; *cur = 0; end = cur = start = 0; return(ret); }

private:
    void Realloc(size_t neededSpace);

    char *start;    // start of the buffer
    char *end;      // points at the last place null terminator can go
    char *cur;      // points at a null terminator
};

#endif // _OutString_h

