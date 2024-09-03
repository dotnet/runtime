// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
// Copyright (C) 2016 Quamotion
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Runtime.Serialization;

namespace Claunia.PropertyList
{
    /// <summary>The exception that is thrown when an property list file could not be processed correctly.</summary>
    [Serializable]
    public class PropertyListException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        public PropertyListException() {}

        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public PropertyListException(string message) : base(message) {}

        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">
        ///     The exception that is the cause of the current exception, or <see langword="null" /> if no inner
        ///     exception is specified.
        /// </param>
        public PropertyListException(string message, Exception inner) : base(message, inner) {}

        protected PropertyListException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}