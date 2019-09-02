// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
	partial class Type
	{
		#region keep in sync with object-internals.h
		internal RuntimeTypeHandle _impl;
		#endregion

		internal bool IsRuntimeImplemented () => this.UnderlyingSystemType is RuntimeType;

		public bool IsInterface {
			get {
				if (this is RuntimeType rt)
					return RuntimeTypeHandle.IsInterface (rt);

				return (GetAttributeFlagsImpl () & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
			}
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName, bool throwOnError, bool ignoreCase)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, throwOnError, ignoreCase, false, ref stackMark);
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName, bool throwOnError)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, throwOnError, false, false, ref stackMark);
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, false, false, false, ref stackMark);
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, assemblyResolver, typeResolver, false, false, ref stackMark);
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackMark);
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		public static Type GetType (string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType (typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
		}

		static Type GetType (string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark) {
			return TypeNameParser.GetType (typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
		}

		public static Type? GetTypeFromHandle (RuntimeTypeHandle handle)
		{
			if (handle.Value == IntPtr.Zero)
				return null;

			return internal_from_handle (handle.Value);
		}

		public static Type GetTypeFromCLSID (Guid clsid, string? server, bool throwOnError) => throw new PlatformNotSupportedException ();

		public static Type GetTypeFromProgID (string progID, string? server, bool throwOnError) => throw new PlatformNotSupportedException ();

		internal virtual Type InternalResolve ()
		{
			return UnderlyingSystemType;
		}

		// Called from the runtime to return the corresponding finished Type object
		internal virtual Type RuntimeResolve ()
		{
			throw new NotImplementedException ();
		}

		internal virtual bool IsUserType {
			get {
				return true;
			}
		}

		internal virtual MethodInfo GetMethod (MethodInfo fromNoninstanciated)
		{
			throw new InvalidOperationException ("can only be called in generic type");
		}

		internal virtual ConstructorInfo GetConstructor (ConstructorInfo fromNoninstanciated)
		{
			throw new InvalidOperationException ("can only be called in generic type");
		}

		internal virtual FieldInfo GetField (FieldInfo fromNoninstanciated)
		{
			throw new InvalidOperationException ("can only be called in generic type");
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern Type internal_from_handle (IntPtr handle);

		public static bool operator == (Type? left, Type? right)
		{
			return object.ReferenceEquals (left, right);
		}

		public static bool operator != (Type? left, Type? right)
		{
			return !object.ReferenceEquals (left, right);
		}
	}
}
