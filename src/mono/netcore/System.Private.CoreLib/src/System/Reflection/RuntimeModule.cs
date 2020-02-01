//
// Copyright (C) 2010 Novell, Inc (http://www.novell.com)
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

#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Reflection {

	[StructLayout (LayoutKind.Sequential)]
	class RuntimeModule : Module
	{
#pragma warning disable 649
		#region Sync with object-internals.h
		#region Sync with ModuleBuilder
		internal IntPtr _impl; /* a pointer to a MonoImage */
		internal Assembly assembly;
		internal string fqname;
		internal string name;
		internal string scopename;
		internal bool is_resource;
		internal int token;
		#endregion
		#endregion
#pragma warning restore 649

		public
		override
		Assembly Assembly {
			get { return assembly; }
		}

		public
		override
		// Note: we do not ask for PathDiscovery because no path is returned here.
		// However MS Fx requires it (see FDBK23572 for details).
		string Name {
			get { return name; }
		}
	
		public
		override
		string ScopeName {
			get { return scopename; }
		}

		public
		override
		int MDStreamVersion {
			get {
				if (_impl == IntPtr.Zero)
					throw new NotSupportedException ();
				return GetMDStreamVersion (_impl);
			}
		}

		public
		override
		Guid ModuleVersionId {
			get {
				return GetModuleVersionId ();
			}
		}

		public override
		string FullyQualifiedName {
			get {
				return fqname;
			}
		}

		public
		override
		bool IsResource()
		{
			return is_resource;
		}

		public override
		Type[] FindTypes(TypeFilter filter, object filterCriteria) 
		{
			var filtered = new List<Type> ();
			Type[] types = GetTypes ();
			foreach (Type t in types)
				if (filter (t, filterCriteria))
					filtered.Add (t);
			return filtered.ToArray ();
		}

		public override
		object[] GetCustomAttributes(bool inherit) 
		{
			return CustomAttribute.GetCustomAttributes (this, inherit);
		}

		public override
		object[] GetCustomAttributes(Type attributeType, bool inherit) 
		{
			return CustomAttribute.GetCustomAttributes (this, attributeType, inherit);
		}

		public override
		FieldInfo GetField (string name, BindingFlags bindingAttr) 
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (IsResource ())
				return null;

			Type globalType = GetGlobalType (_impl);
			return (globalType != null) ? globalType.GetField (name, bindingAttr) : null;
		}

		public override
		FieldInfo[] GetFields (BindingFlags bindingFlags)
		{
			if (IsResource ())
				return new FieldInfo [0];

			Type globalType = GetGlobalType (_impl);
			return (globalType != null) ? globalType.GetFields (bindingFlags) : new FieldInfo [0];
		}

		public override
		int MetadataToken {
			get {
				return get_MetadataToken (this);
			}
		}

		protected
		override
		MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) 
		{
			if (IsResource ())
				return null;

			Type globalType = GetGlobalType (_impl);
			if (globalType == null)
				return null;
			if (types == null)
				return globalType.GetMethod (name);
			return globalType.GetMethod (name, bindingAttr, binder, callConvention, types, modifiers);
		}

		public
		override
		MethodInfo[] GetMethods (BindingFlags bindingFlags) {
			if (IsResource ())
				return new MethodInfo [0];

			Type globalType = GetGlobalType (_impl);
			return (globalType != null) ? globalType.GetMethods (bindingFlags) : new MethodInfo [0];
		}

		public override
		void GetPEKind (out PortableExecutableKinds peKind, out ImageFileMachine machine) {
			RuntimeModule.GetPEKind (_impl, out peKind, out machine);
		}

		public override
		Type GetType(string className, bool throwOnError, bool ignoreCase) 
		{
			if (className == null)
				throw new ArgumentNullException ("className");
			if (className == String.Empty)
				throw new ArgumentException ("Type name can't be empty");
			return assembly.InternalGetType (this, className, throwOnError, ignoreCase);
		}
	
		public override
		bool IsDefined (Type attributeType, bool inherit) 
		{
			return CustomAttribute.IsDefined (this, attributeType, inherit);
		}

		public
		override
		FieldInfo ResolveField (int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			return ResolveField (this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
		}

		internal static FieldInfo ResolveField (Module module, IntPtr monoModule, int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			ResolveTokenError error;

			IntPtr handle = ResolveFieldToken (monoModule, metadataToken, ptrs_from_types (genericTypeArguments), ptrs_from_types (genericMethodArguments), out error);
			if (handle == IntPtr.Zero)
				throw resolve_token_exception (module.Name, metadataToken, error, "Field");
			else
				return FieldInfo.GetFieldFromHandle (new RuntimeFieldHandle (handle));
		}

		public
		override
		MemberInfo ResolveMember (int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			return ResolveMember (this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
		}

		internal static MemberInfo ResolveMember (Module module, IntPtr monoModule, int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			ResolveTokenError error;

			MemberInfo m = ResolveMemberToken (monoModule, metadataToken, ptrs_from_types (genericTypeArguments), ptrs_from_types (genericMethodArguments), out error);
			if (m == null)
				throw resolve_token_exception (module.Name, metadataToken, error, "MemberInfo");
			else
				return m;
		}

		public
		override
		MethodBase ResolveMethod (int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			return ResolveMethod (this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
		}

		internal static MethodBase ResolveMethod (Module module, IntPtr monoModule, int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			ResolveTokenError error;

			IntPtr handle = ResolveMethodToken (monoModule, metadataToken, ptrs_from_types (genericTypeArguments), ptrs_from_types (genericMethodArguments), out error);
			if (handle == IntPtr.Zero)
				throw resolve_token_exception (module.Name, metadataToken, error, "MethodBase");
			else
				return RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (new RuntimeMethodHandle (handle));
		}

		public
		override
		string ResolveString (int metadataToken) {
			return ResolveString (this, _impl, metadataToken);
		}

		internal static string ResolveString (Module module, IntPtr monoModule, int metadataToken) {
			ResolveTokenError error;

			string s = ResolveStringToken (monoModule, metadataToken, out error);
			if (s == null)
				throw resolve_token_exception (module.Name, metadataToken, error, "string");
			else
				return s;
		}

		public
		override
		Type ResolveType (int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			return ResolveType (this, _impl,  metadataToken, genericTypeArguments, genericMethodArguments);
		}

		internal static Type ResolveType (Module module, IntPtr monoModule, int metadataToken, Type [] genericTypeArguments, Type [] genericMethodArguments) {
			ResolveTokenError error;

			IntPtr handle = ResolveTypeToken (monoModule, metadataToken, ptrs_from_types (genericTypeArguments), ptrs_from_types (genericMethodArguments), out error);
			if (handle == IntPtr.Zero)
				throw resolve_token_exception (module.Name, metadataToken, error, "Type");
			else
				return Type.GetTypeFromHandle (new RuntimeTypeHandle (handle));
		}

		public
		override
		byte[] ResolveSignature (int metadataToken) {
			return ResolveSignature (this, _impl, metadataToken);
		}

		internal static byte[] ResolveSignature (Module module, IntPtr monoModule, int metadataToken) {
			ResolveTokenError error;

		    byte[] res = ResolveSignature (monoModule, metadataToken, out error);
			if (res == null)
				throw resolve_token_exception (module.Name, metadataToken, error, "signature");
			else
				return res;
		}

		public override
		Type[] GetTypes() 
		{
			return InternalGetTypes (_impl);
		}

		public override IList<CustomAttributeData> GetCustomAttributesData () {
			return CustomAttributeData.GetCustomAttributes (this);
		}

		internal RuntimeAssembly GetRuntimeAssembly ()
		{
			return (RuntimeAssembly)assembly;
		}

		internal IntPtr MonoModule {
			get {
				return _impl;
			}
		}

		internal Guid GetModuleVersionId ()
		{
			var guid = new byte [16];
			GetGuidInternal (_impl, guid);
			return new Guid (guid);
		}

		internal static Exception resolve_token_exception (string name, int metadataToken, ResolveTokenError error, string tokenType) {
			if (error == ResolveTokenError.OutOfRange)
				return new ArgumentOutOfRangeException ("metadataToken", String.Format ("Token 0x{0:x} is not valid in the scope of module {1}", metadataToken, name));
			else
				return new ArgumentException (String.Format ("Token 0x{0:x} is not a valid {1} token in the scope of module {2}", metadataToken, tokenType, name), "metadataToken");
		}

		internal static IntPtr[] ptrs_from_types (Type[] types) {
			if (types == null)
				return null;
			else {
				IntPtr[] res = new IntPtr [types.Length];
				for (int i = 0; i < types.Length; ++i) {
					if (types [i] == null)
						throw new ArgumentException ();
					res [i] = types [i].TypeHandle.Value;
				}
				return res;
			}
		}

		// This calls ves_icall_reflection_get_token, so needs a Module argument
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern int get_MetadataToken (Module module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern int GetMDStreamVersion (IntPtr module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern Type[] InternalGetTypes (IntPtr module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern IntPtr GetHINSTANCE (IntPtr module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private static extern void GetGuidInternal (IntPtr module, byte[] guid);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern Type GetGlobalType (IntPtr module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern IntPtr ResolveTypeToken (IntPtr module, int token, IntPtr[] type_args, IntPtr[] method_args, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern IntPtr ResolveMethodToken (IntPtr module, int token, IntPtr[] type_args, IntPtr[] method_args, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern IntPtr ResolveFieldToken (IntPtr module, int token, IntPtr[] type_args, IntPtr[] method_args, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern string ResolveStringToken (IntPtr module, int token, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern MemberInfo ResolveMemberToken (IntPtr module, int token, IntPtr[] type_args, IntPtr[] method_args, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern byte[] ResolveSignature (IntPtr module, int metadataToken, out ResolveTokenError error);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern void GetPEKind (IntPtr module, out PortableExecutableKinds peKind, out ImageFileMachine machine);
	}

	internal enum ResolveTokenError {
		OutOfRange,
		BadTable,
		Other
	}	
}
