// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/***************************
 * Stack.cs
 * Author: Alina Popa
 * Date: 11/5/2008
 * Description: Simple stack class that holds items of type T and has the following methods:
 * - Push
 * - Pop
 * - Peek
 * - GetItemAt(index) -- for iterating
 **************************/

using System;

public class Stack<T>
{
    private const int DEFAULT_CAPACITY = 30;
    private T[] objArray;
    private int capacity;
    private int size;

    public Stack(int initialCapacity)
    {
        if (initialCapacity <= 0)
            throw new System.Exception("Stack initial capacity should be a positive number");

        objArray = new T[initialCapacity];
        size = 0;
        capacity = initialCapacity;
    }

    public Stack()
    {
        objArray = new T[DEFAULT_CAPACITY];
        size = 0;
        capacity = DEFAULT_CAPACITY;
    }

    //Put item at the end
    public void Push(T item)
    {
        if(size == capacity)
        {
            ExpandArray();
        }
        objArray[size] = item;
        size ++;                
    }

    //Pop the last item
    public T Pop()
    {
        if (size <= 0)
            throw new System.Exception("Empty stack");

        size--;
        return objArray[size];
    }

    //Get the last item, without removing it from stack
    public T Peek()
    {
        if (size <= 0)
            throw new System.Exception("Empty stack");

        return objArray[size-1];
    }

    public int Count
    {
        get
        {
            return size;
        }
    }

    //Returns the item in a given position
    public T GetItemAt(int index)
    {
        if ((index < 0) || (index > size - 1))
        {
            throw new System.Exception("Invalid index");
        }

        return objArray[index];
    }

    //Doubles the capacity of the array
    private void ExpandArray()
    {
        int newCapacity = 2 * capacity;
        T[] newArray = new T[newCapacity];

        //copy objArray in newArray
        for (int i = 0; i < capacity; i++)
        {
            newArray[i] = objArray[i];
        }
        objArray = newArray;
        capacity = newCapacity;
    }
}

