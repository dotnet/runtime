// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ensure localloc in a loop is not converted
// to local buffer

using System;

unsafe class Program
{
    struct Element
    {
        public Element* Next;
        public int Value;
    }

    static int foo(int n)
    {
        Element* root = null;
        for (int i = 0; i < n; i++)
        {
            byte* pb = stackalloc byte[16];
            Element* p = (Element*)pb;
            p->Value = i;
            p->Next = root;
            root = p;
        }

        int sum = 0;
        while (root != null)
        {
            sum += root->Value;
            root = root->Next;
        }
        return sum;
    }

    static int Main(string[] args)
    {
        return foo(10) + 55;
    }
}
