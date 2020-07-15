// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// DynamicMethod.cs
//
// Authors:
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright (C) 2016 Xamarin Inc (http://www.xamarin.com)
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

#if !MONO_FEATURE_SRE

using System.Globalization;

namespace System.Reflection.Emit
{
	public sealed class DynamicMethod : MethodInfo
	{
		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes, bool restrictedSkipVisibility)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes, Module m)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes, Type owner)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
		{
			throw new PlatformNotSupportedException ();
		}

		public DynamicMethod (string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
		{
			throw new PlatformNotSupportedException ();
		}

		public override MethodAttributes Attributes { 
			get {
				throw new PlatformNotSupportedException ();
			}
		}

		public override CallingConventions CallingConvention {
			get {
				throw new PlatformNotSupportedException ();
			}
		}

		public override Type? DeclaringType {
			get {
				throw new PlatformNotSupportedException ();
			}
		}

		public bool InitLocals { get; set; }

		public override MethodImplAttributes MethodImplementationFlags {
			get {
				throw new PlatformNotSupportedException ();
			}				
		}

		public override string Name {
			get {
				throw new PlatformNotSupportedException ();
			}
		}				

		public override ParameterInfo ReturnParameter {
			get {
				throw new PlatformNotSupportedException ();
			}
		}

		public override Type? ReturnType {
			get {
				throw new PlatformNotSupportedException ();
			}
		}

		public ILGenerator GetILGenerator ()
		{
			throw new PlatformNotSupportedException ();
		}

		public ILGenerator GetILGenerator (int streamSize)
		{
			throw new PlatformNotSupportedException ();
		}

		public override ParameterInfo[] GetParameters ()
		{
			throw new PlatformNotSupportedException ();
		}

		public override RuntimeMethodHandle MethodHandle { get { throw new PlatformNotSupportedException (); } }
		public override Type ReflectedType { get { throw new PlatformNotSupportedException (); } }
		public override ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw new PlatformNotSupportedException (); } }

		public override object[] GetCustomAttributes (bool inherit) { throw new PlatformNotSupportedException (); }
		public override object[] GetCustomAttributes (Type attributeType, bool inherit) { throw new PlatformNotSupportedException (); }
		public override MethodImplAttributes GetMethodImplementationFlags () { throw new PlatformNotSupportedException (); }
		public override MethodInfo GetBaseDefinition () { throw new PlatformNotSupportedException (); }

		public override object? Invoke (object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) { throw new PlatformNotSupportedException (); }

		public override bool IsDefined (Type attributeType, bool inherit) { throw new PlatformNotSupportedException (); }

		public ParameterBuilder? DefineParameter (int position, ParameterAttributes attributes, string? parameterName) => throw new PlatformNotSupportedException ();
		public DynamicILInfo GetDynamicILInfo () => throw new PlatformNotSupportedException ();
	}
}

#endif
