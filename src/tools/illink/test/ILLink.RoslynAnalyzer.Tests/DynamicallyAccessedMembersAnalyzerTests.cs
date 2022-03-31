// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class DynamicallyAccessedMembersAnalyzerTests
	{
		static Task VerifyDynamicallyAccessedMembersAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected: expected);
		}

		[Fact]
		public Task NoWarningsIfAnalyzerIsNotEnabled ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}";
			return VerifyCS.VerifyAnalyzerAsync (TargetParameterWithAnnotations);
		}

		#region SourceParameter
		[Fact]
		public Task SourceParameterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}";
			// (23,3): warning IL2067: 'parameter' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
				.WithSpan (23, 3, 23, 38)
				.WithArguments ("parameter",
					"C.NeedsPublicMethodsOnParameter(Type)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M(Type type)
    {
        return type;
    }
}";

			// (19,9): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType)
				.WithSpan (19, 16, 19, 20)
				.WithArguments ("C.M(Type)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M(Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f = typeof(Foo);
}";

			// (18,9): warning IL2069: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField)
				.WithSpan (18, 9, 18, 17)
				.WithArguments ("C.f",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M(Type type)
    {
        type.GetMethod(""Bar"");
	}
}";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (17,9): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
				.WithSpan (17, 9, 17, 30)
				.WithArguments ("System.Type.GetMethod(String)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethodReturnType
		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        NeedsPublicMethodsOnParameter(GetT());
    }

    private static void NeedsPublicMethodsOnParameter(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static Type GetT()
    {
        return typeof(T);
    }
}";

			// (13,9): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (13, 9, 13, 46)
				.WithArguments ("type", "C.NeedsPublicMethodsOnParameter(Type)", "C.GetT()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
    {
        return GetFoo();
    }

    private static Type GetFoo()
    {
        return typeof(Foo);
    }
}";

			// (19,9): warning IL2073: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType)
				.WithSpan (19, 16, 19, 24)
				.WithArguments ("C.M()", "C.GetFoo()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        f = M();
    }

    private static Type M()
    {
        return typeof(Foo);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;
}";

			// (13,9): warning IL2074: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField)
				.WithSpan (13, 9, 13, 16)
				.WithArguments ("C.f",
					"C.M()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethod ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class Foo
{
}

class C
{
    public static void Main()
    {
        GetFoo().GetMethod(""Bar"");

	}

	private static Type GetFoo ()
	{
		return typeof (Foo);
	}
}";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (12,9): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
				.WithSpan (12, 9, 12, 34)
				.WithArguments ("System.Type.GetMethod(String)", "C.GetFoo()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceField
		[Fact]
		public Task SourceFieldDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        NeedsPublicMethods(f);
    }

    private static void NeedsPublicMethods(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
	}
}";

			// (15,9): warning IL2077: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethods(Type)'.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter)
				.WithSpan (15, 9, 15, 30)
				.WithArguments ("type",
					"C.NeedsPublicMethods(Type)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
	{
        return f;
	}
}";

			// (21,9): warning IL2078: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType)
				.WithSpan (21, 16, 21, 17)
				.WithArguments ("C.M()", "C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f1 = typeof(Foo);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f2 = typeof(Foo);

    public static void Main()
    {
        f2 = f1;
    }
}";
			// (18,9): warning IL2079: value stored in field 'C.f2' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'C.f1' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField)
				.WithSpan (18, 9, 18, 16)
				.WithArguments ("C.f2",
					"C.f1",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod(""Bar"");
	}
}";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (14,9): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
				.WithSpan (14, 9, 14, 27)
				.WithArguments ("System.Type.GetMethod(String)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethod

		static string GetSystemTypeBase ()
		{
			return @"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System
{
	public class TestSystemTypeBase : Type
	{
		public override Assembly Assembly => throw new NotImplementedException ();

		public override string AssemblyQualifiedName => throw new NotImplementedException ();

		public override Type BaseType => throw new NotImplementedException ();

		public override string FullName => throw new NotImplementedException ();

		public override Guid GUID => throw new NotImplementedException ();

		public override Module Module => throw new NotImplementedException ();

		public override string Namespace => throw new NotImplementedException ();

		public override Type UnderlyingSystemType => throw new NotImplementedException ();

		public override string Name => throw new NotImplementedException ();

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
			| DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override Type GetElementType ()
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents
		| DynamicallyAccessedMemberTypes.NonPublicEvents)]
		public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
		public override EventInfo[] GetEvents (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields
			| DynamicallyAccessedMemberTypes.NonPublicFields)]
		public override FieldInfo[] GetFields (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		public override Type[] GetInterfaces ()
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)0x1FFF)]
		public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
		public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotImplementedException ();
		}

		public override bool IsDefined (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		protected override TypeAttributes GetAttributeFlagsImpl ()
		{
			throw new NotImplementedException ();
		}

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
		| DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
		protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override bool HasElementTypeImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsArrayImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsByRefImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsCOMObjectImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPointerImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPrimitiveImpl ()
		{
			throw new NotImplementedException ();
		}
	}
}";
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(this);
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}";

			// (200,16): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
			// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetParameterWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
				.WithSpan (200, 13, 200, 21)
				.WithArguments ("type", "System.C.M2(Type)", "System.C.M1()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task ConversionOperation ()
		{
			var ConversionOperation = @"
namespace System
{
    class ConvertsToType
    {
        public static implicit operator Type(ConvertsToType value) => typeof (ConvertsToType);
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), ConversionOperation),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (205, 13, 205, 37)
				.WithArguments ("type", "System.C.M2(Type)", "System.ConvertsToType.implicit operator Type(ConvertsToType)", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}


		[Fact]
		public Task ConversionOperationAnnotationDoesNotMatch ()
		{
			var AnnotatedConversionOperation = @"
namespace System
{
    class ConvertsToType
    {
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public static implicit operator Type(ConvertsToType value) => null;
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), AnnotatedConversionOperation),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (207, 13, 207, 37)
				.WithArguments ("type", "System.C.M2(Type)", "System.ConvertsToType.implicit operator Type(ConvertsToType)", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task ConversionOperationAnnotationMatches ()
		{
			var AnnotatedConversionOperation = @"
namespace System
{
    class ConvertsToType
    {
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public static implicit operator Type(ConvertsToType value) => null;
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), AnnotatedConversionOperation));
		}


		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private Type M()
        {
            return this;
        }
    }
}";

			// (202,13): warning IL2083: 'System.C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodReturnTypeWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType)
				.WithSpan (202, 20, 202, 24)
				.WithArguments ("System.C.M()", "System.C.M()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            f = this;
        }

        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private static Type f;
    }
}";

			// (200,13): warning IL2084: value stored in field 'System.C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetFieldWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField)
				.WithSpan (200, 13, 200, 21)
				.WithArguments ("System.C.f",
					"System.C.M()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            this.GetMethods();
        }
    }
}";

			// (200,13): warning IL2085: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter)
				.WithSpan (200, 13, 200, 30)
				.WithArguments ("System.Type.GetMethods()", "System.C.M()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static void M2<T>()
    {
        M1(typeof(T));
    }
}";

			// (19,9): warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.M1(Type)'.
			// The generic parameter 'T' of 'C.M2<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter)
				.WithSpan (19, 9, 19, 22)
				.WithArguments ("type", "C.M1(Type)", "T", "C.M2<T>()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private static Type M<T>()
    {
        return typeof(T);
    }
}";

			// (15,9): warning IL2088: 'C.M<T>()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' requirements.
			// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType)
				.WithSpan (15, 16, 15, 25)
				.WithArguments ("C.M<T>()", "T", "C.M<T>()", "'DynamicallyAccessedMemberTypes.PublicConstructors'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;

    private static void M<T>()
    {
        f = typeof(T);
    }
}";

			// (17,9): warning IL2089: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField)
				.WithSpan (17, 9, 17, 22)
				.WithArguments ("C.f",
					"T",
					"C.M<T>()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetGenericParameterAnnotations ()
		{
			var TargetGenericParameterWithAnnotations = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    {
    }

    private static void M2<S>()
    {
        M1<S>();
    }
}";

			// (17,9): warning IL2091: 'T' generic argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods'
			// in 'C.M1<T>()'. The generic parameter 'S' of 'C.M2<S>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetGenericParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter)
				.WithSpan (17, 9, 17, 14)
				.WithArguments ("T", "C.M1<T>()", "S", "C.M2<S>()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceTypeofFlowsIntoTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }
}";
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations);
		}

		[Fact]
		public Task SourceTypeofFlowsIntoTargetMethodReturnTypeAnnotation ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
    {
        return typeof(Foo);
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsInfoTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        return type;
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsIntoTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f  = typeof(Foo);
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsIntoTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        type.GetMethod(""Bar"");
	}
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations);
		}
	}
}
