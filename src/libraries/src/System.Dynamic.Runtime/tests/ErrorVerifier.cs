// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    CallingBaseFinalizeDeprecated, // Do not directly call your base public class Finalize method. It is called automatically from your destructor.
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
    GenericConstraintNotSatisfiedNullableInterface, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'. Nullable types can not satisfy any public interface constraints.
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
        return String.Empty;
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
