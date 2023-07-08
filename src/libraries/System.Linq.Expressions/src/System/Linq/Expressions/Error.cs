// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    ///    Strongly-typed and parameterized exception factory.
    /// </summary>
    internal static class Error
    {
        /// <summary>
        /// ArgumentException with message like "reducible nodes must override Expression.Reduce()"
        /// </summary>
        internal static ArgumentException ReducibleMustOverrideReduce()
        {
            return new ArgumentException(Strings.ReducibleMustOverrideReduce);
        }
        /// <summary>
        /// ArgumentException with message like "Argument count must be greater than number of named arguments."
        /// </summary>
        internal static ArgumentException ArgCntMustBeGreaterThanNameCnt()
        {
            return new ArgumentException(Strings.ArgCntMustBeGreaterThanNameCnt);
        }
        /// <summary>
        /// InvalidOperationException with message like "An IDynamicMetaObjectProvider {0} created an invalid DynamicMetaObject instance."
        /// </summary>
        internal static InvalidOperationException InvalidMetaObjectCreated(object? p0)
        {
            return new InvalidOperationException(Strings.InvalidMetaObjectCreated(p0));
        }
        /// <summary>
        /// System.Reflection.AmbiguousMatchException with message like "More than one key matching '{0}' was found in the ExpandoObject."
        /// </summary>
        internal static AmbiguousMatchException AmbiguousMatchInExpandoObject(object? p0)
        {
            return new AmbiguousMatchException(Strings.AmbiguousMatchInExpandoObject(p0));
        }
        /// <summary>
        /// ArgumentException with message like "An element with the same key '{0}' already exists in the ExpandoObject."
        /// </summary>
        internal static ArgumentException SameKeyExistsInExpando(object? key)
        {
            return new ArgumentException(Strings.SameKeyExistsInExpando(key), nameof(key));
        }
        /// <summary>
        /// System.Collections.Generic.KeyNotFoundException with message like "The specified key '{0}' does not exist in the ExpandoObject."
        /// </summary>
        internal static KeyNotFoundException KeyDoesNotExistInExpando(object? p0)
        {
            return new KeyNotFoundException(Strings.KeyDoesNotExistInExpando(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Collection was modified; enumeration operation may not execute."
        /// </summary>
        internal static InvalidOperationException CollectionModifiedWhileEnumerating()
        {
            return new InvalidOperationException(Strings.CollectionModifiedWhileEnumerating);
        }
        /// <summary>
        /// NotSupportedException with message like "Collection is read-only."
        /// </summary>
        internal static NotSupportedException CollectionReadOnly()
        {
            return new NotSupportedException(Strings.CollectionReadOnly);
        }
        /// <summary>
        /// ArgumentException with message like "node cannot reduce to itself or null"
        /// </summary>
        internal static ArgumentException MustReduceToDifferent()
        {
            return new ArgumentException(Strings.MustReduceToDifferent);
        }
        /// <summary>
        /// InvalidOperationException with message like "The result type '{0}' of the binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static InvalidOperationException BinderNotCompatibleWithCallSite(object? p0, object? p1, object? p2)
        {
            return new InvalidOperationException(Strings.BinderNotCompatibleWithCallSite(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "The result of the dynamic binding produced by the object with type '{0}' for the binder '{1}' needs at least one restriction."
        /// </summary>
        internal static InvalidOperationException DynamicBindingNeedsRestrictions(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.DynamicBindingNeedsRestrictions(p0, p1));
        }
        /// <summary>
        /// InvalidCastException with message like "The result type '{0}' of the dynamic binding produced by the object with type '{1}' for the binder '{2}' is not compatible with the result type '{3}' expected by the call site."
        /// </summary>
        internal static InvalidCastException DynamicObjectResultNotAssignable(object? p0, object? p1, object? p2, object? p3)
        {
            return new InvalidCastException(Strings.DynamicObjectResultNotAssignable(p0, p1, p2, p3));
        }
        /// <summary>
        /// InvalidCastException with message like "The result type '{0}' of the dynamic binding produced by binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static InvalidCastException DynamicBinderResultNotAssignable(object? p0, object? p1, object? p2)
        {
            return new InvalidCastException(Strings.DynamicBinderResultNotAssignable(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Bind cannot return null."
        /// </summary>
        internal static InvalidOperationException BindingCannotBeNull()
        {
            return new InvalidOperationException(Strings.BindingCannotBeNull);
        }
        /// <summary>
        /// ArgumentException with message like "cannot assign from the reduced node type to the original node type"
        /// </summary>
        internal static ArgumentException ReducedNotCompatible()
        {
            return new ArgumentException(Strings.ReducedNotCompatible);
        }
        /// <summary>
        /// ArgumentException with message like "Setter must have parameters."
        /// </summary>
        internal static ArgumentException SetterHasNoParams(string? paramName)
        {
            return new ArgumentException(Strings.SetterHasNoParams, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Property cannot have a managed pointer type."
        /// </summary>
        internal static ArgumentException PropertyCannotHaveRefType(string? paramName)
        {
            return new ArgumentException(Strings.PropertyCannotHaveRefType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Indexing parameters of getter and setter must match."
        /// </summary>
        internal static ArgumentException IndexesOfSetGetMustMatch(string? paramName)
        {
            return new ArgumentException(Strings.IndexesOfSetGetMustMatch, paramName);
        }
        /// <summary>
        /// InvalidOperationException with message like "Type parameter is {0}. Expected a delegate."
        /// </summary>
        internal static InvalidOperationException TypeParameterIsNotDelegate(object? p0)
        {
            return new InvalidOperationException(Strings.TypeParameterIsNotDelegate(p0));
        }
        /// <summary>
        /// ArgumentException with message like "First argument of delegate must be CallSite"
        /// </summary>
        internal static ArgumentException FirstArgumentMustBeCallSite()
        {
            return new ArgumentException(Strings.FirstArgumentMustBeCallSite);
        }
        /// <summary>
        /// ArgumentException with message like "Accessor method should not have VarArgs."
        /// </summary>
        internal static ArgumentException AccessorsCannotHaveVarArgs(string? paramName)
        {
            return new ArgumentException(Strings.AccessorsCannotHaveVarArgs, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Accessor indexes cannot be passed ByRef."
        /// </summary>
        private static ArgumentException AccessorsCannotHaveByRefArgs(string? paramName)
        {
            return new ArgumentException(Strings.AccessorsCannotHaveByRefArgs, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Accessor indexes cannot be passed ByRef."
        /// </summary>
        internal static ArgumentException AccessorsCannotHaveByRefArgs(string? paramName, int index)
        {
            return AccessorsCannotHaveByRefArgs(GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Type must be derived from System.Delegate"
        /// </summary>
        internal static ArgumentException TypeMustBeDerivedFromSystemDelegate()
        {
            return new ArgumentException(Strings.TypeMustBeDerivedFromSystemDelegate);
        }
        /// <summary>
        /// InvalidOperationException with message like "No or Invalid rule produced"
        /// </summary>
        internal static InvalidOperationException NoOrInvalidRuleProduced()
        {
            return new InvalidOperationException(Strings.NoOrInvalidRuleProduced);
        }
        /// <summary>
        /// ArgumentException with message like "Bounds count cannot be less than 1"
        /// </summary>
        internal static ArgumentException BoundsCannotBeLessThanOne(string? paramName)
        {
            return new ArgumentException(Strings.BoundsCannotBeLessThanOne, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Type must not be ByRef"
        /// </summary>
        internal static ArgumentException TypeMustNotBeByRef(string? paramName)
        {
            return new ArgumentException(Strings.TypeMustNotBeByRef, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Type must not be a pointer type"
        /// </summary>
        internal static ArgumentException TypeMustNotBePointer(string? paramName)
        {
            return new ArgumentException(Strings.TypeMustNotBePointer, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Setter should have void type."
        /// </summary>
        internal static ArgumentException SetterMustBeVoid(string? paramName)
        {
            return new ArgumentException(Strings.SetterMustBeVoid, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Property type must match the value type of getter"
        /// </summary>
        internal static ArgumentException PropertyTypeMustMatchGetter(string? paramName)
        {
            return new ArgumentException(Strings.PropertyTypeMustMatchGetter, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Property type must match the value type of setter"
        /// </summary>
        internal static ArgumentException PropertyTypeMustMatchSetter(string? paramName)
        {
            return new ArgumentException(Strings.PropertyTypeMustMatchSetter, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Both accessors must be static."
        /// </summary>
        internal static ArgumentException BothAccessorsMustBeStatic(string? paramName)
        {
            return new ArgumentException(Strings.BothAccessorsMustBeStatic, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Static field requires null instance, non-static field requires non-null instance."
        /// </summary>
        internal static ArgumentException OnlyStaticFieldsHaveNullInstance(string? paramName)
        {
            return new ArgumentException(Strings.OnlyStaticFieldsHaveNullInstance, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Static property requires null instance, non-static property requires non-null instance."
        /// </summary>
        internal static ArgumentException OnlyStaticPropertiesHaveNullInstance(string? paramName)
        {
            return new ArgumentException(Strings.OnlyStaticPropertiesHaveNullInstance, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Static method requires null instance, non-static method requires non-null instance."
        /// </summary>
        internal static ArgumentException OnlyStaticMethodsHaveNullInstance()
        {
            return new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance);
        }
        /// <summary>
        /// ArgumentException with message like "Property cannot have a void type."
        /// </summary>
        internal static ArgumentException PropertyTypeCannotBeVoid(string? paramName)
        {
            return new ArgumentException(Strings.PropertyTypeCannotBeVoid, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Can only unbox from an object or interface type to a value type."
        /// </summary>
        internal static ArgumentException InvalidUnboxType(string? paramName)
        {
            return new ArgumentException(Strings.InvalidUnboxType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Expression must be writeable"
        /// </summary>
        internal static ArgumentException ExpressionMustBeWriteable(string? paramName)
        {
            return new ArgumentException(Strings.ExpressionMustBeWriteable, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must not have a value type."
        /// </summary>
        internal static ArgumentException ArgumentMustNotHaveValueType(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustNotHaveValueType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "must be reducible node"
        /// </summary>
        internal static ArgumentException MustBeReducible()
        {
            return new ArgumentException(Strings.MustBeReducible);
        }
        /// <summary>
        /// ArgumentException with message like "All test values must have the same type."
        /// </summary>
        internal static ArgumentException AllTestValuesMustHaveSameType(string? paramName)
        {
            return new ArgumentException(Strings.AllTestValuesMustHaveSameType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "All case bodies and the default body must have the same type."
        /// </summary>
        internal static ArgumentException AllCaseBodiesMustHaveSameType(string? paramName)
        {
            return new ArgumentException(Strings.AllCaseBodiesMustHaveSameType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Default body must be supplied if case bodies are not System.Void."
        /// </summary>
        internal static ArgumentException DefaultBodyMustBeSupplied(string? paramName)
        {
            return new ArgumentException(Strings.DefaultBodyMustBeSupplied, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Label type must be System.Void if an expression is not supplied"
        /// </summary>
        internal static ArgumentException LabelMustBeVoidOrHaveExpression(string? paramName)
        {
            return new ArgumentException(Strings.LabelMustBeVoidOrHaveExpression, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Type must be System.Void for this label argument"
        /// </summary>
        internal static ArgumentException LabelTypeMustBeVoid(string? paramName)
        {
            return new ArgumentException(Strings.LabelTypeMustBeVoid, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Quoted expression must be a lambda"
        /// </summary>
        internal static ArgumentException QuotedExpressionMustBeLambda(string? paramName)
        {
            return new ArgumentException(Strings.QuotedExpressionMustBeLambda, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Variable '{0}' uses unsupported type '{1}'. Reference types are not supported for variables."
        /// </summary>
        internal static ArgumentException VariableMustNotBeByRef(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.VariableMustNotBeByRef(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Variable '{0}' uses unsupported type '{1}'. Reference types are not supported for variables."
        /// </summary>
        internal static ArgumentException VariableMustNotBeByRef(object? p0, object? p1, string? paramName, int index)
        {
            return VariableMustNotBeByRef(p0, p1, GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Found duplicate parameter '{0}'. Each ParameterExpression in the list must be a unique object."
        /// </summary>
        private static ArgumentException DuplicateVariable(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.DuplicateVariable(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Found duplicate parameter '{0}'. Each ParameterExpression in the list must be a unique object."
        /// </summary>
        internal static ArgumentException DuplicateVariable(object? p0, string? paramName, int index)
        {
            return DuplicateVariable(p0, GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Start and End must be well ordered"
        /// </summary>
        internal static ArgumentException StartEndMustBeOrdered()
        {
            return new ArgumentException(Strings.StartEndMustBeOrdered);
        }
        /// <summary>
        /// ArgumentException with message like "fault cannot be used with catch or finally clauses"
        /// </summary>
        internal static ArgumentException FaultCannotHaveCatchOrFinally(string? paramName)
        {
            return new ArgumentException(Strings.FaultCannotHaveCatchOrFinally, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "try must have at least one catch, finally, or fault clause"
        /// </summary>
        internal static ArgumentException TryMustHaveCatchFinallyOrFault()
        {
            return new ArgumentException(Strings.TryMustHaveCatchFinallyOrFault);
        }
        /// <summary>
        /// ArgumentException with message like "Body of catch must have the same type as body of try."
        /// </summary>
        internal static ArgumentException BodyOfCatchMustHaveSameTypeAsBodyOfTry()
        {
            return new ArgumentException(Strings.BodyOfCatchMustHaveSameTypeAsBodyOfTry);
        }
        /// <summary>
        /// InvalidOperationException with message like "Extension node must override the property {0}."
        /// </summary>
        internal static InvalidOperationException ExtensionNodeMustOverrideProperty(object? p0)
        {
            return new InvalidOperationException(Strings.ExtensionNodeMustOverrideProperty(p0));
        }
        /// <summary>
        /// ArgumentException with message like "User-defined operator method '{0}' must be static."
        /// </summary>
        internal static ArgumentException UserDefinedOperatorMustBeStatic(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.UserDefinedOperatorMustBeStatic(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "User-defined operator method '{0}' must not be void."
        /// </summary>
        internal static ArgumentException UserDefinedOperatorMustNotBeVoid(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.UserDefinedOperatorMustNotBeVoid(p0), paramName);
        }
        /// <summary>
        /// InvalidOperationException with message like "No coercion operator is defined between types '{0}' and '{1}'."
        /// </summary>
        internal static InvalidOperationException CoercionOperatorNotDefined(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.CoercionOperatorNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The unary operator {0} is not defined for the type '{1}'."
        /// </summary>
        internal static InvalidOperationException UnaryOperatorNotDefined(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.UnaryOperatorNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The binary operator {0} is not defined for the types '{1}' and '{2}'."
        /// </summary>
        internal static InvalidOperationException BinaryOperatorNotDefined(object? p0, object? p1, object? p2)
        {
            return new InvalidOperationException(Strings.BinaryOperatorNotDefined(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Reference equality is not defined for the types '{0}' and '{1}'."
        /// </summary>
        internal static InvalidOperationException ReferenceEqualityNotDefined(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.ReferenceEqualityNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The operands for operator '{0}' do not match the parameters of method '{1}'."
        /// </summary>
        internal static InvalidOperationException OperandTypesDoNotMatchParameters(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.OperandTypesDoNotMatchParameters(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The return type of overload method for operator '{0}' does not match the parameter type of conversion method '{1}'."
        /// </summary>
        internal static InvalidOperationException OverloadOperatorTypeDoesNotMatchConversionType(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.OverloadOperatorTypeDoesNotMatchConversionType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Conversion is not supported for arithmetic types without operator overloading."
        /// </summary>
        internal static InvalidOperationException ConversionIsNotSupportedForArithmeticTypes()
        {
            return new InvalidOperationException(Strings.ConversionIsNotSupportedForArithmeticTypes);
        }
        /// <summary>
        /// ArgumentException with message like "Argument type cannot be void"
        /// </summary>
        internal static ArgumentException ArgumentTypeCannotBeVoid()
        {
            return new ArgumentException(Strings.ArgumentTypeCannotBeVoid);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be array"
        /// </summary>
        internal static ArgumentException ArgumentMustBeArray(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeArray, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be boolean"
        /// </summary>
        internal static ArgumentException ArgumentMustBeBoolean(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeBoolean, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined equality method '{0}' must return a boolean value."
        /// </summary>
        internal static ArgumentException EqualityMustReturnBoolean(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.EqualityMustReturnBoolean(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be either a FieldInfo or PropertyInfo"
        /// </summary>
        internal static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfo(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfo, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be either a FieldInfo, PropertyInfo or MethodInfo"
        /// </summary>
        private static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be either a FieldInfo, PropertyInfo or MethodInfo"
        /// </summary>
        internal static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(string? paramName, int index)
        {
            return ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Argument must be an instance member"
        /// </summary>
        private static ArgumentException ArgumentMustBeInstanceMember(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeInstanceMember, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be an instance member"
        /// </summary>
        internal static ArgumentException ArgumentMustBeInstanceMember(string? paramName, int index)
        {
            return ArgumentMustBeInstanceMember(GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be of an integer type"
        /// </summary>
        private static ArgumentException ArgumentMustBeInteger(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeInteger, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be of an integer type"
        /// </summary>
        internal static ArgumentException ArgumentMustBeInteger(string? paramName, int index)
        {
            return ArgumentMustBeInteger(GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Argument for array index must be of type Int32"
        /// </summary>
        internal static ArgumentException ArgumentMustBeArrayIndexType(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeArrayIndexType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument for array index must be of type Int32"
        /// </summary>
        internal static ArgumentException ArgumentMustBeArrayIndexType(string? paramName, int index)
        {
            return ArgumentMustBeArrayIndexType(GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be single-dimensional, zero-based array type"
        /// </summary>
        internal static ArgumentException ArgumentMustBeSingleDimensionalArrayType(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMustBeSingleDimensionalArrayType, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument types do not match"
        /// </summary>
        internal static ArgumentException ArgumentTypesMustMatch()
        {
            return new ArgumentException(Strings.ArgumentTypesMustMatch);
        }
        /// <summary>
        /// ArgumentException with message like "Argument types do not match"
        /// </summary>
        internal static ArgumentException ArgumentTypesMustMatch(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentTypesMustMatch, paramName);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot auto initialize elements of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static InvalidOperationException CannotAutoInitializeValueTypeElementThroughProperty(object? p0)
        {
            return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeElementThroughProperty(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot auto initialize members of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static InvalidOperationException CannotAutoInitializeValueTypeMemberThroughProperty(object? p0)
        {
            return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeMemberThroughProperty(p0));
        }
        /// <summary>
        /// ArgumentException with message like "The type used in TypeAs Expression must be of reference or nullable type, {0} is neither"
        /// </summary>
        internal static ArgumentException IncorrectTypeForTypeAs(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.IncorrectTypeForTypeAs(p0), paramName);
        }
        /// <summary>
        /// InvalidOperationException with message like "Coalesce used with type that cannot be null"
        /// </summary>
        internal static InvalidOperationException CoalesceUsedOnNonNullType()
        {
            return new InvalidOperationException(Strings.CoalesceUsedOnNonNullType);
        }
        /// <summary>
        /// InvalidOperationException with message like "An expression of type '{0}' cannot be used to initialize an array of type '{1}'"
        /// </summary>
        internal static InvalidOperationException ExpressionTypeCannotInitializeArrayType(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.ExpressionTypeCannotInitializeArrayType(p0, p1));
        }

        /// <summary>
        /// ArgumentException with message like " Argument type '{0}' does not match the corresponding member type '{1}'"
        /// </summary>
        private static ArgumentException ArgumentTypeDoesNotMatchMember(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.ArgumentTypeDoesNotMatchMember(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like " Argument type '{0}' does not match the corresponding member type '{1}'"
        /// </summary>
        internal static ArgumentException ArgumentTypeDoesNotMatchMember(object? p0, object? p1, string? paramName, int index)
        {
            return ArgumentTypeDoesNotMatchMember(p0, p1, GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like " The member '{0}' is not declared on type '{1}' being created"
        /// </summary>
        private static ArgumentException ArgumentMemberNotDeclOnType(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.ArgumentMemberNotDeclOnType(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like " The member '{0}' is not declared on type '{1}' being created"
        /// </summary>
        internal static ArgumentException ArgumentMemberNotDeclOnType(object? p0, object? p1, string? paramName, int index)
        {
            return ArgumentMemberNotDeclOnType(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for return type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchReturn(object? p0, object? p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchReturn(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for assignment to type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchAssignment(object? p0, object? p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchAssignment(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for label of type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchLabel(object? p0, object? p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchLabel(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be invoked"
        /// </summary>
        internal static ArgumentException ExpressionTypeNotInvocable(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeNotInvocable(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static ArgumentException FieldNotDefinedForType(object? p0, object? p1)
        {
            return new ArgumentException(Strings.FieldNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static ArgumentException InstanceFieldNotDefinedForType(object? p0, object? p1)
        {
            return new ArgumentException(Strings.InstanceFieldNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Field '{0}.{1}' is not defined for type '{2}'"
        /// </summary>
        internal static ArgumentException FieldInfoNotDefinedForType(object? p0, object? p1, object? p2)
        {
            return new ArgumentException(Strings.FieldInfoNotDefinedForType(p0, p1, p2));
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of indexes"
        /// </summary>
        internal static ArgumentException IncorrectNumberOfIndexes()
        {
            return new ArgumentException(Strings.IncorrectNumberOfIndexes);
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of parameters supplied for lambda declaration"
        /// </summary>
        internal static ArgumentException IncorrectNumberOfLambdaDeclarationParameters()
        {
            return new ArgumentException(Strings.IncorrectNumberOfLambdaDeclarationParameters);
        }

        /// <summary>
        /// ArgumentException with message like " Incorrect number of members for constructor"
        /// </summary>
        internal static ArgumentException IncorrectNumberOfMembersForGivenConstructor()
        {
            return new ArgumentException(Strings.IncorrectNumberOfMembersForGivenConstructor);
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments for the given members "
        /// </summary>
        internal static ArgumentException IncorrectNumberOfArgumentsForMembers()
        {
            return new ArgumentException(Strings.IncorrectNumberOfArgumentsForMembers);
        }
        /// <summary>
        /// ArgumentException with message like "Lambda type parameter must be derived from System.MulticastDelegate"
        /// </summary>
        internal static ArgumentException LambdaTypeMustBeDerivedFromSystemDelegate(string? paramName)
        {
            return new ArgumentException(Strings.LambdaTypeMustBeDerivedFromSystemDelegate, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Member '{0}' not field or property"
        /// </summary>
        internal static ArgumentException MemberNotFieldOrProperty(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.MemberNotFieldOrProperty(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Method {0} contains generic parameters"
        /// </summary>
        internal static ArgumentException MethodContainsGenericParameters(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.MethodContainsGenericParameters(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Method {0} is a generic method definition"
        /// </summary>
        internal static ArgumentException MethodIsGeneric(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.MethodIsGeneric(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "The method '{0}.{1}' is not a property accessor"
        /// </summary>
        private static ArgumentException MethodNotPropertyAccessor(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.MethodNotPropertyAccessor(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "The method '{0}.{1}' is not a property accessor"
        /// </summary>
        internal static ArgumentException MethodNotPropertyAccessor(object? p0, object? p1, string? paramName, int index)
        {
            return MethodNotPropertyAccessor(p0, p1, GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'get' accessor"
        /// </summary>
        internal static ArgumentException PropertyDoesNotHaveGetter(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveGetter(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'get' accessor"
        /// </summary>
        internal static ArgumentException PropertyDoesNotHaveGetter(object? p0, string? paramName, int index)
        {
            return PropertyDoesNotHaveGetter(p0, GetParamName(paramName, index));
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'set' accessor"
        /// </summary>
        internal static ArgumentException PropertyDoesNotHaveSetter(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveSetter(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'get' or 'set' accessors"
        /// </summary>
        internal static ArgumentException PropertyDoesNotHaveAccessor(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveAccessor(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "'{0}' is not a member of type '{1}'"
        /// </summary>
        internal static ArgumentException NotAMemberOfType(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.NotAMemberOfType(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "'{0}' is not a member of type '{1}'"
        /// </summary>
        internal static ArgumentException NotAMemberOfType(object? p0, object? p1, string? paramName, int index)
        {
            return NotAMemberOfType(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "'{0}' is not a member of any type"
        /// </summary>
        internal static ArgumentException NotAMemberOfAnyType(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.NotAMemberOfAnyType(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "ParameterExpression of type '{0}' cannot be used for delegate parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException ParameterExpressionNotValidAsDelegate(object? p0, object? p1)
        {
            return new ArgumentException(Strings.ParameterExpressionNotValidAsDelegate(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static ArgumentException PropertyNotDefinedForType(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.PropertyNotDefinedForType(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static ArgumentException InstancePropertyNotDefinedForType(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.InstancePropertyNotDefinedForType(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}' that takes no argument is not defined for type '{1}'"
        /// </summary>
        internal static ArgumentException InstancePropertyWithoutParameterNotDefinedForType(object? p0, object? p1)
        {
            return new ArgumentException(Strings.InstancePropertyWithoutParameterNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}{1}' is not defined for type '{2}'"
        /// </summary>
        internal static ArgumentException InstancePropertyWithSpecifiedParametersNotDefinedForType(object? p0, object? p1, object? p2, string? paramName)
        {
            return new ArgumentException(Strings.InstancePropertyWithSpecifiedParametersNotDefinedForType(p0, p1, p2), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Method '{0}' declared on type '{1}' cannot be called with instance of type '{2}'"
        /// </summary>
        internal static ArgumentException InstanceAndMethodTypeMismatch(object? p0, object? p1, object? p2)
        {
            return new ArgumentException(Strings.InstanceAndMethodTypeMismatch(p0, p1, p2));
        }

        /// <summary>
        /// ArgumentException with message like "Type '{0}' does not have a default constructor"
        /// </summary>
        internal static ArgumentException TypeMissingDefaultConstructor(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.TypeMissingDefaultConstructor(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Element initializer method must be named 'Add'"
        /// </summary>
        internal static ArgumentException ElementInitializerMethodNotAdd(string? paramName)
        {
            return new ArgumentException(Strings.ElementInitializerMethodNotAdd, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Parameter '{0}' of element initializer method '{1}' must not be a pass by reference parameter"
        /// </summary>
        internal static ArgumentException ElementInitializerMethodNoRefOutParam(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.ElementInitializerMethodNoRefOutParam(p0, p1), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Element initializer method must have at least 1 parameter"
        /// </summary>
        internal static ArgumentException ElementInitializerMethodWithZeroArgs(string? paramName)
        {
            return new ArgumentException(Strings.ElementInitializerMethodWithZeroArgs, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Element initializer method must be an instance method"
        /// </summary>
        internal static ArgumentException ElementInitializerMethodStatic(string? paramName)
        {
            return new ArgumentException(Strings.ElementInitializerMethodStatic, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Type '{0}' is not IEnumerable"
        /// </summary>
        internal static ArgumentException TypeNotIEnumerable(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.TypeNotIEnumerable(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Unhandled binary: {0}"
        /// </summary>
        internal static ArgumentException UnhandledBinary(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.UnhandledBinary(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled binding "
        /// </summary>
        internal static ArgumentException UnhandledBinding()
        {
            return new ArgumentException(Strings.UnhandledBinding);
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled Binding Type: {0}"
        /// </summary>
        internal static ArgumentException UnhandledBindingType(object? p0)
        {
            return new ArgumentException(Strings.UnhandledBindingType(p0));
        }

        /// <summary>
        /// ArgumentException with message like "Unhandled unary: {0}"
        /// </summary>
        internal static ArgumentException UnhandledUnary(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.UnhandledUnary(p0), paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Unknown binding type"
        /// </summary>
        internal static ArgumentException UnknownBindingType(int index)
        {
            return new ArgumentException(Strings.UnknownBindingType, $"bindings[{index}]");
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must have identical parameter and return types."
        /// </summary>
        internal static ArgumentException UserDefinedOpMustHaveConsistentTypes(object? p0, object? p1)
        {
            return new ArgumentException(Strings.UserDefinedOpMustHaveConsistentTypes(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must return the same type as its parameter or a derived type."
        /// </summary>
        internal static ArgumentException UserDefinedOpMustHaveValidReturnType(object? p0, object? p1)
        {
            return new ArgumentException(Strings.UserDefinedOpMustHaveValidReturnType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must have associated boolean True and False operators."
        /// </summary>
        internal static ArgumentException LogicalOperatorMustHaveBooleanOperators(object? p0, object? p1)
        {
            return new ArgumentException(Strings.LogicalOperatorMustHaveBooleanOperators(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "No method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static InvalidOperationException MethodWithArgsDoesNotExistOnType(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.MethodWithArgsDoesNotExistOnType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "No generic method '{0}' on type '{1}' is compatible with the supplied type arguments and arguments. No type arguments should be provided if the method is non-generic. "
        /// </summary>
        internal static InvalidOperationException GenericMethodWithArgsDoesNotExistOnType(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.GenericMethodWithArgsDoesNotExistOnType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "More than one method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static InvalidOperationException MethodWithMoreThanOneMatch(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.MethodWithMoreThanOneMatch(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "More than one property '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static InvalidOperationException PropertyWithMoreThanOneMatch(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.PropertyWithMoreThanOneMatch(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "An incorrect number of type arguments were specified for the declaration of a Func type."
        /// </summary>
        internal static ArgumentException IncorrectNumberOfTypeArgsForFunc(string? paramName)
        {
            return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForFunc, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "An incorrect number of type arguments were specified for the declaration of an Action type."
        /// </summary>
        internal static ArgumentException IncorrectNumberOfTypeArgsForAction(string? paramName)
        {
            return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForAction, paramName);
        }
        /// <summary>
        /// ArgumentException with message like "Argument type cannot be System.Void."
        /// </summary>
        internal static ArgumentException ArgumentCannotBeOfTypeVoid(string? paramName)
        {
            return new ArgumentException(Strings.ArgumentCannotBeOfTypeVoid, paramName);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot redefine label '{0}' in an inner block."
        /// </summary>
        internal static InvalidOperationException LabelTargetAlreadyDefined(object? p0)
        {
            return new InvalidOperationException(Strings.LabelTargetAlreadyDefined(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to undefined label '{0}'."
        /// </summary>
        internal static InvalidOperationException LabelTargetUndefined(object? p0)
        {
            return new InvalidOperationException(Strings.LabelTargetUndefined(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot leave a finally block."
        /// </summary>
        internal static InvalidOperationException ControlCannotLeaveFinally()
        {
            return new InvalidOperationException(Strings.ControlCannotLeaveFinally);
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot leave a filter test."
        /// </summary>
        internal static InvalidOperationException ControlCannotLeaveFilterTest()
        {
            return new InvalidOperationException(Strings.ControlCannotLeaveFilterTest);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to ambiguous label '{0}'."
        /// </summary>
        internal static InvalidOperationException AmbiguousJump(object? p0)
        {
            return new InvalidOperationException(Strings.AmbiguousJump(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot enter a try block."
        /// </summary>
        internal static InvalidOperationException ControlCannotEnterTry()
        {
            return new InvalidOperationException(Strings.ControlCannotEnterTry);
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot enter an expression--only statements can be jumped into."
        /// </summary>
        internal static InvalidOperationException ControlCannotEnterExpression()
        {
            return new InvalidOperationException(Strings.ControlCannotEnterExpression);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to non-local label '{0}' with a value. Only jumps to labels defined in outer blocks can pass values."
        /// </summary>
        internal static InvalidOperationException NonLocalJumpWithValue(object? p0)
        {
            return new InvalidOperationException(Strings.NonLocalJumpWithValue(p0));
        }

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// InvalidOperationException with message like "CompileToMethod cannot compile constant '{0}' because it is a non-trivial value, such as a live object. Instead, create an expression tree that can construct this value."
        /// </summary>
        internal static InvalidOperationException CannotCompileConstant(object? p0)
        {
            return new InvalidOperationException(Strings.CannotCompileConstant(p0));
        }
        /// <summary>
        /// NotSupportedException with message like "Dynamic expressions are not supported by CompileToMethod. Instead, create an expression tree that uses System.Runtime.CompilerServices.CallSite."
        /// </summary>
        internal static NotSupportedException CannotCompileDynamic()
        {
            return new NotSupportedException(Strings.CannotCompileDynamic);
        }
        /// <summary>
        /// ArgumentException with message like "MethodBuilder does not have a valid TypeBuilder"
        /// </summary>
        internal static ArgumentException MethodBuilderDoesNotHaveTypeBuilder()
        {
            return new ArgumentException(Strings.MethodBuilderDoesNotHaveTypeBuilder);
        }
#endif
        /// <summary>
        /// InvalidOperationException with message like "Invalid lvalue for assignment: {0}."
        /// </summary>
        internal static InvalidOperationException InvalidLvalue(ExpressionType p0)
        {
            return new InvalidOperationException(Strings.InvalidLvalue(p0));
        }

        /// <summary>
        /// InvalidOperationException with message like "variable '{0}' of type '{1}' referenced from scope '{2}', but it is not defined"
        /// </summary>
        internal static InvalidOperationException UndefinedVariable(object? p0, object? p1, object? p2)
        {
            return new InvalidOperationException(Strings.UndefinedVariable(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot close over byref parameter '{0}' referenced in lambda '{1}'"
        /// </summary>
        internal static InvalidOperationException CannotCloseOverByRef(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.CannotCloseOverByRef(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Unexpected VarArgs call to method '{0}'"
        /// </summary>
        internal static InvalidOperationException UnexpectedVarArgsCall(object? p0)
        {
            return new InvalidOperationException(Strings.UnexpectedVarArgsCall(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rethrow statement is valid only inside a Catch block."
        /// </summary>
        internal static InvalidOperationException RethrowRequiresCatch()
        {
            return new InvalidOperationException(Strings.RethrowRequiresCatch);
        }
        /// <summary>
        /// InvalidOperationException with message like "Try expression is not allowed inside a filter body."
        /// </summary>
        internal static InvalidOperationException TryNotAllowedInFilter()
        {
            return new InvalidOperationException(Strings.TryNotAllowedInFilter);
        }
        /// <summary>
        /// InvalidOperationException with message like "When called from '{0}', rewriting a node of type '{1}' must return a non-null value of the same type. Alternatively, override '{2}' and change it to not visit children of this type."
        /// </summary>
        internal static InvalidOperationException MustRewriteToSameNode(object? p0, object? p1, object? p2)
        {
            return new InvalidOperationException(Strings.MustRewriteToSameNode(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rewriting child expression from type '{0}' to type '{1}' is not allowed, because it would change the meaning of the operation. If this is intentional, override '{2}' and change it to allow this rewrite."
        /// </summary>
        internal static InvalidOperationException MustRewriteChildToSameType(object? p0, object? p1, object? p2)
        {
            return new InvalidOperationException(Strings.MustRewriteChildToSameType(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rewritten expression calls operator method '{0}', but the original node had no operator method. If this is intentional, override '{1}' and change it to allow this rewrite."
        /// </summary>
        internal static InvalidOperationException MustRewriteWithoutMethod(object? p0, object? p1)
        {
            return new InvalidOperationException(Strings.MustRewriteWithoutMethod(p0, p1));
        }
        /// <summary>
        /// NotSupportedException with message like "TryExpression is not supported as an argument to method '{0}' because it has an argument with by-ref type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static NotSupportedException TryNotSupportedForMethodsWithRefArgs(object? p0)
        {
            return new NotSupportedException(Strings.TryNotSupportedForMethodsWithRefArgs(p0));
        }
        /// <summary>
        /// NotSupportedException with message like "TryExpression is not supported as a child expression when accessing a member on type '{0}' because it is a value type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static NotSupportedException TryNotSupportedForValueTypeInstances(object? p0)
        {
            return new NotSupportedException(Strings.TryNotSupportedForValueTypeInstances(p0));
        }

        /// <summary>
        /// ArgumentException with message like "Test value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException TestValueTypeDoesNotMatchComparisonMethodParameter(object? p0, object? p1)
        {
            return new ArgumentException(Strings.TestValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Switch value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException SwitchValueTypeDoesNotMatchComparisonMethodParameter(object? p0, object? p1)
        {
            return new ArgumentException(Strings.SwitchValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
        }

#if FEATURE_COMPILE_TO_METHODBUILDER && FEATURE_PDB_GENERATOR
        /// <summary>
        /// NotSupportedException with message like "DebugInfoGenerator created by CreatePdbGenerator can only be used with LambdaExpression.CompileToMethod."
        /// </summary>
        internal static NotSupportedException PdbGeneratorNeedsExpressionCompiler()
        {
            return new NotSupportedException(Strings.PdbGeneratorNeedsExpressionCompiler);
        }
#endif

        /// <summary>
        /// The exception that is thrown when the value of an argument is outside the allowable range of values as defined by the invoked method.
        /// </summary>
        internal static ArgumentOutOfRangeException ArgumentOutOfRange(string? paramName)
        {
            return new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// The exception that is thrown when an invoked method is not supported, or when there is an attempt to read, seek, or write to a stream that does not support the invoked functionality.
        /// </summary>
        internal static NotSupportedException NotSupported()
        {
            return new NotSupportedException();
        }

        /// <summary>
        /// ArgumentException with message like "The constructor should not be static"
        /// </summary>
        internal static ArgumentException NonStaticConstructorRequired(string? paramName)
        {
            return new ArgumentException(Strings.NonStaticConstructorRequired, paramName);
        }

        /// <summary>
        /// InvalidOperationException with message like "Can't compile a NewExpression with a constructor declared on an abstract class"
        /// </summary>
        internal static InvalidOperationException NonAbstractConstructorRequired()
        {
            return new InvalidOperationException(Strings.NonAbstractConstructorRequired);
        }

        /// <summary>
        /// InvalidProgramException with default message.
        /// </summary>
        internal static InvalidProgramException InvalidProgram()
        {
            return new InvalidProgramException();
        }

        /// <summary>
        /// InvalidOperationException with message like "Enumeration has either not started or has already finished."
        /// </summary>
        internal static InvalidOperationException EnumerationIsDone()
        {
            return new InvalidOperationException(Strings.EnumerationIsDone);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} contains generic parameters"
        /// </summary>
        private static ArgumentException TypeContainsGenericParameters(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.TypeContainsGenericParameters(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} contains generic parameters"
        /// </summary>
        internal static ArgumentException TypeContainsGenericParameters(object? p0, string? paramName, int index)
        {
            return TypeContainsGenericParameters(p0, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} is a generic type definition"
        /// </summary>
        internal static ArgumentException TypeIsGeneric(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.TypeIsGeneric(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} is a generic type definition"
        /// </summary>
        internal static ArgumentException TypeIsGeneric(object? p0, string? paramName, int index)
        {
            return TypeIsGeneric(p0, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments for constructor"
        /// </summary>
        internal static ArgumentException IncorrectNumberOfConstructorArguments()
        {
            return new ArgumentException(Strings.IncorrectNumberOfConstructorArguments);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchMethodParameter(object? p0, object? p1, object? p2, string? paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchMethodParameter(object? p0, object? p1, object? p2, string? paramName, int index)
        {
            return ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchParameter(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchParameter(p0, p1), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchParameter(object? p0, object? p1, string? paramName, int index)
        {
            return ExpressionTypeDoesNotMatchParameter(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// InvalidOperationException with message like "Incorrect number of arguments supplied for lambda invocation"
        /// </summary>
        internal static InvalidOperationException IncorrectNumberOfLambdaArguments()
        {
            return new InvalidOperationException(Strings.IncorrectNumberOfLambdaArguments);
        }

        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments supplied for call to method '{0}'"
        /// </summary>
        internal static ArgumentException IncorrectNumberOfMethodCallArguments(object? p0, string? paramName)
        {
            return new ArgumentException(Strings.IncorrectNumberOfMethodCallArguments(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchConstructorParameter(object? p0, object? p1, string? paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchConstructorParameter(p0, p1), paramName);
        }


        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static ArgumentException ExpressionTypeDoesNotMatchConstructorParameter(object? p0, object? p1, string? paramName, int index)
        {
            return ExpressionTypeDoesNotMatchConstructorParameter(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Expression must be readable"
        /// </summary>
        internal static ArgumentException ExpressionMustBeReadable(string? paramName)
        {
            return new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression must be readable"
        /// </summary>
        internal static ArgumentException ExpressionMustBeReadable(string? paramName, int index)
        {
            return ExpressionMustBeReadable(GetParamName(paramName, index));
        }

        internal static ArgumentException InvalidArgumentValue(string? paramName)
        {
            return new ArgumentException(Strings.InvalidArgumentValue_ParamName, paramName);
        }

        internal static ArgumentException NonEmptyCollectionRequired(string? paramName)
        {
            return new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
        }

        internal static ArgumentException InvalidNullValue(Type? type, string? paramName)
        {
            return new ArgumentException(Strings.InvalidNullValue(type), paramName);
        }

        internal static ArgumentException InvalidTypeException(object? value, Type? type, string? paramName)
        {
            return new ArgumentException(Strings.InvalidObjectType(value?.GetType() as object ?? "null", type), paramName);
        }

        private static string? GetParamName(string? paramName, int index)
        {
            if (index >= 0)
            {
                return $"{paramName}[{index}]";
            }

            return paramName;
        }
    }
}
