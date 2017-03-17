// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.errorverifier.errorverifier
{
    using System.Reflection;
    using System.Resources;

    public enum ErrorElementId
    {
        None,
        SK_METHOD, // method
        SK_CLASS, // type
        SK_NAMESPACE, // namespace
        SK_FIELD, // field
        SK_PROPERTY, // property
        SK_UNKNOWN, // element
        SK_VARIABLE, // variable
        SK_EVENT, // event
        SK_TYVAR, // type parameter
        SK_ALIAS, // using alias
        ERRORSYM, // <error>
        NULL, // <null>
        GlobalNamespace, // <global namespace>
        MethodGroup, // method group
        AnonMethod, // anonymous method
        Lambda, // lambda expression
        AnonymousType, // anonymous type
    }

    public enum ErrorMessageId
    {
        None,
        BadBinaryOps, // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
        IntDivByZero, // Division by constant zero
        BadIndexLHS, // Cannot apply indexing with [] to an expression of type '{0}'
        BadIndexCount, // Wrong number of indices inside []; expected '{0}'
        BadUnaryOp, // Operator '{0}' cannot be applied to operand of type '{1}'
        NoImplicitConv, // Cannot implicitly convert type '{0}' to '{1}'
        NoExplicitConv, // Cannot convert type '{0}' to '{1}'
        ConstOutOfRange, // Constant value '{0}' cannot be converted to a '{1}'
        AmbigBinaryOps, // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
        AmbigUnaryOp, // Operator '{0}' is ambiguous on an operand of type '{1}'
        ValueCantBeNull, // Cannot convert null to '{0}' because it is a non-nullable value type
        WrongNestedThis, // Cannot access a non-static member of outer type '{0}' via nested type '{1}'
        NoSuchMember, // '{0}' does not contain a definition for '{1}'
        ObjectRequired, // An object reference is required for the non-static field, method, or property '{0}'
        AmbigCall, // The call is ambiguous between the following methods or properties: '{0}' and '{1}'
        BadAccess, // '{0}' is inaccessible due to its protection level
        MethDelegateMismatch, // No overload for '{0}' matches delegate '{1}'
        AssgLvalueExpected, // The left-hand side of an assignment must be a variable, property or indexer
        NoConstructors, // The type '{0}' has no constructors defined
        BadDelegateConstructor, // The delegate '{0}' does not have a valid constructor
        PropertyLacksGet, // The property or indexer '{0}' cannot be used in this context because it lacks the get accessor
        ObjectProhibited, // Member '{0}' cannot be accessed with an instance reference; qualify it with a type name instead
        AssgReadonly, // A readonly field cannot be assigned to (except in a constructor or a variable initializer)
        RefReadonly, // A readonly field cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic, // A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic, // A static readonly field cannot be passed ref or out (except in a static constructor)
        AssgReadonlyProp, // Property or indexer '{0}' cannot be assigned to -- it is read only
        AbstractBaseCall, // Cannot call an abstract base member: '{0}'
        RefProperty, // A property or indexer may not be passed as an out or ref parameter
        ManagedAddr, // Cannot take the address of, get the size of, or declare a pointer to a managed type ('{0}')
        FixedNotNeeded, // You cannot use the fixed statement to take the address of an already fixed expression
        UnsafeNeeded, // Dynamic calls cannot be used in conjunction with pointers
        BadBoolOp, // In order to be applicable as a short circuit operator a user-defined logical operator ('{0}') must have the same return type as the type of its 2 parameters
        MustHaveOpTF, // The type ('{0}') must contain declarations of operator true and operator false
        CheckedOverflow, // The operation overflows at compile time in checked mode
        ConstOutOfRangeChecked, // Constant value '{0}' cannot be converted to a '{1}' (use 'unchecked' syntax to override)
        AmbigMember, // Ambiguity between '{0}' and '{1}'
        SizeofUnsafe, // '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
        FieldInitRefNonstatic, // A field initializer cannot reference the non-static field, method, or property '{0}'
        CallingFinalizeDepracated, // Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
        CallingBaseFinalizeDeprecated, // Do not directly call your base class Finalize method. It is called automatically from your destructor.
        BadCastInFixed, // The right hand side of a fixed statement assignment may not be a cast expression
        NoImplicitConvCast, // Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)
        InaccessibleGetter, // The property or indexer '{0}' cannot be used in this context because the get accessor is inaccessible
        InaccessibleSetter, // The property or indexer '{0}' cannot be used in this context because the set accessor is inaccessible
        BadArity, // Using the generic {1} '{0}' requires '{2}' type arguments
        BadTypeArgument, // The type '{0}' may not be used as a type argument
        TypeArgsNotAllowed, // The {1} '{0}' cannot be used with type arguments
        HasNoTypeVars, // The non-generic {1} '{0}' cannot be used with type arguments
        NewConstraintNotSatisfied, // '{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'
        GenericConstraintNotSatisfiedRefType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedNullableEnum, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'.
        GenericConstraintNotSatisfiedNullableInterface, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'. Nullable types can not satisfy any interface constraints.
        GenericConstraintNotSatisfiedTyVar, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion or type parameter conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedValType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion from '{3}' to '{1}'.
        TypeVarCantBeNull, // Cannot convert null to type parameter '{0}' because it could be a non-nullable value type. Consider using 'default({0})' instead.
        BadRetType, // '{1} {0}' has the wrong return type
        CantInferMethTypeArgs, // The type arguments for method '{0}' cannot be inferred from the usage. Try specifying the type arguments explicitly.
        MethGrpToNonDel, // Cannot convert method group '{0}' to non-delegate type '{1}'. Did you intend to invoke the method?
        RefConstraintNotSatisfied, // The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'
        ValConstraintNotSatisfied, // The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'
        CircularConstraint, // Circular constraint dependency involving '{0}' and '{1}'
        BaseConstraintConflict, // Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'
        ConWithValCon, // Type parameter '{1}' has the 'struct' constraint so '{1}' cannot be used as a constraint for '{0}'
        AmbigUDConv, // Ambiguous user defined conversions '{0}' and '{1}' when converting from '{2}' to '{3}'
        PredefinedTypeNotFound, // Predefined type '{0}' is not defined or imported
        PredefinedTypeBadType, // Predefined type '{0}' is declared incorrectly
        BindToBogus, // '{0}' is not supported by the language
        CantCallSpecialMethod, // '{0}': cannot explicitly call operator or accessor
        BogusType, // '{0}' is a type not supported by the language
        MissingPredefinedMember, // Missing compiler required member '{0}.{1}'
        LiteralDoubleCast, // Literal of type double cannot be implicitly converted to type '{1}'; use an '{0}' suffix to create a literal of this type
        UnifyingInterfaceInstantiations, // '{0}' cannot implement both '{1}' and '{2}' because they may unify for some type parameter substitutions
        ConvertToStaticClass, // Cannot convert to static type '{0}'
        GenericArgIsStaticClass, // '{0}': static types cannot be used as type arguments
        PartialMethodToDelegate, // Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
        IncrementLvalueExpected, // The operand of an increment or decrement operator must be a variable, property or indexer
        NoSuchMemberOrExtension, // '{0}' does not contain a definition for '{1}' and no extension method '{1}' accepting a first argument of type '{0}' could be found (are you missing a using directive or an assembly reference?)
        ValueTypeExtDelegate, // Extension methods '{0}' defined on value type '{1}' cannot be used to create delegates
        BadArgCount, // No overload for method '{0}' takes '{1}' arguments
        BadArgTypes, // The best overloaded method match for '{0}' has some invalid arguments
        BadArgType, // Argument '{0}': cannot convert from '{1}' to '{2}'
        RefLvalueExpected, // A ref or out argument must be an assignable variable
        BadProtectedAccess, // Cannot access protected member '{0}' via a qualifier of type '{1}'; the qualifier must be of type '{2}' (or derived from it)
        BindToBogusProp2, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor methods '{1}' or '{2}'
        BindToBogusProp1, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor method '{1}'
        BadDelArgCount, // Delegate '{0}' does not take '{1}' arguments
        BadDelArgTypes, // Delegate '{0}' has some invalid arguments
        AssgReadonlyLocal, // Cannot assign to '{0}' because it is read-only
        RefReadonlyLocal, // Cannot pass '{0}' as a ref or out argument because it is read-only
        ReturnNotLValue, // Cannot modify the return value of '{0}' because it is not a variable
        BadArgExtraRef, // Argument '{0}' should not be passed with the '{1}' keyword
        // DelegateOnConditional, // Cannot create delegate with '{0}' because it has a Conditional attribute (REMOVED)
        BadArgRef, // Argument '{0}' must be passed with the '{1}' keyword
        AssgReadonly2, // Members of readonly field '{0}' cannot be modified (except in a constructor or a variable initializer)
        RefReadonly2, // Members of readonly field '{0}' cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic2, // Fields of static readonly field '{0}' cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic2, // Fields of static readonly field '{0}' cannot be passed ref or out (except in a static constructor)
        AssgReadonlyLocalCause, // Cannot assign to '{0}' because it is a '{1}'
        RefReadonlyLocalCause, // Cannot pass '{0}' as a ref or out argument because it is a '{1}'
        ThisStructNotInAnonMeth, // Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
        DelegateOnNullable, // Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
        BadCtorArgCount, // '{0}' does not contain a constructor that takes '{1}' arguments
        BadExtensionArgTypes, // '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' has some invalid arguments
        BadInstanceArgType, // Instance argument: cannot convert from '{0}' to '{1}'
        BadArgTypesForCollectionAdd, // The best overloaded Add method '{0}' for the collection initializer has some invalid arguments
        InitializerAddHasParamModifiers, // The best overloaded method match '{0}' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
        NonInvocableMemberCalled, // Non-invocable member '{0}' cannot be used like a method.
        NamedArgumentSpecificationBeforeFixedArgument, // Named argument specifications must appear after all fixed arguments have been specified
        BadNamedArgument, // The best overload for '{0}' does not have a parameter named '{1}'
        BadNamedArgumentForDelegateInvoke, // The delegate '{0}' does not have a parameter named '{1}'
        DuplicateNamedArgument, // Named argument '{0}' cannot be specified multiple times
        NamedArgumentUsedInPositional, // Named argument '{0}' specifies a parameter for which a positional argument has already been given
    }

    public enum RuntimeErrorId
    {
        None,
        // RuntimeBinderInternalCompilerException
        InternalCompilerError, // An unexpected exception occurred while binding a dynamic operation
        // ArgumentException
        BindRequireArguments, // Cannot bind call with no calling object
        // RuntimeBinderException
        BindCallFailedOverloadResolution, // Overload resolution failed
        // ArgumentException
        BindBinaryOperatorRequireTwoArguments, // Binary operators must be invoked with two arguments
        // ArgumentException
        BindUnaryOperatorRequireOneArgument, // Unary operators must be invoked with one argument
        // RuntimeBinderException
        BindPropertyFailedMethodGroup, // The name '{0}' is bound to a method and cannot be used like a property
        // RuntimeBinderException
        BindPropertyFailedEvent, // The event '{0}' can only appear on the left hand side of += or -=
        // RuntimeBinderException
        BindInvokeFailedNonDelegate, // Cannot invoke a non-delegate type
        // ArgumentException
        BindImplicitConversionRequireOneArgument, // Implicit conversion takes exactly one argument
        // ArgumentException
        BindExplicitConversionRequireOneArgument, // Explicit conversion takes exactly one argument
        // ArgumentException
        BindBinaryAssignmentRequireTwoArguments, // Binary operators cannot be invoked with one argument
        // RuntimeBinderException
        BindBinaryAssignmentFailedNullReference, // Cannot perform member assignment on a null reference
        // RuntimeBinderException
        NullReferenceOnMemberException, // Cannot perform runtime binding on a null reference
        // RuntimeBinderException
        BindCallToConditionalMethod, // Cannot dynamically invoke method '{0}' because it has a Conditional attribute
        // RuntimeBinderException
        BindToVoidMethodButExpectResult, // Cannot implicitly convert type 'void' to 'object'
        // EE?
        EmptyDynamicView, // No further information on this object could be discovered
        // MissingMemberException
        GetValueonWriteOnlyProperty, // Write Only properties are not supported
    }

    public class ErrorVerifier
    {
        private static Assembly s_asm;
        private static ResourceManager s_rm1;
        private static ResourceManager s_rm2;
        public static string GetErrorElement(ErrorElementId id)
        {
            return string.Empty;
        }

        public static bool Verify(ErrorMessageId id, string actualError, params string[] args)
        {
            return true;
        }

        public static bool Verify(RuntimeErrorId id, string actualError, params string[] args)
        {
            return true;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.derived001.derived001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.errorverifier.errorverifier;
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass<T, U>
        where T : List<object>, new()
    {
        internal T myList = new T();
    }

    public class MyClassDerived<U> : MyClass<List<dynamic>, U>
    {
        public void Foo()
        {
            if (myList.Count != 0)
                Test.Status = 2;
            myList.Add(1);
            try
            {
                myList[0].Foo(); //This should compile into a call site
                Test.Status = 2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                Test.Status = 2;
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClassDerived<object> mc = new MyClassDerived<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.derived003.derived003
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class MyClass<T, U>
        where T : U
    {
    }

    public class MyClassDerived<T> : MyClass<T, object>
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClassDerived<dynamic> mc = new MyClassDerived<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.derived004.derived004
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass<T, U>
        where T : U
    {
    }

    public class MyClassDerived<T> : MyClass<T, dynamic>
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClassDerived<dynamic> mc = new MyClassDerived<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.derived005.derived005
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass<T, U>
        where T : U
    {
    }

    public class MyClassDerived<T> : MyClass<T, dynamic>
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClassDerived<object> mc = new MyClassDerived<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.b813045bindfield01.b813045bindfield01
{
    // <Title>Generic constraints</Title>
    // <Description>Event invocation throws an ArgumentNullException incorrectly
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    using System;

    public interface I<T>
    {
        T Prop
        {
            get;
            set;
        }

        void M(T t, out char ret);
    }

    public class DC<T> : I<T>
    {
        private T _tt = default(T);
        public T Prop
        {
            get
            {
                return _tt;
            }

            set
            {
                _tt = value;
            }
        }

        public void M(T t, out char ret)
        {
            _tt = t;
            ret = 'i';
        }
    }

    public struct DS<T> : I<T>
    {
        public T Prop
        {
            get
            {
                return default(T);
            }

            set
            {
            }
        }

        public void M(T t, out char ret)
        {
            T tt = t;
            ret = 'y';
        }
    }

    /// <summary>
    /// NO public, no instance field in generic type
    /// </summary>
    /// <typeparam name = "T"></typeparam>
    public class C<T>
    {
        public event EventHandler E = delegate
        {
        }

        ;
        public static void Foo()
        {
            dynamic c = new C<T>();
            c.E(null, null);
        }

        // field
        private T _tt;
        public char Bar(T t1, T t2)
        {
            _tt = t1;
            return 'q'; // t1 == t2;
        }
    }

    public struct S<T, V>
    {
        public event EventHandler E;
        public S(EventHandler e)
        {
            E = e;
        }

        public static void Foo()
        {
            dynamic d = new S<T, V>(delegate
            {
            }

            );
            d.E(null, null);
        }

        // local var
        public char Bar(T t, V v)
        {
            T t1 = t;
            return 'c';
        }
    }

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C<int>.Foo();
            S<string, char>.Foo();
            dynamic d1 = new C<Test>();
            bool ret = 'q' == d1.Bar(null, null);
            dynamic d2 = new S<sbyte, dynamic>(new EventHandler((p, q) =>
            {
            }

            ));
            ret &= 'c' == d2.Bar(-1, null);
            dynamic v = new DC<object>();
            ret &= default(object) == v.Prop;
            char c = ' ';
            v.M(null, out c);
            ret &= 'i' == c;
            d2 = new DS<dynamic>();
            ret &= default(dynamic) == d2.Prop;
            d2.M(new object(), out c);
            ret &= 'y' == c;
            System.Console.WriteLine(ret);
            return ret ? 0 : 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.changetypearg001.changetypearg001
{
    // <Title>Specializing generic overloads </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var TestValue2 = new GC1<string>();
            int rez = 0;
            dynamic d = new SubGenericClass<string>();
            dynamic d1 = default(GC1<GC1<string>>);
            d[d1, TestValue2] = d1;
            if (d[d1, TestValue2] != null)
                rez++;
            if (d.Method1(i: d1, t: TestValue2) != null)
                rez++;
            var TestValue = new GC1<GC1<string>>();
            dynamic dt = new VeryDerived<string>();
            dynamic d2 = default(GC1<GC1<GC1<string>>>);
            dt[d2, TestValue] = d2;
            if (dt[d2, TestValue] != null)
                rez++;
            if (dt.Method1(i: d2, t: TestValue) != null)
                rez++;
            return rez;
        }
    }

    public class GC1<T>
    {
    }

    public abstract class GenericClass<T>
    {
        public abstract GC1<T> this[GC1<T> i, T t]
        {
            get;
            set;
        }

        public abstract GC1<T> Method1(GC1<T> i, T t);
    }

    public class SubGenericClass<T> : GenericClass<GC1<T>>
    {
        public override GC1<GC1<T>> this[GC1<GC1<T>> i, GC1<T> t]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public override GC1<GC1<T>> Method1(GC1<GC1<T>> i, GC1<T> t)
        {
            return null;
        }
    }

    public class VeryDerived<T> : SubGenericClass<GC1<T>>
    {
        public override GC1<GC1<GC1<T>>> Method1(GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t)
        {
            return base.Method1(i, t);
        }

        public override GC1<GC1<GC1<T>>> this[GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t]
        {
            get
            {
                return base[i, t];
            }

            set
            {
                base[i, t] = value;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.changetypearg002.changetypearg002
{
    // <Title>Specializing generic overloads </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var TestValue2 = new GC1<string>();
            int rez = 0;
            dynamic d = new SubGenericClass<string>();
            dynamic d1 = default(GC1<GC1<string>>);
            d[d1, TestValue2] = d1;
            if (d[d1, TestValue2] != null)
                rez++;
            if (d.Method1(i: d1, t: TestValue2) != null)
                rez++;
            var TestValue = new GC1<GC1<string>>();
            dynamic dt = new VeryDerived<string>();
            dynamic d2 = default(GC1<GC1<GC1<string>>>);
            dt[d2, TestValue] = d2; // System.InvalidProgramException
            if (dt[d2, TestValue] != null)
                rez++;
            if (dt.Method1(i: d2, t: TestValue) != null)
                rez++;
            return rez;
        }
    }

    public class GC1<T>
    {
    }

    public class GenericClass<T>
    {
        public virtual GC1<T> this[GC1<T> i, T t]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public virtual GC1<T> Method1(GC1<T> i, T t)
        {
            return null;
        }
    }

    public class SubGenericClass<T> : GenericClass<GC1<T>>
    {
        public override GC1<GC1<T>> this[GC1<GC1<T>> i, GC1<T> t]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public override GC1<GC1<T>> Method1(GC1<GC1<T>> i, GC1<T> t)
        {
            return null;
        }
    }

    public class VeryDerived<T> : SubGenericClass<GC1<T>>
    {
        public override GC1<GC1<GC1<T>>> Method1(GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t)
        {
            return base.Method1(i, t);
        }

        public override GC1<GC1<GC1<T>>> this[GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t]
        {
            get
            {
                return base[i, t];
            }

            set
            {
                base[i, t] = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.changetypearg003.changetypearg003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.errorverifier.errorverifier;
    // <Title>Specializing generic overloads </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public class Program
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var TestValue2 = new GC1<string>();
            dynamic d = new SubGenericClass<string>();
            dynamic d1 = default(GC1<GC1<string>>);
            d[d1, TestValue2] = d1;
            if (d[d1, TestValue2] != null)
                rez++;
            try
            {
                if (d.Method1(i: d1, t: TestValue2) != null)
                    rez++;
            }
            catch (RuntimeBinderException exc)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, exc.Message, "GenericClass<GC1<string>>.Method1(ref GC1<GC1<string>>, GC1<string>)") == false)
                    rez++;
            }

            var TestValue = new GC1<GC1<string>>();
            dynamic dt = new VeryDerived<string>();
            dynamic d2 = default(GC1<GC1<GC1<string>>>);
            dt[d2, TestValue] = d2;
            if (dt[d2, TestValue] != null)
                rez++;
            try
            {
                if (dt.Method1(i: ref d2, t: TestValue) != null)
                    rez++;
            }
            catch (RuntimeBinderException exc)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, exc.Message, "GenericClass<GC1<GC1<string>>>.Method1(ref GC1<GC1<GC1<string>>>, GC1<GC1<string>>)") == false)
                    rez++;
            }

            return rez;
        }
    }

    public class GC1<T>
    {
    }

    public class GenericClass<T>
    {
        public virtual GC1<T> this[GC1<T> i, T t]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public virtual GC1<T> Method1(ref GC1<T> i, T t)
        {
            return null;
        }
    }

    public class SubGenericClass<T> : GenericClass<GC1<T>>
    {
        public override GC1<GC1<T>> this[GC1<GC1<T>> i, GC1<T> t]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public override GC1<GC1<T>> Method1(ref GC1<GC1<T>> i, GC1<T> t)
        {
            return null;
        }
    }

    public class VeryDerived<T> : SubGenericClass<GC1<T>>
    {
        public override GC1<GC1<GC1<T>>> Method1(ref GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t)
        {
            return null;
        }

        public override GC1<GC1<GC1<T>>> this[GC1<GC1<GC1<T>>> i, GC1<GC1<T>> t]
        {
            get
            {
                return base[i, t];
            }

            set
            {
                base[i, t] = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr001.ovr001
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    public class C<T>
    {
        public virtual int M(T x)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T> ev;
        public virtual void Raise(T t)
        {
            ev(t);
        }
    }

    public class D : C<int>
    {
        public override int M(int y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override int this[int x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override event Foo<int> ev;
        public override void Raise(int t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr002.ovr002
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    public class A
    {
        public virtual int M(int t)
        {
            return 2;
        }

        public virtual int this[int x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }
    }

    public class C<T> : A
    {
        public virtual int M(T x)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T> ev;
        public virtual void Raise(T t)
        {
            ev(t);
        }
    }

    public class D : C<int>
    {
        public override int M(int y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override int this[int x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override event Foo<int> ev;
        public override void Raise(int t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr003.ovr003
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T, U>(T t, U u);
    public class C<T, U>
    {
        public virtual int M(T t, U u)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x, U u]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T, U> ev;
        public virtual void Raise(T t, U u)
        {
            ev(t, u);
        }
    }

    public class D<T> : C<T, int>
    {
        public override int M(T t, int y)
        {
            return 1;
        }

        public override int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public override int this[T t, int x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public override event Foo<T, int> ev;
        public override void Raise(T t, int u)
        {
            ev(t, u);
        }
    }

    public class E : D<string>
    {
        public override event Foo<string, int> ev;
        public override int M(string t, int y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
                base.P = value;
            }
        }

        public override int this[string t, int x]
        {
            get
            {
                return 0;
            }

            set
            {
                base[t, x] = value;
            }
        }

        public override void Raise(string t, int u)
        {
            ev(t, u);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D<int>();
            rez += x.M((dynamic)0, (dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3, (dynamic)3];
            rez += x.M(t: (dynamic)0, y: (dynamic)0);
            rez += x[t: (dynamic)3, x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3, (dynamic)3);
            //at this point rez should be 5
            rez -= 5;
            var y = new E();
            rez += y.M("", (dynamic)0);
            rez += ((dynamic)y).P;
            rez += y["a", (dynamic)3];
            rez += y.M(t: "adfsa", y: (dynamic)0);
            rez += y[t: "", x: (dynamic)3];
            y.ev += Foo;
            y.Raise("safs", (dynamic)3);
            dynamic t = new E();
            rez += t.M("", (dynamic)0);
            rez += ((dynamic)t).P;
            rez += t["a", (dynamic)3];
            rez += t.M(t: "adfsa", y: (dynamic)0);
            rez += t[t: "", x: (dynamic)3];
            t.ev += (Foo<string, int>)Foo;
            t.Raise("safs", (dynamic)3);
            return rez;
        }

        private static int Foo(int x, int y)
        {
            return 0;
        }

        private static int Foo(string x, int y)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr004.ovr004
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T, U>(T t, U u);
    public class A<T>
    {
    }

    public class C<T, U>
    {
        public virtual int M(T t, U u)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x, U u]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T, U> ev;
        public virtual void Raise(T t, U u)
        {
            ev(t, u);
        }
    }

    public class D<T> : C<T, A<A<long>>>
    {
        public override int M(T t, A<A<long>> y)
        {
            return 1;
        }

        public override int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public override int this[T t, A<A<long>> x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public override event Foo<T, A<A<long>>> ev;
        public override void Raise(T t, A<A<long>> u)
        {
            ev(t, u);
        }
    }

    public class E : D<A<A<string>>>
    {
        public override event Foo<A<A<string>>, A<A<long>>> ev;
        public override int M(A<A<string>> t, A<A<long>> y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
                base.P = value;
            }
        }

        public override int this[A<A<string>> t, A<A<long>> x]
        {
            get
            {
                return 0;
            }

            set
            {
                base[t, x] = value;
            }
        }

        public override void Raise(A<A<string>> t, A<A<long>> u)
        {
            ev(t, u);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D<A<A<string>>>();
            rez += x.M((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            rez += ((dynamic)x).P;
            rez += x[(dynamic)new A<A<string>>(), (dynamic)new A<A<long>>()];
            rez += x.M(t: (dynamic)new A<A<string>>(), y: (dynamic)new A<A<long>>());
            rez += x[t: (dynamic)new A<A<string>>(), x: (dynamic)new A<A<long>>()];
            x.ev += Foo;
            x.Raise((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            //at this point rez should be 5
            rez -= 5;
            var y = new E();
            rez += y.M((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            rez += ((dynamic)y).P;
            rez += y[(dynamic)new A<A<string>>(), (dynamic)new A<A<long>>()];
            rez += y.M(t: (dynamic)new A<A<string>>(), y: (dynamic)new A<A<long>>());
            rez += y[t: (dynamic)new A<A<string>>(), x: (dynamic)new A<A<long>>()];
            y.ev += Foo;
            y.Raise((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            var t = new E();
            rez += t.M((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            rez += ((dynamic)y).P;
            rez += t[(dynamic)new A<A<string>>(), (dynamic)new A<A<long>>()];
            rez += t.M(t: (dynamic)new A<A<string>>(), y: (dynamic)new A<A<long>>());
            rez += t[t: (dynamic)new A<A<string>>(), x: (dynamic)new A<A<long>>()];
            t.ev += Foo;
            t.Raise((dynamic)new A<A<string>>(), (dynamic)new A<A<long>>());
            return rez;
        }

        private static int Foo(A<A<long>> x, A<A<long>> y)
        {
            return 0;
        }

        private static int Foo(A<A<string>> x, A<A<long>> y)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr005.ovr005
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    public class C<T>
    {
        public virtual int M<U>(T x, U u)
        {
            return 1;
        }
    }

    public class D : C<int>
    {
        public override int M<U>(int y, U u)
        {
            return 0;
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M<int>((dynamic)0, (dynamic)4);
            rez += x.M<long>(y: (dynamic)0, u: (dynamic)4);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr006.ovr006
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(29,16\).*CS0114</Expects>
    //<Expects Status=warning>\(30,16\).*CS0114</Expects>
    //<Expects Status=warning>\(31,16\).*CS0114</Expects>
    //<Expects Status=warning>\(32,27\).*CS0114</Expects>
    //<Expects Status=warning>\(33,17\).*CS0114</Expects>

    public delegate int Foo<T>(T t);
    public class C<T>
    {
        public virtual int M(T x)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T> ev;
        public virtual void Raise(T t)
        {
            ev(t);
        }
    }

    public class D : C<int>
    {
        public int M(int y)
        {
            return 0;
        }

        public int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public int this[int x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public event Foo<int> ev;
        public void Raise(int t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr007.ovr007
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    public class C<T>
    {
        public virtual int M(T x)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T> ev;
        public virtual void Raise(T t)
        {
            ev(t);
        }
    }

    public class D : C<int>
    {
        public new int M(int y)
        {
            return 0;
        }

        public new int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public new int this[int x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public new event Foo<int> ev;
        public new void Raise(int t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr009.ovr009
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    abstract public class C<T>
    {
        public abstract int M(T x);
        public abstract int P
        {
            get;
            set;
        }

        public abstract int this[T x]
        {
            get;
            set;
        }

        public abstract event Foo<T> ev;
        public abstract void Raise(T t);
    }

    public class D : C<int>
    {
        public override int M(int y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override int this[int x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override event Foo<int> ev;
        public override void Raise(int t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int x)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.derived.ovr011.ovr011
{
    // <Title>Virtual generic methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate int Foo<T>(T t);
    public class C<T>
    {
        public virtual int M(T x)
        {
            return 1;
        }

        public virtual int P
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual int this[T x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public virtual event Foo<T> ev;
        public virtual void Raise(T t)
        {
            ev(t);
        }
    }

    public class D : C<int?>
    {
        public override int M(int? y)
        {
            return 0;
        }

        public override int P
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override int this[int? x]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public override event Foo<int?> ev;
        public override void Raise(int? t)
        {
            ev(t);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var x = new D();
            rez += x.M((dynamic)0);
            rez += ((dynamic)x).P;
            rez += x[(dynamic)3];
            rez += x.M(y: (dynamic)0);
            rez += x[x: (dynamic)3];
            x.ev += Foo;
            x.Raise((dynamic)3);
            return rez;
        }

        private static int Foo(int? x)
        {
            return 0;
        }
    }
    // </Code>
}
