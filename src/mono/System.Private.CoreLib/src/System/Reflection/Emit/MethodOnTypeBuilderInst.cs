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

#if MONO_FEATURE_SRE
using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    /*
     * This class represents a method of an instantiation of a generic type builder.
     */
    internal sealed class MethodOnTypeBuilderInst : MethodInfo
    {
        private Type instantiation;
        private MethodInfo base_method; /*This is the base method definition, it must be non-inflated and belong to a non-inflated type.*/
        private Type[]? method_arguments;

        private MethodInfo? generic_method_definition;

        internal MethodOnTypeBuilderInst(Type instantiation, MethodInfo base_method)
        {
            this.instantiation = instantiation;
            this.base_method = base_method;
        }

        internal MethodOnTypeBuilderInst(MethodOnTypeBuilderInst gmd, Type[] typeArguments)
            : this(gmd.instantiation, gmd.base_method)
        {
            this.method_arguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(this.method_arguments, 0);
            this.generic_method_definition = gmd;
        }

        internal MethodOnTypeBuilderInst(MethodInfo method, Type[] typeArguments)
            : this(method.DeclaringType!, ExtractBaseMethod(method))
        {
            this.method_arguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(this.method_arguments, 0);
            if (base_method != method)
                this.generic_method_definition = method;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Reflection.Emit is not subject to trimming")]
        private static MethodInfo ExtractBaseMethod(MethodInfo info)
        {
            if (info is MethodBuilder)
                return info;
            if (info is MethodOnTypeBuilderInst)
                return ((MethodOnTypeBuilderInst)info).base_method;

            if (info.IsGenericMethod)
                info = info.GetGenericMethodDefinition();

            Type t = info.DeclaringType!;
            if (!t.IsGenericType || t.IsGenericTypeDefinition)
                return info;

            return (MethodInfo)t.Module.ResolveMethod(info.MetadataToken)!;
        }

        internal Type[]? GetTypeArgs()
        {
            if (!instantiation.IsGenericType || instantiation.IsGenericParameter)
                return null;

            return instantiation.GetGenericArguments();
        }

        // Called from the runtime to return the corresponding finished MethodInfo object
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "MethodOnTypeBuilderInst is Reflection.Emit's underlying implementation of MakeGenericMethod. " +
                "Callers of the outer calls to MakeGenericMethod will be warned as appropriate.")]
        internal MethodInfo RuntimeResolve()
        {
            Type type = instantiation.InternalResolve();
            MethodInfo m = type.GetMethod(base_method);
            if (method_arguments != null)
            {
                var args = new Type[method_arguments.Length];
                for (int i = 0; i < method_arguments.Length; ++i)
                    args[i] = method_arguments[i].InternalResolve();
                m = m.MakeGenericMethod(args);
            }
            return m;
        }

        //
        // MemberInfo members
        //

        public override Type DeclaringType
        {
            get
            {
                return instantiation;
            }
        }

        public override string Name
        {
            get
            {
                return base_method.Name;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return instantiation;
            }
        }

        public override Type ReturnType
        {
            get
            {
                return base_method.ReturnType;
            }
        }

        public override Module Module
        {
            get
            {
                return base_method.Module;
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            //IEnumerable`1 get_Item(TKey)
            StringBuilder sb = new StringBuilder(ReturnType.ToString());
            sb.Append(' ');
            sb.Append(base_method.Name);
            sb.Append('(');
            sb.Append(')');
            return sb.ToString();
        }
        //
        // MethodBase members
        //

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return base_method.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return GetParametersInternal();
        }

        internal override ParameterInfo[] GetParametersInternal()
        {
            throw new NotSupportedException();
        }

        public override int MetadataToken
        {
            get
            {
                return base.MetadataToken;
            }
        }

        internal override int GetParametersCount()
        {
            return base_method.GetParametersCount();
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return base_method.Attributes;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return base_method.CallingConvention;
            }
        }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
        {
            if (!base_method.IsGenericMethodDefinition || (method_arguments != null))
                throw new InvalidOperationException("Method is not a generic method definition");

            ArgumentNullException.ThrowIfNull(methodInstantiation);

            if (base_method.GetGenericArguments().Length != methodInstantiation.Length)
                throw new ArgumentException("Incorrect length", nameof(methodInstantiation));

            foreach (Type type in methodInstantiation)
            {
                ArgumentNullException.ThrowIfNull(type, nameof(methodInstantiation));
            }

            return new MethodOnTypeBuilderInst(this, methodInstantiation);
        }

        public override Type[] GetGenericArguments()
        {
            if (!base_method.IsGenericMethodDefinition)
                return Type.EmptyTypes;
            Type[] source = method_arguments ?? base_method.GetGenericArguments();
            Type[] result = new Type[source.Length];
            source.CopyTo(result, 0);
            return result;
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return generic_method_definition ?? base_method;
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                if (base_method.ContainsGenericParameters)
                    return true;
                if (!base_method.IsGenericMethodDefinition)
                    throw new NotSupportedException();
                if (method_arguments == null)
                    return true;
                foreach (Type t in method_arguments)
                {
                    if (t.ContainsGenericParameters)
                        return true;
                }
                return false;
            }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return base_method.IsGenericMethodDefinition && method_arguments == null;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return base_method.IsGenericMethodDefinition;
            }
        }

        //
        // MethodInfo members
        //

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotSupportedException();
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}

#endif
