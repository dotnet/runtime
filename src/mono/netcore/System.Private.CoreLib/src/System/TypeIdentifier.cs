// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Copyright (C) 2015 Xamarin, Inc (http://www.xamarin.com)
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

using System.Diagnostics.CodeAnalysis;

namespace System
{
    // A TypeName is wrapper around type names in display form
    // (that is, with special characters escaped).
    //
    // Note that in general if you unescape a type name, you will
    // lose information: If the type name's DisplayName is
    // Foo\+Bar+Baz (outer class ``Foo+Bar``, inner class Baz)
    // unescaping the first plus will give you (outer class Foo,
    // inner class Bar, innermost class Baz).
    //
    // The correct way to take a TypeName apart is to feed its
    // DisplayName to TypeSpec.Parse()
    //
    internal interface ITypeName : IEquatable<ITypeName>
    {
        string DisplayName
        {
            get;
        }

        // add a nested name under this one.
        ITypeName NestedName(ITypeIdentifier innerName);
    }

    // A type identifier is a single component of a type name.
    // Unlike a general typename, a type identifier can be be
    // converted to internal form without loss of information.
    internal interface ITypeIdentifier : ITypeName
    {
        string InternalName
        {
            get;
        }
    }

    internal static class TypeNames
    {
        internal static ITypeName FromDisplay(string displayName)
        {
            return new Display(displayName);
        }

        internal abstract class ATypeName : ITypeName
        {
            public abstract string DisplayName { get; }

            public abstract ITypeName NestedName(ITypeIdentifier innerName);

            public bool Equals(ITypeName? other)
            {
                return other != null && DisplayName == other.DisplayName;
            }

            public override int GetHashCode()
            {
                return DisplayName.GetHashCode();
            }

            public override bool Equals(object? other)
            {
                return Equals(other as ITypeName);
            }
        }

        private class Display : ATypeName
        {
            private readonly string displayName;

            internal Display(string displayName)
            {
                this.displayName = displayName;
            }

            public override string DisplayName { get { return displayName; } }

            public override ITypeName NestedName(ITypeIdentifier innerName)
            {
                return new Display(DisplayName + "+" + innerName.DisplayName);
            }

        }
    }

    internal static class TypeIdentifiers
    {
        internal static ITypeIdentifier FromDisplay(string displayName)
        {
            return new Display(displayName);
        }

        internal static ITypeIdentifier FromInternal(string internalName)
        {
            return new Internal(internalName);
        }

        internal static ITypeIdentifier FromInternal(string internalNameSpace, ITypeIdentifier typeName)
        {
            return new Internal(internalNameSpace, typeName);
        }

        // Only use if simpleName is certain not to contain
        // unexpected characters that ordinarily require
        // escaping: ,+*&[]\
        internal static ITypeIdentifier WithoutEscape(string simpleName)
        {
            return new NoEscape(simpleName);
        }

        private class Display : TypeNames.ATypeName, ITypeIdentifier
        {
            private readonly string displayName;
            private string? internal_name; //cached

            internal Display(string displayName)
            {
                this.displayName = displayName;
            }

            public override string DisplayName
            {
                get { return displayName; }
            }

            public string InternalName => internal_name ??= GetInternalName();

            private string GetInternalName() => TypeSpec.UnescapeInternalName(displayName);

            public override ITypeName NestedName(ITypeIdentifier innerName) =>
                TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
        }

        private class Internal : TypeNames.ATypeName, ITypeIdentifier
        {
            private readonly string internalName;
            private string? display_name; //cached

            internal Internal(string internalName)
            {
                this.internalName = internalName;
            }

            internal Internal(string nameSpaceInternal, ITypeIdentifier typeName)
            {
                this.internalName = nameSpaceInternal + "." + typeName.InternalName;
            }

            public override string DisplayName => display_name ??= GetDisplayName();

            public string InternalName => internalName;

            private string GetDisplayName() => TypeSpec.EscapeDisplayName(internalName);

            public override ITypeName NestedName(ITypeIdentifier innerName) =>
                TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
        }

        private class NoEscape : TypeNames.ATypeName, ITypeIdentifier
        {
            private readonly string simpleName;
            internal NoEscape(string simpleName)
            {
                this.simpleName = simpleName;
            }

            public override string DisplayName { get { return simpleName; } }
            public string InternalName { get { return simpleName; } }

            public override ITypeName NestedName(ITypeIdentifier innerName)
            {
                return TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
            }
        }
    }
}
