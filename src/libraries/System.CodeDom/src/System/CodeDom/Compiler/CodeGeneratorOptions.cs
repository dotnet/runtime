// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.CodeDom.Compiler
{
    public class CodeGeneratorOptions
    {
        private readonly ListDictionary _options = new ListDictionary();

        public CodeGeneratorOptions() { }

        public object this[string index]
        {
            get => _options[index];
            set => _options[index] = value;
        }

        public string IndentString
        {
            get
            {
                object o = _options[nameof(IndentString)];
                return o != null ? (string)o : "    ";
            }
            set => _options[nameof(IndentString)] = value;
        }

        public string BracingStyle
        {
            get
            {
                object o = _options[nameof(BracingStyle)];
                return o != null ? (string)o : "Block";
            }
            set => _options[nameof(BracingStyle)] = value;
        }

        public bool ElseOnClosing
        {
            get
            {
                object o = _options[nameof(ElseOnClosing)];
                return o != null ? (bool)o : false;
            }
            set => _options[nameof(ElseOnClosing)] = value;
        }

        public bool BlankLinesBetweenMembers
        {
            get
            {
                object o = _options[nameof(BlankLinesBetweenMembers)];
                return o != null ? (bool)o : true;
            }
            set => _options[nameof(BlankLinesBetweenMembers)] = value;
        }

        public bool VerbatimOrder
        {
            get
            {
                object o = _options[nameof(VerbatimOrder)];
                return o != null ? (bool)o : false;
            }
            set => _options[nameof(VerbatimOrder)] = value;
        }
    }
}
