// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Author:
//   Zoltan Varga (vargaz@gmail.com)
//   Carlos Alberto Cortez (calberto.cortez@gmail.com)
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection
{
    internal sealed class RuntimeCustomAttributeData : CustomAttributeData
    {
        private sealed class LazyCAttrData
        {
            internal Assembly assembly = null!; // only call site always sets it
            internal IntPtr data;
            internal uint data_length;
        }

        private ConstructorInfo ctorInfo = null!;
        private IList<CustomAttributeTypedArgument> ctorArgs = null!;
        private IList<CustomAttributeNamedArgument> namedArgs = null!;
        private LazyCAttrData? lazyData;

        // custom-attrs.c:create_custom_attr_data ()
        internal RuntimeCustomAttributeData(ConstructorInfo ctorInfo, Assembly assembly, IntPtr data, uint data_length)
        {
            this.ctorInfo = ctorInfo;
            this.lazyData = new LazyCAttrData();
            this.lazyData.assembly = assembly;
            this.lazyData.data = data;
            this.lazyData.data_length = data_length;
        }

        internal RuntimeCustomAttributeData(ConstructorInfo ctorInfo)
            : this(ctorInfo, Array.Empty<CustomAttributeTypedArgument>(), Array.Empty<CustomAttributeNamedArgument>())
        {
        }

        internal RuntimeCustomAttributeData(ConstructorInfo ctorInfo, IList<CustomAttributeTypedArgument> ctorArgs, IList<CustomAttributeNamedArgument> namedArgs)
        {
            this.ctorInfo = ctorInfo;
            this.ctorArgs = ctorArgs;
            this.namedArgs = namedArgs;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ResolveArgumentsInternal(ConstructorInfo ctor, Assembly assembly, IntPtr data, uint data_length, out object[] ctorArgs, out object[] namedArgs);

        private void ResolveArguments()
        {
            object[] ctor_args, named_args;
            if (lazyData == null)
                return;

            ResolveArgumentsInternal(ctorInfo, lazyData.assembly, lazyData.data, lazyData.data_length, out ctor_args, out named_args);

            this.ctorArgs = Array.AsReadOnly<CustomAttributeTypedArgument>
                (ctor_args != null ? UnboxValues<CustomAttributeTypedArgument>(ctor_args) : Array.Empty<CustomAttributeTypedArgument>());
            this.namedArgs = Array.AsReadOnly<CustomAttributeNamedArgument>
                (named_args != null ? UnboxValues<CustomAttributeNamedArgument>(named_args) : Array.Empty<CustomAttributeNamedArgument>());

            lazyData = null;
        }

        public
        override
        ConstructorInfo Constructor
        {
            get
            {
                return ctorInfo;
            }
        }

        public
        override
        IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                ResolveArguments();
                return ctorArgs;
            }
        }

        public
        override
        IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                ResolveArguments();
                return namedArgs;
            }
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        private static T[] UnboxValues<T>(object[] values)
        {
            T[] retval = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
                retval[i] = (T)values[i];

            return retval;
        }
    }

}
