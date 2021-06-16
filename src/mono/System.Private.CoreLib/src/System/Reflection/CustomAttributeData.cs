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
    public class CustomAttributeData
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

        protected CustomAttributeData()
        {
        }

        // custom-attrs.c:create_custom_attr_data ()
        internal CustomAttributeData(ConstructorInfo ctorInfo, Assembly assembly, IntPtr data, uint data_length)
        {
            this.ctorInfo = ctorInfo;
            this.lazyData = new LazyCAttrData();
            this.lazyData.assembly = assembly;
            this.lazyData.data = data;
            this.lazyData.data_length = data_length;
        }

        internal CustomAttributeData(ConstructorInfo ctorInfo)
            : this(ctorInfo, Array.Empty<CustomAttributeTypedArgument>(), Array.Empty<CustomAttributeNamedArgument>())
        {
        }

        internal CustomAttributeData(ConstructorInfo ctorInfo, IList<CustomAttributeTypedArgument> ctorArgs, IList<CustomAttributeNamedArgument> namedArgs)
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
        virtual
        ConstructorInfo Constructor
        {
            get
            {
                return ctorInfo;
            }
        }

        public
        virtual
        IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                ResolveArguments();
                return ctorArgs;
            }
        }

        public
        virtual
        IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                ResolveArguments();
                return namedArgs;
            }
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Module target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
        {
            return CustomAttribute.GetCustomAttributesData(target);
        }

        public virtual Type AttributeType
        {
            get { return ctorInfo.DeclaringType!; }
        }

        public override string ToString()
        {
            ResolveArguments();

            StringBuilder sb = new StringBuilder();

            sb.Append('[').Append(ctorInfo.DeclaringType!.FullName).Append('(');
            for (int i = 0; i < ctorArgs.Count; i++)
            {
                sb.Append(ctorArgs[i].ToString());
                if (i + 1 < ctorArgs.Count)
                    sb.Append(", ");
            }

            if (namedArgs.Count > 0)
                sb.Append(", ");

            for (int j = 0; j < namedArgs.Count; j++)
            {
                sb.Append(namedArgs[j].ToString());
                if (j + 1 < namedArgs.Count)
                    sb.Append(", ");
            }
            sb.Append(")]");

            return sb.ToString();
        }

        private static T[] UnboxValues<T>(object[] values)
        {
            T[] retval = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
                retval[i] = (T)values[i];

            return retval;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool Equals(object? obj)
        {
            return obj == (object)this;
        }
    }

}
