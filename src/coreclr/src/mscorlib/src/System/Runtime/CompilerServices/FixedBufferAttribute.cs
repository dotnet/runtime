// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: Used by a compiler for generating value types
**   in-place within other value types containing a certain
**   number of elements of the given (primitive) type.  Somewhat
**   similar to P/Invoke's ByValTStr attribute.
**   Used by C# with this syntax: "fixed int buffer[10];"
**
===========================================================*/
using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Field, Inherited=false)]
    public sealed class FixedBufferAttribute : Attribute
    {
        private Type elementType;
        private int length;

        public FixedBufferAttribute(Type elementType, int length)
        {
            this.elementType = elementType;
            this.length = length;
        }

        public Type ElementType {
            get {
                return elementType;
            }
        }

        public int Length {
            get {
                return length;
            }
        }
    }
}
