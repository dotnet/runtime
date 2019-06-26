#nullable disable

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

//
// System.Reflection.Emit/FieldBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001-2002 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit {
	[StructLayout (LayoutKind.Sequential)]
	public sealed partial class FieldBuilder : FieldInfo {
	
#pragma warning disable 169, 414
		private FieldAttributes attrs;
		private Type type;
		private String name;
		private object def_value;
		private int offset;
		internal TypeBuilder typeb;
		private byte[] rva_data;
		private CustomAttributeBuilder[] cattrs;
		private UnmanagedMarshal marshal_info;
		private RuntimeFieldHandle handle;
		private Type[] modReq;
		private Type[] modOpt;
#pragma warning restore 169, 414

		internal FieldBuilder (TypeBuilder tb, string fieldName, Type type, FieldAttributes attributes, Type[] modReq, Type[] modOpt)
		{
			if (type == null)
				throw new ArgumentNullException ("type");

			attrs = attributes;
			name = fieldName;
			this.type = type;
			this.modReq = modReq;
			this.modOpt = modOpt;
			offset = -1;
			typeb = tb;

			((ModuleBuilder) tb.Module).RegisterToken (this, GetToken ().Token);
		}

		public override FieldAttributes Attributes {
			get { return attrs; }
		}

		public override Type DeclaringType {
			get { return typeb; }
		}

		public override RuntimeFieldHandle FieldHandle {
			get {
				throw CreateNotSupportedException ();
			}
		}

		public override Type FieldType {
			get { return type; }
		}

		public override string Name {
			get { return name; }
		}

		public override Type ReflectedType {
			get { return typeb; }
		}

		public override object[] GetCustomAttributes(bool inherit) {
			/*
			 * On MS.NET, this always returns not_supported, but we can't do this
			 * since there would be no way to obtain custom attributes of 
			 * dynamically created ctors.
			 */
			if (typeb.is_created)
				return CustomAttribute.GetCustomAttributes (this, inherit);
			else
				throw CreateNotSupportedException ();
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			if (typeb.is_created)
				return CustomAttribute.GetCustomAttributes (this, attributeType, inherit);
			else
				throw CreateNotSupportedException ();
		}

		public override int MetadataToken { get { return ((ModuleBuilder) typeb.Module).GetToken (this); } }

		public FieldToken GetToken() {
			return new FieldToken (MetadataToken, type);
		}

		public override object GetValue(object obj) {
			throw CreateNotSupportedException ();
		}

		public override bool IsDefined( Type attributeType, bool inherit) {
			throw CreateNotSupportedException ();
		}

		internal override int GetFieldOffset () {
			/* FIXME: */
			return 0;
		}

		internal void SetRVAData (byte[] data) {
			rva_data = (byte[])data.Clone ();
		}

		public void SetConstant( object defaultValue) {
			RejectIfCreated ();

			/*if (defaultValue.GetType() != type)
				throw new ArgumentException ("Constant doesn't match field type");*/
			def_value = defaultValue;
		}

		public void SetCustomAttribute (CustomAttributeBuilder customBuilder) {
			RejectIfCreated ();

			if (customBuilder == null)
				throw new ArgumentNullException ("customBuilder");

			string attrname = customBuilder.Ctor.ReflectedType.FullName;
			if (attrname == "System.Runtime.InteropServices.FieldOffsetAttribute") {
				byte[] data = customBuilder.Data;
				offset = (int)data [2];
				offset |= ((int)data [3]) << 8;
				offset |= ((int)data [4]) << 16;
				offset |= ((int)data [5]) << 24;
				return;
			} else if (attrname == "System.NonSerializedAttribute") {
				attrs |= FieldAttributes.NotSerialized;
				return;
			} else if (attrname == "System.Runtime.CompilerServices.SpecialNameAttribute") {
				attrs |= FieldAttributes.SpecialName;
				return;
			} else if (attrname == "System.Runtime.InteropServices.MarshalAsAttribute") {
				attrs |= FieldAttributes.HasFieldMarshal;
				marshal_info = CustomAttributeBuilder.get_umarshal (customBuilder, true);
				/* FIXME: check for errors */
				return;
			}
			if (cattrs != null) {
				CustomAttributeBuilder[] new_array = new CustomAttributeBuilder [cattrs.Length + 1];
				cattrs.CopyTo (new_array, 0);
				new_array [cattrs.Length] = customBuilder;
				cattrs = new_array;
			} else {
				cattrs = new CustomAttributeBuilder [1];
				cattrs [0] = customBuilder;
			}
		}

		[ComVisible (true)]
		public void SetCustomAttribute( ConstructorInfo con, byte[] binaryAttribute) {
			RejectIfCreated ();
			SetCustomAttribute (new CustomAttributeBuilder (con, binaryAttribute));
		}

		public void SetOffset( int iOffset) {
			RejectIfCreated ();
			if (iOffset < 0)
				throw new ArgumentException ("Negative field offset is not allowed");
			offset = iOffset;
		}

		public override void SetValue( object obj, object val, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
			throw CreateNotSupportedException ();
		}

		private Exception CreateNotSupportedException ()
		{
			return new NotSupportedException ("The invoked member is not supported in a dynamic module.");
		}

		private void RejectIfCreated ()
		{
			if (typeb.is_created)
				throw new InvalidOperationException ("Unable to change after type has been created.");
		}

		internal void ResolveUserTypes () {
			type = TypeBuilder.ResolveUserType (type);
			TypeBuilder.ResolveUserTypes (modReq);
			TypeBuilder.ResolveUserTypes (modOpt);
			if (marshal_info != null)
				marshal_info.marshaltyperef = TypeBuilder.ResolveUserType (marshal_info.marshaltyperef);
		}

		internal FieldInfo RuntimeResolve () {
			// typeb.CreateType() populates this.handle
			var type_handle = new RuntimeTypeHandle (typeb.CreateType () as RuntimeType);
			return FieldInfo.GetFieldFromHandle (handle, type_handle);
		}

		public override Module Module {
			get {
				return base.Module;
			}
		}
	}
}

#endif
