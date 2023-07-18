//
// MethodDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil {

	using Mono.Cecil.Binary;
	using Mono.Cecil.Cil;

	internal sealed class MethodDefinition : MethodReference, IMemberDefinition,
		IHasSecurity, ICustomAttributeProvider {

		public const string Cctor = ".cctor";
		public const string Ctor = ".ctor";

		MethodAttributes m_attributes;
		MethodImplAttributes m_implAttrs;
		MethodSemanticsAttributes m_semAttrs;
		SecurityDeclarationCollection m_secDecls;
		CustomAttributeCollection m_customAttrs;

		MethodBody m_body;
		RVA m_rva;
		OverrideCollection m_overrides;
		PInvokeInfo m_pinvoke;
		readonly ParameterDefinition m_this;

		public MethodAttributes Attributes {
			get { return m_attributes; }
			set { m_attributes = value; }
		}

		public MethodImplAttributes ImplAttributes {
			get { return m_implAttrs; }
			set { m_implAttrs = value; }
		}

		public MethodSemanticsAttributes SemanticsAttributes {
			get { return m_semAttrs; }
			set { m_semAttrs = value; }
		}

		public bool HasSecurityDeclarations {
			get { return (m_secDecls == null) ? false : (m_secDecls.Count > 0); }
		}

		public SecurityDeclarationCollection SecurityDeclarations {
			get {
				if (m_secDecls == null)
					m_secDecls = new SecurityDeclarationCollection (this);

				return m_secDecls;
			}
		}

		public bool HasCustomAttributes {
			get { return (m_customAttrs == null) ? false : (m_customAttrs.Count > 0); }
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public RVA RVA {
			get { return m_rva; }
			set { m_rva = value; }
		}

		public MethodBody Body {
			get {
				LoadBody ();
				return m_body;
			}
			set { m_body = value; }
		}

		public PInvokeInfo PInvokeInfo {
			get { return m_pinvoke; }
			set { m_pinvoke = value; }
		}

		public bool HasOverrides {
			get { return (m_overrides == null) ? false : (m_overrides.Count > 0); }
		}

		public OverrideCollection Overrides {
			get {
				if (m_overrides == null)
					m_overrides = new OverrideCollection (this);

				return m_overrides;
			}
		}

		public ParameterDefinition This {
			get { return m_this; }
		}

		#region MethodAttributes

		public bool IsCompilerControlled {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Compilercontrolled; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.Compilercontrolled;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.Compilercontrolled);
			}
		}

		public bool IsPrivate {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.Private;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.Private);
			}
		}

		public bool IsFamilyAndAssembly {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.FamANDAssem;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.FamANDAssem);
			}
		}

		public bool IsAssembly {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assem; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.Assem;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.Assem);
			}
		}

		public bool IsFamily {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.Family;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.Family);
			}
		}

		public bool IsFamilyOrAssembly {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.FamORAssem;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.FamORAssem);
			}
		}

		public bool IsPublic {
			get { return (m_attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.MemberAccessMask;
					m_attributes |= MethodAttributes.Public;
				} else
					m_attributes &= ~(MethodAttributes.MemberAccessMask & MethodAttributes.Public);
			}
		}

		public bool IsStatic {
			get { return (m_attributes & MethodAttributes.Static) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.Static;
				else
					m_attributes &= ~MethodAttributes.Static;
			}
		}

		public bool IsFinal {
			get { return (m_attributes & MethodAttributes.Final) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.Final;
				else
					m_attributes &= ~MethodAttributes.Final;
			}
		}

		public bool IsVirtual {
			get { return (m_attributes & MethodAttributes.Virtual) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.Virtual;
				else
					m_attributes &= ~MethodAttributes.Virtual;
			}
		}

		public bool IsHideBySig {
			get { return (m_attributes & MethodAttributes.HideBySig) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.HideBySig;
				else
					m_attributes &= ~MethodAttributes.HideBySig;
			}
		}

		public bool IsReuseSlot {
			get { return (m_attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.VtableLayoutMask;
					m_attributes |= MethodAttributes.ReuseSlot;
				} else
					m_attributes &= ~(MethodAttributes.VtableLayoutMask & MethodAttributes.ReuseSlot);
			}
		}

		public bool IsNewSlot {
			get { return (m_attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot; }
			set {
				if (value) {
					m_attributes &= ~MethodAttributes.VtableLayoutMask;
					m_attributes |= MethodAttributes.NewSlot;
				} else
					m_attributes &= ~(MethodAttributes.VtableLayoutMask & MethodAttributes.NewSlot);
			}
		}

		public bool IsStrict {
			get { return (m_attributes & MethodAttributes.Strict) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.Strict;
				else
					m_attributes &= ~MethodAttributes.Strict;
			}
		}

		public bool IsAbstract {
			get { return (m_attributes & MethodAttributes.Abstract) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.Abstract;
				else
					m_attributes &= ~MethodAttributes.Abstract;
			}
		}

		public bool IsSpecialName {
			get { return (m_attributes & MethodAttributes.SpecialName) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.SpecialName;
				else
					m_attributes &= ~MethodAttributes.SpecialName;
			}
		}

		public bool IsPInvokeImpl {
			get { return (m_attributes & MethodAttributes.PInvokeImpl) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.PInvokeImpl;
				else
					m_attributes &= ~MethodAttributes.PInvokeImpl;
			}
		}

		public bool IsUnmanagedExport {
			get { return (m_attributes & MethodAttributes.UnmanagedExport) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.UnmanagedExport;
				else
					m_attributes &= ~MethodAttributes.UnmanagedExport;
			}
		}

		public bool IsRuntimeSpecialName {
			get { return (m_attributes & MethodAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.RTSpecialName;
				else
					m_attributes &= ~MethodAttributes.RTSpecialName;
			}
		}

		public bool HasSecurity {
			get { return (m_attributes & MethodAttributes.HasSecurity) != 0; }
			set {
				if (value)
					m_attributes |= MethodAttributes.HasSecurity;
				else
					m_attributes &= ~MethodAttributes.HasSecurity;
			}
		}

		#endregion

		#region MethodImplAttributes

		public bool IsIL {
			get { return (m_implAttrs & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL; }
			set {
				if (value) {
					m_implAttrs &= ~MethodImplAttributes.CodeTypeMask;
					m_implAttrs |= MethodImplAttributes.IL;
				} else
					m_implAttrs &= ~(MethodImplAttributes.CodeTypeMask & MethodImplAttributes.IL);
			}
		}

		public bool IsNative {
			get { return (m_implAttrs & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native; }
			set {
				if (value) {
					m_implAttrs &= ~MethodImplAttributes.CodeTypeMask;
					m_implAttrs |= MethodImplAttributes.Native;
				} else
					m_implAttrs &= ~(MethodImplAttributes.CodeTypeMask & MethodImplAttributes.Native);
			}
		}

		public bool IsRuntime {
			get { return (m_implAttrs & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime; }
			set {
				if (value) {
					m_implAttrs &= ~MethodImplAttributes.CodeTypeMask;
					m_implAttrs |= MethodImplAttributes.Runtime;
				} else
					m_implAttrs &= ~(MethodImplAttributes.CodeTypeMask & MethodImplAttributes.Runtime);
			}
		}

		public bool IsUnmanaged {
			get { return (m_implAttrs & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Unmanaged; }
			set {
				if (value) {
					m_implAttrs &= ~MethodImplAttributes.ManagedMask;
					m_implAttrs |= MethodImplAttributes.Unmanaged;
				} else
					m_implAttrs &= ~(MethodImplAttributes.ManagedMask & MethodImplAttributes.Unmanaged);
			}
		}

		public bool IsManaged {
			get { return (m_implAttrs & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed; }
			set {
				if (value) {
					m_implAttrs &= ~MethodImplAttributes.ManagedMask;
					m_implAttrs |= MethodImplAttributes.Managed;
				} else
					m_implAttrs &= ~(MethodImplAttributes.ManagedMask & MethodImplAttributes.Managed);
			}
		}

		public bool IsForwardRef {
			get { return (m_implAttrs & MethodImplAttributes.ForwardRef) != 0; }
			set {
				if (value)
					m_implAttrs |= MethodImplAttributes.ForwardRef;
				else
					m_implAttrs &= ~MethodImplAttributes.ForwardRef;
			}
		}

		public bool IsPreserveSig {
			get { return (m_implAttrs & MethodImplAttributes.PreserveSig) != 0; }
			set {
				if (value)
					m_implAttrs |= MethodImplAttributes.PreserveSig;
				else
					m_implAttrs &= ~MethodImplAttributes.PreserveSig;
			}
		}

		public bool IsInternalCall {
			get { return (m_implAttrs & MethodImplAttributes.InternalCall) != 0; }
			set {
				if (value)
					m_implAttrs |= MethodImplAttributes.InternalCall;
				else
					m_implAttrs &= ~MethodImplAttributes.InternalCall;
			}
		}

		public bool IsSynchronized {
			get { return (m_implAttrs & MethodImplAttributes.Synchronized) != 0; }
			set {
				if (value)
					m_implAttrs |= MethodImplAttributes.Synchronized;
				else
					m_implAttrs &= ~MethodImplAttributes.Synchronized;
			}
		}

		public bool NoInlining {
			get { return (m_implAttrs & MethodImplAttributes.NoInlining) != 0; }
			set {
				if (value)
					m_implAttrs |= MethodImplAttributes.NoInlining;
				else
					m_implAttrs &= ~MethodImplAttributes.NoInlining;
			}
		}

		#endregion

		#region MethodSemanticsAttributes
		public bool IsSetter {
			get { return (m_semAttrs & MethodSemanticsAttributes.Setter) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.Setter;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.Setter;
			}
		}

		public bool IsGetter {
			get { return (m_semAttrs & MethodSemanticsAttributes.Getter) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.Getter;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.Getter;
			}
		}

		public bool IsOther {
			get { return (m_semAttrs & MethodSemanticsAttributes.Other) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.Other;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.Other;
			}
		}

		public bool IsAddOn {
			get { return (m_semAttrs & MethodSemanticsAttributes.AddOn) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.AddOn;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.AddOn;
			}
		}

		public bool IsRemoveOn {
			get { return (m_semAttrs & MethodSemanticsAttributes.RemoveOn) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.RemoveOn;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.RemoveOn;
			}
		}

		public bool IsFire {
			get { return (m_semAttrs & MethodSemanticsAttributes.Fire) != 0; }
			set {
				if (value)
					m_semAttrs |= MethodSemanticsAttributes.Fire;
				else
					m_semAttrs &= ~MethodSemanticsAttributes.Fire;
			}
		}

		#endregion

		public bool IsConstructor {
			get {
				return this.IsRuntimeSpecialName && this.IsSpecialName &&
					(this.Name == Cctor || this.Name == Ctor);
			}
		}

		public bool HasBody {
			get {
				return (m_attributes & MethodAttributes.Abstract) == 0 &&
					(m_attributes & MethodAttributes.PInvokeImpl) == 0 &&
					(m_implAttrs & MethodImplAttributes.InternalCall) == 0 &&
					(m_implAttrs & MethodImplAttributes.Native) == 0 &&
					(m_implAttrs & MethodImplAttributes.Unmanaged) == 0 &&
					(m_implAttrs & MethodImplAttributes.Runtime) == 0;
			}
		}

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public MethodDefinition (string name, RVA rva,
			MethodAttributes attrs, MethodImplAttributes implAttrs,
			bool hasThis, bool explicitThis, MethodCallingConvention callConv) :
			base (name, hasThis, explicitThis, callConv)
		{
			m_rva = rva;
			m_attributes = attrs;
			m_implAttrs = implAttrs;

			if (!IsStatic)
				m_this = new ParameterDefinition ("this", 0, (ParameterAttributes) 0, null);
		}

		internal MethodDefinition (string name, MethodAttributes attrs) : base (name)
		{
			m_attributes = attrs;

			this.HasThis = !this.IsStatic;
			if (!IsStatic)
				m_this = new ParameterDefinition ("this", 0, (ParameterAttributes) 0, null);
		}

		public MethodDefinition (string name, MethodAttributes attrs, TypeReference returnType) :
			this (name, attrs)
		{
			this.ReturnType.ReturnType = returnType;
		}

		internal void LoadBody ()
		{
			if (m_body == null && this.HasBody) {
				m_body = new MethodBody (this);

				ModuleDefinition module = DeclaringType != null ? DeclaringType.Module : null;

				if (module != null && m_rva != RVA.Zero)
					module.Controller.Reader.Code.VisitMethodBody (m_body);
			}
		}

		public override MethodDefinition Resolve ()
		{
			return this;
		}

		public MethodDefinition Clone ()
		{
			return Clone (this, new ImportContext (NullReferenceImporter.Instance, this));
		}

		internal static MethodDefinition Clone (MethodDefinition meth, ImportContext context)
		{
			MethodDefinition nm = new MethodDefinition (
				meth.Name,
				RVA.Zero,
				meth.Attributes,
				meth.ImplAttributes,
				meth.HasThis,
				meth.ExplicitThis,
				meth.CallingConvention);

			MethodReference contextMethod = context.GenericContext.Method;

			context.GenericContext.Method = nm;

			GenericParameter.CloneInto (meth, nm, context);

			nm.ReturnType.ReturnType = context.Import (meth.ReturnType.ReturnType);

			if (meth.ReturnType.Parameter != null) {
				nm.ReturnType.Parameter = ParameterDefinition.Clone (meth.ReturnType.Parameter, context);
				nm.ReturnType.Parameter.Method = nm;
			}

			if (meth.PInvokeInfo != null)
				nm.PInvokeInfo = meth.PInvokeInfo; // TODO: import module ?

			if (meth.HasParameters) {
				foreach (ParameterDefinition param in meth.Parameters)
					nm.Parameters.Add (ParameterDefinition.Clone (param, context));
			}
			if (meth.HasOverrides) {
				foreach (MethodReference ov in meth.Overrides)
					nm.Overrides.Add (context.Import (ov));
			}
			if (meth.HasCustomAttributes) {
				foreach (CustomAttribute ca in meth.CustomAttributes)
					nm.CustomAttributes.Add (CustomAttribute.Clone (ca, context));
			}
			if (meth.HasSecurityDeclarations) {
				foreach (SecurityDeclaration sec in meth.SecurityDeclarations)
					nm.SecurityDeclarations.Add (SecurityDeclaration.Clone (sec));
			}

			if (meth.Body != null)
				nm.Body = MethodBody.Clone (meth.Body, nm, context);

			context.GenericContext.Method = contextMethod;

			return nm;
		}

		public override void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitMethodDefinition (this);

			this.GenericParameters.Accept (visitor);
			this.Parameters.Accept (visitor);

			if (this.PInvokeInfo != null)
				this.PInvokeInfo.Accept (visitor);

			this.SecurityDeclarations.Accept (visitor);
			this.Overrides.Accept (visitor);
			this.CustomAttributes.Accept (visitor);
		}
	}
}
