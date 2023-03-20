// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/MethodOnTypeBuilderInst.cs
//
// Author:
//   Zoltan Varga (vargaz@gmail.com)
//
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

namespace System.Reflection.Emit
{
    internal partial class MethodOnTypeBuilderInstantiation
    {
        private Type[]? _typeArguments;
        private MethodInfo? _genericMethodDefinition;

        internal MethodOnTypeBuilderInstantiation(MethodOnTypeBuilderInstantiation gmd, Type[] typeArguments)
            : this(gmd._method, gmd._type)
        {
            _typeArguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(_typeArguments, 0);
            _genericMethodDefinition = gmd;
        }

        internal MethodOnTypeBuilderInstantiation(MethodInfo method, Type[] typeArguments)
            : this(ExtractBaseMethod(method), method.DeclaringType!)
        {
            _typeArguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(_typeArguments, 0);
            if (_method != method)
                _genericMethodDefinition = method;
        }

        public override Type[] GetGenericArguments()
        {
            if (!_method.IsGenericMethodDefinition)
                return Type.EmptyTypes;
            Type[] source = _typeArguments ?? _method.GetGenericArguments();
            Type[] result = new Type[source.Length];
            source.CopyTo(result, 0);
            return result;
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                if (_method.ContainsGenericParameters)
                    return true;
                if (!_method.IsGenericMethodDefinition)
                    throw new NotSupportedException();
                if (_typeArguments == null)
                    return true;
                foreach (Type t in _typeArguments)
                {
                    if (t.ContainsGenericParameters)
                        return true;
                }
                return false;
            }
        }

        public override bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition && _typeArguments == null;

        public override MethodInfo GetGenericMethodDefinition() { return _genericMethodDefinition ?? _method; }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
        {
            if (!_method.IsGenericMethodDefinition || (_typeArguments != null))
                throw new InvalidOperationException(SR.Argument_NeedGenericMethodDefinition);

            ArgumentNullException.ThrowIfNull(typeArgs);

            if (_method.GetGenericArguments().Length != typeArgs.Length)
                throw new ArgumentException(SR.Format(SR.Argument_NotEnoughGenArguments, _method.GetGenericArguments().Length, typeArgs.Length));

            foreach (Type type in typeArgs)
            {
                ArgumentNullException.ThrowIfNull(type, nameof(typeArgs));
            }

            return new MethodOnTypeBuilderInstantiation(this, typeArgs);
        }

        // Called from the runtime to return the corresponding finished MethodInfo object
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "MethodOnTypeBuilderInst is Reflection.Emit's underlying implementation of MakeGenericMethod. " +
                "Callers of the outer calls to MakeGenericMethod will be warned as appropriate.")]
        internal MethodInfo RuntimeResolve()
        {
            Type type = _type.InternalResolve();
            MethodInfo m = type.GetMethod(_method);
            if (_typeArguments != null)
            {
                var args = new Type[_typeArguments.Length];
                for (int i = 0; i < _typeArguments.Length; ++i)
                    args[i] = _typeArguments[i].InternalResolve();
                m = m.MakeGenericMethod(args);
            }
            return m;
        }

        internal override int GetParametersCount()
        {
            return _method.GetParametersCount();
        }
    }
}
