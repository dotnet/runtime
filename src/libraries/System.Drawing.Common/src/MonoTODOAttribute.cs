// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// MonoTODOAttribute.cs
//
// Authors:
//   Ravi Pratap (ravi@ximian.com)
//   Eyal Alaluf <eyala@mainsoft.com> 
//
// (C) Ximian, Inc.  http://www.ximian.com
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2006 Mainsoft, Inc (http://www.mainsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Diagnostics;

namespace System
{
#pragma warning disable 436
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [Conditional("MONO")]
    internal class MonoTODOAttribute : Attribute
    {

        string comment;

        public MonoTODOAttribute()
        {
        }

        public MonoTODOAttribute(string comment)
        {
            this.comment = comment;
        }

        public string Comment
        {
            get { return comment; }
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class MonoDocumentationNoteAttribute : MonoTODOAttribute
    {

        public MonoDocumentationNoteAttribute(string comment)
            : base(comment)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class MonoExtensionAttribute : MonoTODOAttribute
    {

        public MonoExtensionAttribute(string comment)
            : base(comment)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class MonoInternalNoteAttribute : MonoTODOAttribute
    {

        public MonoInternalNoteAttribute(string comment)
            : base(comment)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class MonoLimitationAttribute : MonoTODOAttribute
    {

        public MonoLimitationAttribute(string comment)
            : base(comment)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class MonoNotSupportedAttribute : MonoTODOAttribute
    {

        public MonoNotSupportedAttribute(string comment)
            : base(comment)
        {
        }
    }
#pragma warning restore 436    
}
