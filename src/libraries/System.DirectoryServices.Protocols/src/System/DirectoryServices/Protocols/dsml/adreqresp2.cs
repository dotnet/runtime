// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Runtime.InteropServices;

    //ADSearch Operation related classes 
    internal class ADFilter
    {
        public ADFilter()
        {
            Filter = new FilterContent();
        }

        public FilterType Type;
        public FilterContent Filter;

        public enum FilterType
        {
            And, Or, Not,
            EqualityMatch, Substrings, GreaterOrEqual,
            LessOrEqual, Present, ApproxMatch, ExtensibleMatch
        }

        public struct FilterContent
        {
            public ArrayList And;       //type ADFilter

            public ArrayList Or;        //type ADFilter

            public ADFilter Not;

            public ADAttribute EqualityMatch;

            public ADSubstringFilter Substrings;

            public ADAttribute GreaterOrEqual;

            public ADAttribute LessOrEqual;

            public string Present;

            public ADAttribute ApproxMatch;

            public ADExtenMatchFilter ExtensibleMatch;
        }
    }

    internal class ADExtenMatchFilter
    {
        public ADExtenMatchFilter()
        {
            Value = null;
            DNAttributes = false;
        }

        public string Name;
        public ADValue Value;
        public bool DNAttributes;
        public string MatchingRule;
    }

    internal class ADSubstringFilter
    {
        public ADSubstringFilter()
        {
            Initial = null;
            Final = null;
            Any = new ArrayList();
        }

        public string Name;
        public ADValue Initial;
        public ADValue Final;
        public ArrayList Any;
    }
    //end ADSearchOperation classes

    internal class ADAttribute
    {
        public ADAttribute()
        {
            Values = new ArrayList();
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public string Name;
        public ArrayList Values;            //type ADValue
    }

    internal class ADValue
    {
        public ADValue()
        {
            IsBinary = false;
            BinaryVal = null;
        }

        public bool IsBinary;
        public string StringVal;
        public byte[] BinaryVal;    //to store base64 encoded data
    }
}
