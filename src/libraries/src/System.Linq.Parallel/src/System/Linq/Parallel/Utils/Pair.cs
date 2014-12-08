// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Pair.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq.Parallel
{
    /// <summary>
    /// A pair just wraps two bits of data into a single addressable unit. This is a
    /// value type to ensure it remains very lightweight, since it is frequently used
    /// with other primitive data types as well.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    internal struct Pair<T, U>
    {
        // The first and second bits of data.
        internal T _first;
        internal U _second;

        //-----------------------------------------------------------------------------------
        // A simple constructor that initializes the first/second fields.
        //

        public Pair(T first, U second)
        {
            _first = first;
            _second = second;
        }

        //-----------------------------------------------------------------------------------
        // Accessors for the left and right data.
        //

        public T First
        {
            get { return _first; }
            set { _first = value; }
        }

        public U Second
        {
            get { return _second; }
            set { _second = value; }
        }
    }

    //Non-generic version to avoid cycles when doing static analysis in NUTC.
    internal struct Pair
    {
        // The first and second bits of data.
        internal object _first;
        internal object _second;

        //-----------------------------------------------------------------------------------
        // A simple constructor that initializes the first/second fields.
        //

        public Pair(object first, object second)
        {
            _first = first;
            _second = second;
        }

        //-----------------------------------------------------------------------------------
        // Accessors for the left and right data.
        //

        public object First
        {
            get { return _first; }
            set { _first = value; }
        }

        public object Second
        {
            get { return _second; }
            set { _second = value; }
        }
    }
}