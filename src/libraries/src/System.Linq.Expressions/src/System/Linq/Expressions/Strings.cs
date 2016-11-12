// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    /// <summary>
    ///    Strongly-typed and parameterized string resources.
    /// </summary>
    internal static class Strings
    {
        /// <summary>
        /// A string like "reducible nodes must override Expression.Reduce()"
        /// </summary>
        internal static string ReducibleMustOverrideReduce => SR.ReducibleMustOverrideReduce;

        /// <summary>
        /// A string like "node cannot reduce to itself or null"
        /// </summary>
        internal static string MustReduceToDifferent => SR.MustReduceToDifferent;

        /// <summary>
        /// A string like "cannot assign from the reduced node type to the original node type"
        /// </summary>
        internal static string ReducedNotCompatible => SR.ReducedNotCompatible;

        /// <summary>
        /// A string like "Setter must have parameters."
        /// </summary>
        internal static string SetterHasNoParams => SR.SetterHasNoParams;

        /// <summary>
        /// A string like "Property cannot have a managed pointer type."
        /// </summary>
        internal static string PropertyCannotHaveRefType => SR.PropertyCannotHaveRefType;

        /// <summary>
        /// A string like "Indexing parameters of getter and setter must match."
        /// </summary>
        internal static string IndexesOfSetGetMustMatch => SR.IndexesOfSetGetMustMatch;

        /// <summary>
        /// A string like "Accessor method should not have VarArgs."
        /// </summary>
        internal static string AccessorsCannotHaveVarArgs => SR.AccessorsCannotHaveVarArgs;

        /// <summary>
        /// A string like "Accessor indexes cannot be passed ByRef."
        /// </summary>
        internal static string AccessorsCannotHaveByRefArgs => SR.AccessorsCannotHaveByRefArgs;

        /// <summary>
        /// A string like "Bounds count cannot be less than 1"
        /// </summary>
        internal static string BoundsCannotBeLessThanOne => SR.BoundsCannotBeLessThanOne;

        /// <summary>
        /// A string like "Type must not be ByRef"
        /// </summary>
        internal static string TypeMustNotBeByRef => SR.TypeMustNotBeByRef;

        /// <summary>
        /// A string like "Type must not be a pointer type"
        /// </summary>
        internal static string TypeMustNotBePointer => SR.TypeMustNotBePointer;

        /// <summary>
        /// A string like "Type doesn't have constructor with a given signature"
        /// </summary>
        internal static string TypeDoesNotHaveConstructorForTheSignature => SR.TypeDoesNotHaveConstructorForTheSignature;

        /// <summary>
        /// A string like "Setter should have void type."
        /// </summary>
        internal static string SetterMustBeVoid => SR.SetterMustBeVoid;

        /// <summary>
        /// A string like "Property type must match the value type of setter"
        /// </summary>
        internal static string PropertyTypeMustMatchSetter => SR.PropertyTypeMustMatchSetter;

        /// <summary>
        /// A string like "Both accessors must be static."
        /// </summary>
        internal static string BothAccessorsMustBeStatic => SR.BothAccessorsMustBeStatic;

        /// <summary>
        /// A string like "Static field requires null instance, non-static field requires non-null instance."
        /// </summary>
        internal static string OnlyStaticFieldsHaveNullInstance => SR.OnlyStaticFieldsHaveNullInstance;

        /// <summary>
        /// A string like "Static property requires null instance, non-static property requires non-null instance."
        /// </summary>
        internal static string OnlyStaticPropertiesHaveNullInstance => SR.OnlyStaticPropertiesHaveNullInstance;

        /// <summary>
        /// A string like "Static method requires null instance, non-static method requires non-null instance."
        /// </summary>
        internal static string OnlyStaticMethodsHaveNullInstance => SR.OnlyStaticMethodsHaveNullInstance;

        /// <summary>
        /// A string like "Property cannot have a void type."
        /// </summary>
        internal static string PropertyTypeCannotBeVoid => SR.PropertyTypeCannotBeVoid;

        /// <summary>
        /// A string like "Can only unbox from an object or interface type to a value type."
        /// </summary>
        internal static string InvalidUnboxType => SR.InvalidUnboxType;

        /// <summary>
        /// A string like "Expression must be writeable"
        /// </summary>
        internal static string ExpressionMustBeWriteable => SR.ExpressionMustBeWriteable;

        /// <summary>
        /// A string like "Argument must not have a value type."
        /// </summary>
        internal static string ArgumentMustNotHaveValueType => SR.ArgumentMustNotHaveValueType;

        /// <summary>
        /// A string like "must be reducible node"
        /// </summary>
        internal static string MustBeReducible => SR.MustBeReducible;

        /// <summary>
        /// A string like "All test values must have the same type."
        /// </summary>
        internal static string AllTestValuesMustHaveSameType => SR.AllTestValuesMustHaveSameType;

        /// <summary>
        /// A string like "All case bodies and the default body must have the same type."
        /// </summary>
        internal static string AllCaseBodiesMustHaveSameType => SR.AllCaseBodiesMustHaveSameType;

        /// <summary>
        /// A string like "Default body must be supplied if case bodies are not System.Void."
        /// </summary>
        internal static string DefaultBodyMustBeSupplied => SR.DefaultBodyMustBeSupplied;

        /// <summary>
        /// A string like "Label type must be System.Void if an expression is not supplied"
        /// </summary>
        internal static string LabelMustBeVoidOrHaveExpression => SR.LabelMustBeVoidOrHaveExpression;

        /// <summary>
        /// A string like "Type must be System.Void for this label argument"
        /// </summary>
        internal static string LabelTypeMustBeVoid => SR.LabelTypeMustBeVoid;

        /// <summary>
        /// A string like "Quoted expression must be a lambda"
        /// </summary>
        internal static string QuotedExpressionMustBeLambda => SR.QuotedExpressionMustBeLambda;

        /// <summary>
        /// A string like "Collection was modified; enumeration operation may not execute."
        /// </summary>
        internal static string CollectionModifiedWhileEnumerating => SR.CollectionModifiedWhileEnumerating;

        /// <summary>
        /// A string like "Variable '{0}' uses unsupported type '{1}'. Reference types are not supported for variables."
        /// </summary>
        internal static string VariableMustNotBeByRef(object p0, object p1)
        {
            return SR.Format(SR.VariableMustNotBeByRef, p0, p1);
        }

        /// <summary>
        /// A string like "Collection is read-only."
        /// </summary>
        internal static string CollectionReadOnly => SR.CollectionReadOnly;

        /// <summary>
        /// A string like "More than one key matching '{0}' was found in the ExpandoObject."
        /// </summary>
        internal static string AmbiguousMatchInExpandoObject(object p0) => SR.Format(SR.AmbiguousMatchInExpandoObject, p0);

        /// <summary>
        /// A string like "An element with the same key '{0}' already exists in the ExpandoObject."
        /// </summary>
        internal static string SameKeyExistsInExpando(object p0) => SR.Format(SR.SameKeyExistsInExpando, p0);

        /// <summary>
        /// A string like "The specified key '{0}' does not exist in the ExpandoObject."
        /// </summary>
        internal static string KeyDoesNotExistInExpando(object p0) => SR.Format(SR.KeyDoesNotExistInExpando, p0);

        /// <summary>
        /// A string like "Method precondition violated"
        /// </summary>
        internal static string MethodPreconditionViolated => SR.MethodPreconditionViolated;

        /// <summary>
        /// A string like "Argument count must be greater than number of named arguments."
        /// </summary>
        internal static string ArgCntMustBeGreaterThanNameCnt => SR.ArgCntMustBeGreaterThanNameCnt;

        /// <summary>
        /// A string like "An IDynamicMetaObjectProvider {0} created an invalid DynamicMetaObject instance."
        /// </summary>
        internal static string InvalidMetaObjectCreated(object p0) => SR.Format(SR.InvalidMetaObjectCreated, p0);

        /// <summary>
        /// A string like "The result type '{0}' of the binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static string BinderNotCompatibleWithCallSite(object p0, object p1, object p2) => SR.Format(SR.BinderNotCompatibleWithCallSite, p0, p1, p2);

        /// <summary>
        /// A string like "The result of the dynamic binding produced by the object with type '{0}' for the binder '{1}' needs at least one restriction."
        /// </summary>
        internal static string DynamicBindingNeedsRestrictions(object p0, object p1) => SR.Format(SR.DynamicBindingNeedsRestrictions, p0, p1);

        /// <summary>
        /// A string like "The result type '{0}' of the dynamic binding produced by the object with type '{1}' for the binder '{2}' is not compatible with the result type '{3}' expected by the call site."
        /// </summary>
        internal static string DynamicObjectResultNotAssignable(object p0, object p1, object p2, object p3) => SR.Format(SR.DynamicObjectResultNotAssignable, p0, p1, p2, p3);

        /// <summary>
        /// A string like "The result type '{0}' of the dynamic binding produced by binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static string DynamicBinderResultNotAssignable(object p0, object p1, object p2) => SR.Format(SR.DynamicBinderResultNotAssignable, p0, p1, p2);

        /// <summary>
        /// A string like "Bind cannot return null."
        /// </summary>
        internal static string BindingCannotBeNull => SR.BindingCannotBeNull;

        /// <summary>
        /// A string like "Found duplicate parameter '{0}'. Each ParameterExpression in the list must be a unique object."
        /// </summary>
        internal static string DuplicateVariable(object p0)
        {
            return SR.Format(SR.DuplicateVariable, p0);
        }

        /// <summary>
        /// A string like "Argument type cannot be void"
        /// </summary>
        internal static string ArgumentTypeCannotBeVoid => SR.ArgumentTypeCannotBeVoid;

        /// <summary>
        /// A string like "Type parameter is {0}. Expected a delegate."
        /// </summary>
        internal static string TypeParameterIsNotDelegate(object p0) => SR.Format(SR.TypeParameterIsNotDelegate, p0);

        /// <summary>
        /// A string like "No or Invalid rule produced"
        /// </summary>
        internal static string NoOrInvalidRuleProduced => SR.NoOrInvalidRuleProduced;

        /// <summary>
        /// A string like "Type must be derived from System.Delegate"
        /// </summary>
        internal static string TypeMustBeDerivedFromSystemDelegate => SR.TypeMustBeDerivedFromSystemDelegate;

        /// <summary>
        /// A string like "First argument of delegate must be CallSite"
        /// </summary>
        internal static string FirstArgumentMustBeCallSite => SR.FirstArgumentMustBeCallSite;

        /// <summary>
        /// A string like "Start and End must be well ordered"
        /// </summary>
        internal static string StartEndMustBeOrdered => SR.StartEndMustBeOrdered;

        /// <summary>
        /// A string like "fault cannot be used with catch or finally clauses"
        /// </summary>
        internal static string FaultCannotHaveCatchOrFinally => SR.FaultCannotHaveCatchOrFinally;

        /// <summary>
        /// A string like "try must have at least one catch, finally, or fault clause"
        /// </summary>
        internal static string TryMustHaveCatchFinallyOrFault => SR.TryMustHaveCatchFinallyOrFault;

        /// <summary>
        /// A string like "Body of catch must have the same type as body of try."
        /// </summary>
        internal static string BodyOfCatchMustHaveSameTypeAsBodyOfTry => SR.BodyOfCatchMustHaveSameTypeAsBodyOfTry;

        /// <summary>
        /// A string like "Extension node must override the property {0}."
        /// </summary>
        internal static string ExtensionNodeMustOverrideProperty(object p0)
        {
            return SR.Format(SR.ExtensionNodeMustOverrideProperty, p0);
        }

        /// <summary>
        /// A string like "User-defined operator method '{0}' must be static."
        /// </summary>
        internal static string UserDefinedOperatorMustBeStatic(object p0)
        {
            return SR.Format(SR.UserDefinedOperatorMustBeStatic, p0);
        }

        /// <summary>
        /// A string like "User-defined operator method '{0}' must not be void."
        /// </summary>
        internal static string UserDefinedOperatorMustNotBeVoid(object p0)
        {
            return SR.Format(SR.UserDefinedOperatorMustNotBeVoid, p0);
        }

        /// <summary>
        /// A string like "No coercion operator is defined between types '{0}' and '{1}'."
        /// </summary>
        internal static string CoercionOperatorNotDefined(object p0, object p1)
        {
            return SR.Format(SR.CoercionOperatorNotDefined, p0, p1);
        }

        /// <summary>
        /// A string like "The unary operator {0} is not defined for the type '{1}'."
        /// </summary>
        internal static string UnaryOperatorNotDefined(object p0, object p1)
        {
            return SR.Format(SR.UnaryOperatorNotDefined, p0, p1);
        }

        /// <summary>
        /// A string like "The binary operator {0} is not defined for the types '{1}' and '{2}'."
        /// </summary>
        internal static string BinaryOperatorNotDefined(object p0, object p1, object p2)
        {
            return SR.Format(SR.BinaryOperatorNotDefined, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Reference equality is not defined for the types '{0}' and '{1}'."
        /// </summary>
        internal static string ReferenceEqualityNotDefined(object p0, object p1)
        {
            return SR.Format(SR.ReferenceEqualityNotDefined, p0, p1);
        }

        /// <summary>
        /// A string like "The operands for operator '{0}' do not match the parameters of method '{1}'."
        /// </summary>
        internal static string OperandTypesDoNotMatchParameters(object p0, object p1)
        {
            return SR.Format(SR.OperandTypesDoNotMatchParameters, p0, p1);
        }

        /// <summary>
        /// A string like "The return type of overload method for operator '{0}' does not match the parameter type of conversion method '{1}'."
        /// </summary>
        internal static string OverloadOperatorTypeDoesNotMatchConversionType(object p0, object p1)
        {
            return SR.Format(SR.OverloadOperatorTypeDoesNotMatchConversionType, p0, p1);
        }

        /// <summary>
        /// A string like "Conversion is not supported for arithmetic types without operator overloading."
        /// </summary>
        internal static string ConversionIsNotSupportedForArithmeticTypes => SR.ConversionIsNotSupportedForArithmeticTypes;

        /// <summary>
        /// A string like "Argument must be array"
        /// </summary>
        internal static string ArgumentMustBeArray => SR.ArgumentMustBeArray;

        /// <summary>
        /// A string like "Argument must be boolean"
        /// </summary>
        internal static string ArgumentMustBeBoolean => SR.ArgumentMustBeBoolean;

        /// <summary>
        /// A string like "The user-defined equality method '{0}' must return a boolean value."
        /// </summary>
        internal static string EqualityMustReturnBoolean(object p0)
        {
            return SR.Format(SR.EqualityMustReturnBoolean, p0);
        }

        /// <summary>
        /// A string like "Argument must be either a FieldInfo or PropertyInfo"
        /// </summary>
        internal static string ArgumentMustBeFieldInfoOrPropertyInfo => SR.ArgumentMustBeFieldInfoOrPropertyInfo;

        /// <summary>
        /// A string like "Argument must be either a FieldInfo, PropertyInfo or MethodInfo"
        /// </summary>
        internal static string ArgumentMustBeFieldInfoOrPropertyInfoOrMethod => SR.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod;

        /// <summary>
        /// A string like "Argument must be an instance member"
        /// </summary>
        internal static string ArgumentMustBeInstanceMember => SR.ArgumentMustBeInstanceMember;

        /// <summary>
        /// A string like "Argument must be of an integer type"
        /// </summary>
        internal static string ArgumentMustBeInteger => SR.ArgumentMustBeInteger;

        /// <summary>
        /// A string like "Argument for array index must be of type Int32"
        /// </summary>
        internal static string ArgumentMustBeArrayIndexType => SR.ArgumentMustBeArrayIndexType;

        /// <summary>
        /// A string like "Argument must be single dimensional array type"
        /// </summary>
        internal static string ArgumentMustBeSingleDimensionalArrayType => SR.ArgumentMustBeSingleDimensionalArrayType;

        /// <summary>
        /// A string like "Argument types do not match"
        /// </summary>
        internal static string ArgumentTypesMustMatch => SR.ArgumentTypesMustMatch;

        /// <summary>
        /// A string like "Cannot auto initialize elements of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static string CannotAutoInitializeValueTypeElementThroughProperty(object p0)
        {
            return SR.Format(SR.CannotAutoInitializeValueTypeElementThroughProperty, p0);
        }

        /// <summary>
        /// A string like "Cannot auto initialize members of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static string CannotAutoInitializeValueTypeMemberThroughProperty(object p0)
        {
            return SR.Format(SR.CannotAutoInitializeValueTypeMemberThroughProperty, p0);
        }

        /// <summary>
        /// A string like "The type used in TypeAs Expression must be of reference or nullable type, {0} is neither"
        /// </summary>
        internal static string IncorrectTypeForTypeAs(object p0)
        {
            return SR.Format(SR.IncorrectTypeForTypeAs, p0);
        }

        /// <summary>
        /// A string like "Coalesce used with type that cannot be null"
        /// </summary>
        internal static string CoalesceUsedOnNonNullType => SR.CoalesceUsedOnNonNullType;

        /// <summary>
        /// A string like "An expression of type '{0}' cannot be used to initialize an array of type '{1}'"
        /// </summary>
        internal static string ExpressionTypeCannotInitializeArrayType(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeCannotInitializeArrayType, p0, p1);
        }

        /// <summary>
        /// A string like " Argument type '{0}' does not match the corresponding member type '{1}'"
        /// </summary>
        internal static string ArgumentTypeDoesNotMatchMember(object p0, object p1)
        {
            return SR.Format(SR.ArgumentTypeDoesNotMatchMember, p0, p1);
        }

        /// <summary>
        /// A string like " The member '{0}' is not declared on type '{1}' being created"
        /// </summary>
        internal static string ArgumentMemberNotDeclOnType(object p0, object p1)
        {
            return SR.Format(SR.ArgumentMemberNotDeclOnType, p0, p1);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for return type '{1}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchReturn(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchReturn, p0, p1);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for assignment to type '{1}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchAssignment(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchAssignment, p0, p1);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for label of type '{1}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchLabel(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchLabel, p0, p1);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be invoked"
        /// </summary>
        internal static string ExpressionTypeNotInvocable(object p0)
        {
            return SR.Format(SR.ExpressionTypeNotInvocable, p0);
        }

        /// <summary>
        /// A string like "Field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static string FieldNotDefinedForType(object p0, object p1)
        {
            return SR.Format(SR.FieldNotDefinedForType, p0, p1);
        }

        /// <summary>
        /// A string like "Instance field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static string InstanceFieldNotDefinedForType(object p0, object p1)
        {
            return SR.Format(SR.InstanceFieldNotDefinedForType, p0, p1);
        }

        /// <summary>
        /// A string like "Field '{0}.{1}' is not defined for type '{2}'"
        /// </summary>
        internal static string FieldInfoNotDefinedForType(object p0, object p1, object p2)
        {
            return SR.Format(SR.FieldInfoNotDefinedForType, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Incorrect number of indexes"
        /// </summary>
        internal static string IncorrectNumberOfIndexes => SR.IncorrectNumberOfIndexes;

        /// <summary>
        /// A string like "Incorrect number of parameters supplied for lambda declaration"
        /// </summary>
        internal static string IncorrectNumberOfLambdaDeclarationParameters => SR.IncorrectNumberOfLambdaDeclarationParameters;

        /// <summary>
        /// A string like " Incorrect number of members for constructor"
        /// </summary>
        internal static string IncorrectNumberOfMembersForGivenConstructor => SR.IncorrectNumberOfMembersForGivenConstructor;

        /// <summary>
        /// A string like "Incorrect number of arguments for the given members "
        /// </summary>
        internal static string IncorrectNumberOfArgumentsForMembers => SR.IncorrectNumberOfArgumentsForMembers;

        /// <summary>
        /// A string like "Lambda type parameter must be derived from System.MulticastDelegate"
        /// </summary>
        internal static string LambdaTypeMustBeDerivedFromSystemDelegate => SR.LambdaTypeMustBeDerivedFromSystemDelegate;

        /// <summary>
        /// A string like "Member '{0}' not field or property"
        /// </summary>
        internal static string MemberNotFieldOrProperty(object p0)
        {
            return SR.Format(SR.MemberNotFieldOrProperty, p0);
        }

        /// <summary>
        /// A string like "Method {0} contains generic parameters"
        /// </summary>
        internal static string MethodContainsGenericParameters(object p0)
        {
            return SR.Format(SR.MethodContainsGenericParameters, p0);
        }

        /// <summary>
        /// A string like "Method {0} is a generic method definition"
        /// </summary>
        internal static string MethodIsGeneric(object p0)
        {
            return SR.Format(SR.MethodIsGeneric, p0);
        }

        /// <summary>
        /// A string like "The method '{0}.{1}' is not a property accessor"
        /// </summary>
        internal static string MethodNotPropertyAccessor(object p0, object p1)
        {
            return SR.Format(SR.MethodNotPropertyAccessor, p0, p1);
        }

        /// <summary>
        /// A string like "The property '{0}' has no 'get' accessor"
        /// </summary>
        internal static string PropertyDoesNotHaveGetter(object p0)
        {
            return SR.Format(SR.PropertyDoesNotHaveGetter, p0);
        }

        /// <summary>
        /// A string like "The property '{0}' has no 'set' accessor"
        /// </summary>
        internal static string PropertyDoesNotHaveSetter(object p0)
        {
            return SR.Format(SR.PropertyDoesNotHaveSetter, p0);
        }

        /// <summary>
        /// A string like "The property '{0}' has no 'get' or 'set' accessors"
        /// </summary>
        internal static string PropertyDoesNotHaveAccessor(object p0)
        {
            return SR.Format(SR.PropertyDoesNotHaveAccessor, p0);
        }

        /// <summary>
        /// A string like "'{0}' is not a member of type '{1}'"
        /// </summary>
        internal static string NotAMemberOfType(object p0, object p1)
        {
            return SR.Format(SR.NotAMemberOfType, p0, p1);
        }

        /// <summary>
        /// A string like "The expression '{0}' is not supported for type '{1}'"
        /// </summary>
        internal static string ExpressionNotSupportedForType(object p0, object p1)
        {
            return SR.Format(SR.ExpressionNotSupportedForType, p0, p1);
        }

        /// <summary>
        /// A string like "The expression '{0}' is not supported for nullable type '{1}'"
        /// </summary>
        internal static string ExpressionNotSupportedForNullableType(object p0, object p1)
        {
            return SR.Format(SR.ExpressionNotSupportedForNullableType, p0, p1);
        }


        /// <summary>
        /// A string like "ParameterExpression of type '{0}' cannot be used for delegate parameter of type '{1}'"
        /// </summary>
        internal static string ParameterExpressionNotValidAsDelegate(object p0, object p1)
        {
            return SR.Format(SR.ParameterExpressionNotValidAsDelegate, p0, p1);
        }

        /// <summary>
        /// A string like "Property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static string PropertyNotDefinedForType(object p0, object p1)
        {
            return SR.Format(SR.PropertyNotDefinedForType, p0, p1);
        }

        /// <summary>
        /// A string like "Instance property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static string InstancePropertyNotDefinedForType(object p0, object p1)
        {
            return SR.Format(SR.InstancePropertyNotDefinedForType, p0, p1);
        }

        /// <summary>
        /// A string like "Instance property '{0}' that takes no argument is not defined for type '{1}'"
        /// </summary>
        internal static string InstancePropertyWithoutParameterNotDefinedForType(object p0, object p1)
        {
            return SR.Format(SR.InstancePropertyWithoutParameterNotDefinedForType, p0, p1);
        }

        /// <summary>
        /// A string like "Instance property '{0}{1}' is not defined for type '{2}'"
        /// </summary>
        internal static string InstancePropertyWithSpecifiedParametersNotDefinedForType(object p0, object p1, object p2)
        {
            return SR.Format(SR.InstancePropertyWithSpecifiedParametersNotDefinedForType, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Method '{0}' declared on type '{1}' cannot be called with instance of type '{2}'"
        /// </summary>
        internal static string InstanceAndMethodTypeMismatch(object p0, object p1, object p2)
        {
            return SR.Format(SR.InstanceAndMethodTypeMismatch, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Type '{0}' does not have a default constructor"
        /// </summary>
        internal static string TypeMissingDefaultConstructor(object p0)
        {
            return SR.Format(SR.TypeMissingDefaultConstructor, p0);
        }

        /// <summary>
        /// A string like "Element initializer method must be named 'Add'"
        /// </summary>
        internal static string ElementInitializerMethodNotAdd => SR.ElementInitializerMethodNotAdd;

        /// <summary>
        /// A string like "Parameter '{0}' of element initializer method '{1}' must not be a pass by reference parameter"
        /// </summary>
        internal static string ElementInitializerMethodNoRefOutParam(object p0, object p1)
        {
            return SR.Format(SR.ElementInitializerMethodNoRefOutParam, p0, p1);
        }

        /// <summary>
        /// A string like "Element initializer method must have at least 1 parameter"
        /// </summary>
        internal static string ElementInitializerMethodWithZeroArgs => SR.ElementInitializerMethodWithZeroArgs;

        /// <summary>
        /// A string like "Element initializer method must be an instance method"
        /// </summary>
        internal static string ElementInitializerMethodStatic => SR.ElementInitializerMethodStatic;

        /// <summary>
        /// A string like "Type '{0}' is not IEnumerable"
        /// </summary>
        internal static string TypeNotIEnumerable(object p0)
        {
            return SR.Format(SR.TypeNotIEnumerable, p0);
        }

        /// <summary>
        /// A string like "Unexpected coalesce operator."
        /// </summary>
        internal static string UnexpectedCoalesceOperator => SR.UnexpectedCoalesceOperator;

        /// <summary>
        /// A string like "Cannot cast from type '{0}' to type '{1}"
        /// </summary>
        internal static string InvalidCast(object p0, object p1)
        {
            return SR.Format(SR.InvalidCast, p0, p1);
        }

        /// <summary>
        /// A string like "Unhandled binary: {0}"
        /// </summary>
        internal static string UnhandledBinary(object p0)
        {
            return SR.Format(SR.UnhandledBinary, p0);
        }

        /// <summary>
        /// A string like "Unhandled binding "
        /// </summary>
        internal static string UnhandledBinding => SR.UnhandledBinding;

        /// <summary>
        /// A string like "Unhandled Binding Type: {0}"
        /// </summary>
        internal static string UnhandledBindingType(object p0)
        {
            return SR.Format(SR.UnhandledBindingType, p0);
        }

        /// <summary>
        /// A string like "Unhandled convert: {0}"
        /// </summary>
        internal static string UnhandledConvert(object p0)
        {
            return SR.Format(SR.UnhandledConvert, p0);
        }

        /// <summary>
        /// A string like "Unhandled unary: {0}"
        /// </summary>
        internal static string UnhandledUnary(object p0)
        {
            return SR.Format(SR.UnhandledUnary, p0);
        }

        /// <summary>
        /// A string like "Unknown binding type"
        /// </summary>
        internal static string UnknownBindingType => SR.UnknownBindingType;

        /// <summary>
        /// A string like "The user-defined operator method '{1}' for operator '{0}' must have identical parameter and return types."
        /// </summary>
        internal static string UserDefinedOpMustHaveConsistentTypes(object p0, object p1)
        {
            return SR.Format(SR.UserDefinedOpMustHaveConsistentTypes, p0, p1);
        }

        /// <summary>
        /// A string like "The user-defined operator method '{1}' for operator '{0}' must return the same type as its parameter or a derived type."
        /// </summary>
        internal static string UserDefinedOpMustHaveValidReturnType(object p0, object p1)
        {
            return SR.Format(SR.UserDefinedOpMustHaveValidReturnType, p0, p1);
        }

        /// <summary>
        /// A string like "The user-defined operator method '{1}' for operator '{0}' must have associated boolean True and False operators."
        /// </summary>
        internal static string LogicalOperatorMustHaveBooleanOperators(object p0, object p1)
        {
            return SR.Format(SR.LogicalOperatorMustHaveBooleanOperators, p0, p1);
        }

        /// <summary>
        /// A string like "No method '{0}' exists on type '{1}'."
        /// </summary>
        internal static string MethodDoesNotExistOnType(object p0, object p1)
        {
            return SR.Format(SR.MethodDoesNotExistOnType, p0, p1);
        }

        /// <summary>
        /// A string like "No method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static string MethodWithArgsDoesNotExistOnType(object p0, object p1)
        {
            return SR.Format(SR.MethodWithArgsDoesNotExistOnType, p0, p1);
        }

        /// <summary>
        /// A string like "No generic method '{0}' on type '{1}' is compatible with the supplied type arguments and arguments. No type arguments should be provided if the method is non-generic. "
        /// </summary>
        internal static string GenericMethodWithArgsDoesNotExistOnType(object p0, object p1)
        {
            return SR.Format(SR.GenericMethodWithArgsDoesNotExistOnType, p0, p1);
        }

        /// <summary>
        /// A string like "More than one method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static string MethodWithMoreThanOneMatch(object p0, object p1)
        {
            return SR.Format(SR.MethodWithMoreThanOneMatch, p0, p1);
        }

        /// <summary>
        /// A string like "More than one property '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static string PropertyWithMoreThanOneMatch(object p0, object p1)
        {
            return SR.Format(SR.PropertyWithMoreThanOneMatch, p0, p1);
        }

        /// <summary>
        /// A string like "An incorrect number of type args were specified for the declaration of a Func type."
        /// </summary>
        internal static string IncorrectNumberOfTypeArgsForFunc => SR.IncorrectNumberOfTypeArgsForFunc;

        /// <summary>
        /// A string like "An incorrect number of type args were specified for the declaration of an Action type."
        /// </summary>
        internal static string IncorrectNumberOfTypeArgsForAction => SR.IncorrectNumberOfTypeArgsForAction;

        /// <summary>
        /// A string like "Argument type cannot be System.Void."
        /// </summary>
        internal static string ArgumentCannotBeOfTypeVoid => SR.ArgumentCannotBeOfTypeVoid;

        /// <summary>
        /// A string like "{0} must be greater than or equal to {1}"
        /// </summary>
        internal static string OutOfRange(object p0, object p1)
        {
            return SR.Format(SR.OutOfRange, p0, p1);
        }

        /// <summary>
        /// A string like "Cannot redefine label '{0}' in an inner block."
        /// </summary>
        internal static string LabelTargetAlreadyDefined(object p0)
        {
            return SR.Format(SR.LabelTargetAlreadyDefined, p0);
        }

        /// <summary>
        /// A string like "Cannot jump to undefined label '{0}'."
        /// </summary>
        internal static string LabelTargetUndefined(object p0)
        {
            return SR.Format(SR.LabelTargetUndefined, p0);
        }

        /// <summary>
        /// A string like "Control cannot leave a finally block."
        /// </summary>
        internal static string ControlCannotLeaveFinally => SR.ControlCannotLeaveFinally;

        /// <summary>
        /// A string like "Control cannot leave a filter test."
        /// </summary>
        internal static string ControlCannotLeaveFilterTest => SR.ControlCannotLeaveFilterTest;

        /// <summary>
        /// A string like "Cannot jump to ambiguous label '{0}'."
        /// </summary>
        internal static string AmbiguousJump(object p0)
        {
            return SR.Format(SR.AmbiguousJump, p0);
        }

        /// <summary>
        /// A string like "Control cannot enter a try block."
        /// </summary>
        internal static string ControlCannotEnterTry => SR.ControlCannotEnterTry;

        /// <summary>
        /// A string like "Control cannot enter an expression--only statements can be jumped into."
        /// </summary>
        internal static string ControlCannotEnterExpression => SR.ControlCannotEnterExpression;

        /// <summary>
        /// A string like "Cannot jump to non-local label '{0}' with a value. Only jumps to labels defined in outer blocks can pass values."
        /// </summary>
        internal static string NonLocalJumpWithValue(object p0)
        {
            return SR.Format(SR.NonLocalJumpWithValue, p0);
        }

        /// <summary>
        /// A string like "Extension should have been reduced."
        /// </summary>
        internal static string ExtensionNotReduced => SR.ExtensionNotReduced;

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// A string like "CompileToMethod cannot compile constant '{0}' because it is a non-trivial value, such as a live object. Instead, create an expression tree that can construct this value."
        /// </summary>
        internal static string CannotCompileConstant(object p0)
        {
            return SR.Format(SR.CannotCompileConstant, p0);
        }

        /// <summary>
        /// A string like "Dynamic expressions are not supported by CompileToMethod. Instead, create an expression tree that uses System.Runtime.CompilerServices.CallSite."
        /// </summary>
        internal static string CannotCompileDynamic => SR.CannotCompileDynamic;

        /// <summary>
        /// A string like "MethodBuilder does not have a valid TypeBuilder"
        /// </summary>
        internal static string MethodBuilderDoesNotHaveTypeBuilder => SR.MethodBuilderDoesNotHaveTypeBuilder;
#endif

        /// <summary>
        /// A string like "Invalid lvalue for assignment: {0}."
        /// </summary>
        internal static string InvalidLvalue(object p0)
        {
            return SR.Format(SR.InvalidLvalue, p0);
        }

        /// <summary>
        /// A string like "unknown lift type: '{0}'."
        /// </summary>
        internal static string UnknownLiftType(object p0)
        {
            return SR.Format(SR.UnknownLiftType, p0);
        }

        /// <summary>
        /// A string like "Cannot create instance of {0} because it contains generic parameters"
        /// </summary>
        internal static string IllegalNewGenericParams(object p0)
        {
            return SR.Format(SR.IllegalNewGenericParams, p0);
        }

        /// <summary>
        /// A string like "variable '{0}' of type '{1}' referenced from scope '{2}', but it is not defined"
        /// </summary>
        internal static string UndefinedVariable(object p0, object p1, object p2)
        {
            return SR.Format(SR.UndefinedVariable, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Cannot close over byref parameter '{0}' referenced in lambda '{1}'"
        /// </summary>
        internal static string CannotCloseOverByRef(object p0, object p1)
        {
            return SR.Format(SR.CannotCloseOverByRef, p0, p1);
        }

        /// <summary>
        /// A string like "Unexpected VarArgs call to method '{0}'"
        /// </summary>
        internal static string UnexpectedVarArgsCall(object p0)
        {
            return SR.Format(SR.UnexpectedVarArgsCall, p0);
        }

        /// <summary>
        /// A string like "Rethrow statement is valid only inside a Catch block."
        /// </summary>
        internal static string RethrowRequiresCatch => SR.RethrowRequiresCatch;

        /// <summary>
        /// A string like "Try expression is not allowed inside a filter body."
        /// </summary>
        internal static string TryNotAllowedInFilter => SR.TryNotAllowedInFilter;

        /// <summary>
        /// A string like "When called from '{0}', rewriting a node of type '{1}' must return a non-null value of the same type. Alternatively, override '{2}' and change it to not visit children of this type."
        /// </summary>
        internal static string MustRewriteToSameNode(object p0, object p1, object p2)
        {
            return SR.Format(SR.MustRewriteToSameNode, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Rewriting child expression from type '{0}' to type '{1}' is not allowed, because it would change the meaning of the operation. If this is intentional, override '{2}' and change it to allow this rewrite."
        /// </summary>
        internal static string MustRewriteChildToSameType(object p0, object p1, object p2)
        {
            return SR.Format(SR.MustRewriteChildToSameType, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Rewritten expression calls operator method '{0}', but the original node had no operator method. If this is intentional, override '{1}' and change it to allow this rewrite."
        /// </summary>
        internal static string MustRewriteWithoutMethod(object p0, object p1)
        {
            return SR.Format(SR.MustRewriteWithoutMethod, p0, p1);
        }

        /// <summary>
        /// A string like "TryExpression is not supported as an argument to method '{0}' because it has an argument with by-ref type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static string TryNotSupportedForMethodsWithRefArgs(object p0)
        {
            return SR.Format(SR.TryNotSupportedForMethodsWithRefArgs, p0);
        }

        /// <summary>
        /// A string like "TryExpression is not supported as a child expression when accessing a member on type '{0}' because it is a value type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static string TryNotSupportedForValueTypeInstances(object p0)
        {
            return SR.Format(SR.TryNotSupportedForValueTypeInstances, p0);
        }

        /// <summary>
        /// A string like "Test value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static string TestValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
        {
            return SR.Format(SR.TestValueTypeDoesNotMatchComparisonMethodParameter, p0, p1);
        }

        /// <summary>
        /// A string like "Switch value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static string SwitchValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
        {
            return SR.Format(SR.SwitchValueTypeDoesNotMatchComparisonMethodParameter, p0, p1);
        }

#if FEATURE_COMPILE_TO_METHODBUILDER && FEATURE_PDB_GENERATOR
        /// <summary>
        /// A string like "DebugInfoGenerator created by CreatePdbGenerator can only be used with LambdaExpression.CompileToMethod."
        /// </summary>
        internal static string PdbGeneratorNeedsExpressionCompiler => SR.PdbGeneratorNeedsExpressionCompiler;
#endif

#if FEATURE_COMPILE
        /// <summary>
        /// A string like "The operator '{0}' is not implemented for type '{1}'"
        /// </summary>
        internal static string OperatorNotImplementedForType(object p0, object p1)
        {
            return SR.Format(SR.OperatorNotImplementedForType, p0, p1);
        }
#endif

        /// <summary>
        /// A string like "The constructor should not be static"
        /// </summary>
        internal static string NonStaticConstructorRequired => SR.NonStaticConstructorRequired;

        /// <summary>
        /// A string like "The constructor should not be declared on an abstract class"
        /// </summary>
        internal static string NonAbstractConstructorRequired => SR.NonAbstractConstructorRequired;
    }
}
