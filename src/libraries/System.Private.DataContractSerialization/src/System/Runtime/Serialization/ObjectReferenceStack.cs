// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    internal struct ObjectReferenceStack
    {
        private const int MaximumArraySize = 16;
        private const int InitialArraySize = 4;

        private int _count;
        private object[]? _objectArray;
        private bool[]? _isReferenceArray;
        private Dictionary<object, object?>? _objectDictionary;

        internal void Push(object obj)
        {
            if (_objectArray == null)
            {
                _objectArray = new object[InitialArraySize];
                _objectArray[_count++] = obj;
            }
            else if (_count < MaximumArraySize)
            {
                if (_count == _objectArray.Length)
                    Array.Resize<object>(ref _objectArray, _objectArray.Length * 2);
                _objectArray[_count++] = obj;
            }
            else
            {
                _objectDictionary ??= new Dictionary<object, object?>();

                _objectDictionary.Add(obj, null);
                _count++;
            }
        }

        internal void EnsureSetAsIsReference(object obj)
        {
            if (_count == 0)
                return;
            if (_count > MaximumArraySize)
            {
                if (_objectDictionary == null)
                {
                    Debug.Fail("Object reference stack in invalid state");
                }
                _objectDictionary.Remove(obj);
            }
            else
            {
                if ((_objectArray != null) && _objectArray[_count - 1] == obj)
                {
                    if (_isReferenceArray == null)
                    {
                        _isReferenceArray = new bool[_objectArray.Length];
                    }
                    else if (_count >= _isReferenceArray.Length)
                    {
                        Array.Resize<bool>(ref _isReferenceArray, _objectArray.Length);
                    }
                    _isReferenceArray[_count - 1] = true;
                }
            }
        }

        internal void Pop(object obj)
        {
            if (_count > MaximumArraySize)
            {
                if (_objectDictionary == null)
                {
                    Debug.Fail("Object reference stack in invalid state");
                }
                _objectDictionary.Remove(obj);
            }

            _count--;
        }

        internal bool Contains(object obj)
        {
            int currentCount = _count;
            if (currentCount > MaximumArraySize)
            {
                if (_objectDictionary != null && _objectDictionary.ContainsKey(obj))
                    return true;
                currentCount = MaximumArraySize;
            }
            for (int i = (currentCount - 1); i >= 0; i--)
            {
                if (object.ReferenceEquals(obj, _objectArray![i]) && _isReferenceArray != null && !_isReferenceArray[i])
                    return true;
            }
            return false;
        }
    }
}
