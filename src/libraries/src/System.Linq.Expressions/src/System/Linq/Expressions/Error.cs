// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Linq.Expressions
{
    /// <summary>
    ///    Strongly-typed and parameterized exception factory.
    /// </summary>
    internal static partial class Error
    {
        /// <summary>
        /// ArgumentException with message like "reducible nodes must override Expression.Reduce()"
        /// </summary>
        internal static Exception ReducibleMustOverrideReduce()
        {
            return new ArgumentException(Strings.ReducibleMustOverrideReduce);
        }
        /// <summary>
        /// ArgumentException with message like "node cannot reduce to itself or null"
        /// </summary>
        internal static Exception MustReduceToDifferent()
        {
            return new ArgumentException(Strings.MustReduceToDifferent);
        }
        /// <summary>
        /// ArgumentException with message like "cannot assign from the reduced node type to the original node type"
        /// </summary>
        internal static Exception ReducedNotCompatible()
        {
            return new ArgumentException(Strings.ReducedNotCompatible);
        }
        /// <summary>
        /// ArgumentException with message like "Setter must have parameters."
        /// </summary>
        internal static Exception SetterHasNoParams()
        {
            return new ArgumentException(Strings.SetterHasNoParams);
        }
        /// <summary>
        /// ArgumentException with message like "Property cannot have a managed pointer type."
        /// </summary>
        internal static Exception PropertyCannotHaveRefType()
        {
            return new ArgumentException(Strings.PropertyCannotHaveRefType);
        }
        /// <summary>
        /// ArgumentException with message like "Indexing parameters of getter and setter must match."
        /// </summary>
        internal static Exception IndexesOfSetGetMustMatch()
        {
            return new ArgumentException(Strings.IndexesOfSetGetMustMatch);
        }
        /// <summary>
        /// ArgumentException with message like "Accessor method should not have VarArgs."
        /// </summary>
        internal static Exception AccessorsCannotHaveVarArgs()
        {
            return new ArgumentException(Strings.AccessorsCannotHaveVarArgs);
        }
        /// <summary>
        /// ArgumentException with message like "Accessor indexes cannot be passed ByRef."
        /// </summary>
        internal static Exception AccessorsCannotHaveByRefArgs()
        {
            return new ArgumentException(Strings.AccessorsCannotHaveByRefArgs);
        }
        /// <summary>
        /// ArgumentException with message like "Bounds count cannot be less than 1"
        /// </summary>
        internal static Exception BoundsCannotBeLessThanOne()
        {
            return new ArgumentException(Strings.BoundsCannotBeLessThanOne);
        }
        /// <summary>
        /// ArgumentException with message like "type must not be ByRef"
        /// </summary>
        internal static Exception TypeMustNotBeByRef()
        {
            return new ArgumentException(Strings.TypeMustNotBeByRef);
        }
        /// <summary>
        /// ArgumentException with message like "Type doesn't have constructor with a given signature"
        /// </summary>
        internal static Exception TypeDoesNotHaveConstructorForTheSignature()
        {
            return new ArgumentException(Strings.TypeDoesNotHaveConstructorForTheSignature);
        }
        /// <summary>
        /// ArgumentException with message like "Setter should have void type."
        /// </summary>
        internal static Exception SetterMustBeVoid()
        {
            return new ArgumentException(Strings.SetterMustBeVoid);
        }
        /// <summary>
        /// ArgumentException with message like "Property type must match the value type of setter"
        /// </summary>
        internal static Exception PropertyTyepMustMatchSetter()
        {
            return new ArgumentException(Strings.PropertyTyepMustMatchSetter);
        }
        /// <summary>
        /// ArgumentException with message like "Both accessors must be static."
        /// </summary>
        internal static Exception BothAccessorsMustBeStatic()
        {
            return new ArgumentException(Strings.BothAccessorsMustBeStatic);
        }
        /// <summary>
        /// ArgumentException with message like "Static method requires null instance, non-static method requires non-null instance."
        /// </summary>
        internal static Exception OnlyStaticMethodsHaveNullInstance()
        {
            return new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance);
        }
        /// <summary>
        /// ArgumentException with message like "Property cannot have a void type."
        /// </summary>
        internal static Exception PropertyTypeCannotBeVoid()
        {
            return new ArgumentException(Strings.PropertyTypeCannotBeVoid);
        }
        /// <summary>
        /// ArgumentException with message like "Can only unbox from an object or interface type to a value type."
        /// </summary>
        internal static Exception InvalidUnboxType()
        {
            return new ArgumentException(Strings.InvalidUnboxType);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must not have a value type."
        /// </summary>
        internal static Exception ArgumentMustNotHaveValueType()
        {
            return new ArgumentException(Strings.ArgumentMustNotHaveValueType);
        }
        /// <summary>
        /// ArgumentException with message like "must be reducible node"
        /// </summary>
        internal static Exception MustBeReducible()
        {
            return new ArgumentException(Strings.MustBeReducible);
        }
        /// <summary>
        /// ArgumentException with message like "Default body must be supplied if case bodies are not System.Void."
        /// </summary>
        internal static Exception DefaultBodyMustBeSupplied()
        {
            return new ArgumentException(Strings.DefaultBodyMustBeSupplied);
        }
        /// <summary>
        /// ArgumentException with message like "MethodBuilder does not have a valid TypeBuilder"
        /// </summary>
        internal static Exception MethodBuilderDoesNotHaveTypeBuilder()
        {
            return new ArgumentException(Strings.MethodBuilderDoesNotHaveTypeBuilder);
        }
        /// <summary>
        /// ArgumentException with message like "Label type must be System.Void if an expression is not supplied"
        /// </summary>
        internal static Exception LabelMustBeVoidOrHaveExpression()
        {
            return new ArgumentException(Strings.LabelMustBeVoidOrHaveExpression);
        }
        /// <summary>
        /// ArgumentException with message like "Type must be System.Void for this label argument"
        /// </summary>
        internal static Exception LabelTypeMustBeVoid()
        {
            return new ArgumentException(Strings.LabelTypeMustBeVoid);
        }
        /// <summary>
        /// ArgumentException with message like "Quoted expression must be a lambda"
        /// </summary>
        internal static Exception QuotedExpressionMustBeLambda()
        {
            return new ArgumentException(Strings.QuotedExpressionMustBeLambda);
        }
        /// <summary>
        /// ArgumentException with message like "Variable '{0}' uses unsupported type '{1}'. Reference types are not supported for variables."
        /// </summary>
        internal static Exception VariableMustNotBeByRef(object p0, object p1)
        {
            return new ArgumentException(Strings.VariableMustNotBeByRef(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Found duplicate parameter '{0}'. Each ParameterExpression in the list must be a unique object."
        /// </summary>
        internal static Exception DuplicateVariable(object p0)
        {
            return new ArgumentException(Strings.DuplicateVariable(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Start and End must be well ordered"
        /// </summary>
        internal static Exception StartEndMustBeOrdered()
        {
            return new ArgumentException(Strings.StartEndMustBeOrdered);
        }
        /// <summary>
        /// ArgumentException with message like "fault cannot be used with catch or finally clauses"
        /// </summary>
        internal static Exception FaultCannotHaveCatchOrFinally()
        {
            return new ArgumentException(Strings.FaultCannotHaveCatchOrFinally);
        }
        /// <summary>
        /// ArgumentException with message like "try must have at least one catch, finally, or fault clause"
        /// </summary>
        internal static Exception TryMustHaveCatchFinallyOrFault()
        {
            return new ArgumentException(Strings.TryMustHaveCatchFinallyOrFault);
        }
        /// <summary>
        /// ArgumentException with message like "Body of catch must have the same type as body of try."
        /// </summary>
        internal static Exception BodyOfCatchMustHaveSameTypeAsBodyOfTry()
        {
            return new ArgumentException(Strings.BodyOfCatchMustHaveSameTypeAsBodyOfTry);
        }
        /// <summary>
        /// InvalidOperationException with message like "Extension node must override the property {0}."
        /// </summary>
        internal static Exception ExtensionNodeMustOverrideProperty(object p0)
        {
            return new InvalidOperationException(Strings.ExtensionNodeMustOverrideProperty(p0));
        }
        /// <summary>
        /// ArgumentException with message like "User-defined operator method '{0}' must be static."
        /// </summary>
        internal static Exception UserDefinedOperatorMustBeStatic(object p0)
        {
            return new ArgumentException(Strings.UserDefinedOperatorMustBeStatic(p0));
        }
        /// <summary>
        /// ArgumentException with message like "User-defined operator method '{0}' must not be void."
        /// </summary>
        internal static Exception UserDefinedOperatorMustNotBeVoid(object p0)
        {
            return new ArgumentException(Strings.UserDefinedOperatorMustNotBeVoid(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "No coercion operator is defined between types '{0}' and '{1}'."
        /// </summary>
        internal static Exception CoercionOperatorNotDefined(object p0, object p1)
        {
            return new InvalidOperationException(Strings.CoercionOperatorNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The unary operator {0} is not defined for the type '{1}'."
        /// </summary>
        internal static Exception UnaryOperatorNotDefined(object p0, object p1)
        {
            return new InvalidOperationException(Strings.UnaryOperatorNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The binary operator {0} is not defined for the types '{1}' and '{2}'."
        /// </summary>
        internal static Exception BinaryOperatorNotDefined(object p0, object p1, object p2)
        {
            return new InvalidOperationException(Strings.BinaryOperatorNotDefined(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Reference equality is not defined for the types '{0}' and '{1}'."
        /// </summary>
        internal static Exception ReferenceEqualityNotDefined(object p0, object p1)
        {
            return new InvalidOperationException(Strings.ReferenceEqualityNotDefined(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The operands for operator '{0}' do not match the parameters of method '{1}'."
        /// </summary>
        internal static Exception OperandTypesDoNotMatchParameters(object p0, object p1)
        {
            return new InvalidOperationException(Strings.OperandTypesDoNotMatchParameters(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The return type of overload method for operator '{0}' does not match the parameter type of conversion method '{1}'."
        /// </summary>
        internal static Exception OverloadOperatorTypeDoesNotMatchConversionType(object p0, object p1)
        {
            return new InvalidOperationException(Strings.OverloadOperatorTypeDoesNotMatchConversionType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Conversion is not supported for arithmetic types without operator overloading."
        /// </summary>
        internal static Exception ConversionIsNotSupportedForArithmeticTypes()
        {
            return new InvalidOperationException(Strings.ConversionIsNotSupportedForArithmeticTypes);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be array"
        /// </summary>
        internal static Exception ArgumentMustBeArray()
        {
            return new ArgumentException(Strings.ArgumentMustBeArray);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be boolean"
        /// </summary>
        internal static Exception ArgumentMustBeBoolean()
        {
            return new ArgumentException(Strings.ArgumentMustBeBoolean);
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined equality method '{0}' must return a boolean value."
        /// </summary>
        internal static Exception EqualityMustReturnBoolean(object p0)
        {
            return new ArgumentException(Strings.EqualityMustReturnBoolean(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be either a FieldInfo or PropertyInfo"
        /// </summary>
        internal static Exception ArgumentMustBeFieldInfoOrPropertyInfo()
        {
            return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfo);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be either a FieldInfo, PropertyInfo or MethodInfo"
        /// </summary>
        internal static Exception ArgumentMustBeFieldInfoOrPropertyInfoOrMethod()
        {
            return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be an instance member"
        /// </summary>
        internal static Exception ArgumentMustBeInstanceMember()
        {
            return new ArgumentException(Strings.ArgumentMustBeInstanceMember);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be of an integer type"
        /// </summary>
        internal static Exception ArgumentMustBeInteger()
        {
            return new ArgumentException(Strings.ArgumentMustBeInteger);
        }
        /// <summary>
        /// ArgumentException with message like "Argument for array index must be of type Int32"
        /// </summary>
        internal static Exception ArgumentMustBeArrayIndexType()
        {
            return new ArgumentException(Strings.ArgumentMustBeArrayIndexType);
        }
        /// <summary>
        /// ArgumentException with message like "Argument must be single dimensional array type"
        /// </summary>
        internal static Exception ArgumentMustBeSingleDimensionalArrayType()
        {
            return new ArgumentException(Strings.ArgumentMustBeSingleDimensionalArrayType);
        }
        /// <summary>
        /// ArgumentException with message like "Argument types do not match"
        /// </summary>
        internal static Exception ArgumentTypesMustMatch()
        {
            return new ArgumentException(Strings.ArgumentTypesMustMatch);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot auto initialize elements of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static Exception CannotAutoInitializeValueTypeElementThroughProperty(object p0)
        {
            return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeElementThroughProperty(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot auto initialize members of value type through property '{0}', use assignment instead"
        /// </summary>
        internal static Exception CannotAutoInitializeValueTypeMemberThroughProperty(object p0)
        {
            return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeMemberThroughProperty(p0));
        }
        /// <summary>
        /// ArgumentException with message like "The type used in TypeAs Expression must be of reference or nullable type, {0} is neither"
        /// </summary>
        internal static Exception IncorrectTypeForTypeAs(object p0)
        {
            return new ArgumentException(Strings.IncorrectTypeForTypeAs(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Coalesce used with type that cannot be null"
        /// </summary>
        internal static Exception CoalesceUsedOnNonNullType()
        {
            return new InvalidOperationException(Strings.CoalesceUsedOnNonNullType);
        }
        /// <summary>
        /// InvalidOperationException with message like "An expression of type '{0}' cannot be used to initialize an array of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeCannotInitializeArrayType(object p0, object p1)
        {
            return new InvalidOperationException(Strings.ExpressionTypeCannotInitializeArrayType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1)
        {
            return Dynamic.Utils.Error.ExpressionTypeDoesNotMatchConstructorParameter(p0, p1);
        }
        /// <summary>
        /// ArgumentException with message like " Argument type '{0}' does not match the corresponding member type '{1}'"
        /// </summary>
        internal static Exception ArgumentTypeDoesNotMatchMember(object p0, object p1)
        {
            return new ArgumentException(Strings.ArgumentTypeDoesNotMatchMember(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like " The member '{0}' is not declared on type '{1}' being created"
        /// </summary>
        internal static Exception ArgumentMemberNotDeclOnType(object p0, object p1)
        {
            return new ArgumentException(Strings.ArgumentMemberNotDeclOnType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2)
        {
            return Dynamic.Utils.Error.ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2);
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchParameter(object p0, object p1)
        {
            return Dynamic.Utils.Error.ExpressionTypeDoesNotMatchParameter(p0, p1);
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for return type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchReturn(object p0, object p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchReturn(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for assignment to type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchAssignment(object p0, object p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchAssignment(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for label of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchLabel(object p0, object p1)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchLabel(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be invoked"
        /// </summary>
        internal static Exception ExpressionTypeNotInvocable(object p0)
        {
            return new ArgumentException(Strings.ExpressionTypeNotInvocable(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static Exception FieldNotDefinedForType(object p0, object p1)
        {
            return new ArgumentException(Strings.FieldNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance field '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static Exception InstanceFieldNotDefinedForType(object p0, object p1)
        {
            return new ArgumentException(Strings.InstanceFieldNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Field '{0}.{1}' is not defined for type '{2}'"
        /// </summary>
        internal static Exception FieldInfoNotDefinedForType(object p0, object p1, object p2)
        {
            return new ArgumentException(Strings.FieldInfoNotDefinedForType(p0, p1, p2));
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of indexes"
        /// </summary>
        internal static Exception IncorrectNumberOfIndexes()
        {
            return new ArgumentException(Strings.IncorrectNumberOfIndexes);
        }
        /// <summary>
        /// InvalidOperationException with message like "Incorrect number of arguments supplied for lambda invocation"
        /// </summary>
        internal static Exception IncorrectNumberOfLambdaArguments()
        {
            return Dynamic.Utils.Error.IncorrectNumberOfLambdaArguments();
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of parameters supplied for lambda declaration"
        /// </summary>
        internal static Exception IncorrectNumberOfLambdaDeclarationParameters()
        {
            return new ArgumentException(Strings.IncorrectNumberOfLambdaDeclarationParameters);
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments supplied for call to method '{0}'"
        /// </summary>
        internal static Exception IncorrectNumberOfMethodCallArguments(object p0)
        {
            return Dynamic.Utils.Error.IncorrectNumberOfMethodCallArguments(p0);
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments for constructor"
        /// </summary>
        internal static Exception IncorrectNumberOfConstructorArguments()
        {
            return Dynamic.Utils.Error.IncorrectNumberOfConstructorArguments();
        }
        /// <summary>
        /// ArgumentException with message like " Incorrect number of members for constructor"
        /// </summary>
        internal static Exception IncorrectNumberOfMembersForGivenConstructor()
        {
            return new ArgumentException(Strings.IncorrectNumberOfMembersForGivenConstructor);
        }
        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments for the given members "
        /// </summary>
        internal static Exception IncorrectNumberOfArgumentsForMembers()
        {
            return new ArgumentException(Strings.IncorrectNumberOfArgumentsForMembers);
        }
        /// <summary>
        /// ArgumentException with message like "Lambda type parameter must be derived from System.Delegate"
        /// </summary>
        internal static Exception LambdaTypeMustBeDerivedFromSystemDelegate()
        {
            return new ArgumentException(Strings.LambdaTypeMustBeDerivedFromSystemDelegate);
        }
        /// <summary>
        /// ArgumentException with message like "Member '{0}' not field or property"
        /// </summary>
        internal static Exception MemberNotFieldOrProperty(object p0)
        {
            return new ArgumentException(Strings.MemberNotFieldOrProperty(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Method {0} contains generic parameters"
        /// </summary>
        internal static Exception MethodContainsGenericParameters(object p0)
        {
            return new ArgumentException(Strings.MethodContainsGenericParameters(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Method {0} is a generic method definition"
        /// </summary>
        internal static Exception MethodIsGeneric(object p0)
        {
            return new ArgumentException(Strings.MethodIsGeneric(p0));
        }
        /// <summary>
        /// ArgumentException with message like "The method '{0}.{1}' is not a property accessor"
        /// </summary>
        internal static Exception MethodNotPropertyAccessor(object p0, object p1)
        {
            return new ArgumentException(Strings.MethodNotPropertyAccessor(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'get' accessor"
        /// </summary>
        internal static Exception PropertyDoesNotHaveGetter(object p0)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveGetter(p0));
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'set' accessor"
        /// </summary>
        internal static Exception PropertyDoesNotHaveSetter(object p0)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveSetter(p0));
        }
        /// <summary>
        /// ArgumentException with message like "The property '{0}' has no 'get' or 'set' accessors"
        /// </summary>
        internal static Exception PropertyDoesNotHaveAccessor(object p0)
        {
            return new ArgumentException(Strings.PropertyDoesNotHaveAccessor(p0));
        }
        /// <summary>
        /// ArgumentException with message like "'{0}' is not a member of type '{1}'"
        /// </summary>
        internal static Exception NotAMemberOfType(object p0, object p1)
        {
            return new ArgumentException(Strings.NotAMemberOfType(p0, p1));
        }
        /// <summary>
        /// PlatformNotSupportedException with message like "The instruction '{0}' is not supported for type '{1}'"
        /// </summary>
        internal static Exception ExpressionNotSupportedForType(object p0, object p1)
        {
            return new PlatformNotSupportedException(Strings.ExpressionNotSupportedForType(p0, p1));
        }
        /// <summary>
        /// PlatformNotSupportedException with message like "The instruction '{0}' is not supported for nullable type '{1}'"
        /// </summary>
        internal static Exception ExpressionNotSupportedForNullableType(object p0, object p1)
        {
            return new PlatformNotSupportedException(Strings.ExpressionNotSupportedForNullableType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "ParameterExpression of type '{0}' cannot be used for delegate parameter of type '{1}'"
        /// </summary>
        internal static Exception ParameterExpressionNotValidAsDelegate(object p0, object p1)
        {
            return new ArgumentException(Strings.ParameterExpressionNotValidAsDelegate(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static Exception PropertyNotDefinedForType(object p0, object p1)
        {
            return new ArgumentException(Strings.PropertyNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}' is not defined for type '{1}'"
        /// </summary>
        internal static Exception InstancePropertyNotDefinedForType(object p0, object p1)
        {
            return new ArgumentException(Strings.InstancePropertyNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}' that takes no argument is not defined for type '{1}'"
        /// </summary>
        internal static Exception InstancePropertyWithoutParameterNotDefinedForType(object p0, object p1)
        {
            return new ArgumentException(Strings.InstancePropertyWithoutParameterNotDefinedForType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Instance property '{0}{1}' is not defined for type '{2}'"
        /// </summary>
        internal static Exception InstancePropertyWithSpecifiedParametersNotDefinedForType(object p0, object p1, object p2)
        {
            return new ArgumentException(Strings.InstancePropertyWithSpecifiedParametersNotDefinedForType(p0, p1, p2));
        }
        /// <summary>
        /// ArgumentException with message like "Method '{0}' declared on type '{1}' cannot be called with instance of type '{2}'"
        /// </summary>
        internal static Exception InstanceAndMethodTypeMismatch(object p0, object p1, object p2)
        {
            return new ArgumentException(Strings.InstanceAndMethodTypeMismatch(p0, p1, p2));
        }

        /// <summary>
        /// ArgumentException with message like "Type '{0}' does not have a default constructor"
        /// </summary>
        internal static Exception TypeMissingDefaultConstructor(object p0)
        {
            return new ArgumentException(Strings.TypeMissingDefaultConstructor(p0));
        }
        /// <summary>
        /// ArgumentException with message like "List initializers must contain at least one initializer"
        /// </summary>
        internal static Exception ListInitializerWithZeroMembers()
        {
            return new ArgumentException(Strings.ListInitializerWithZeroMembers);
        }
        /// <summary>
        /// ArgumentException with message like "Element initializer method must be named 'Add'"
        /// </summary>
        internal static Exception ElementInitializerMethodNotAdd()
        {
            return new ArgumentException(Strings.ElementInitializerMethodNotAdd);
        }
        /// <summary>
        /// ArgumentException with message like "Parameter '{0}' of element initializer method '{1}' must not be a pass by reference parameter"
        /// </summary>
        internal static Exception ElementInitializerMethodNoRefOutParam(object p0, object p1)
        {
            return new ArgumentException(Strings.ElementInitializerMethodNoRefOutParam(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Element initializer method must have at least 1 parameter"
        /// </summary>
        internal static Exception ElementInitializerMethodWithZeroArgs()
        {
            return new ArgumentException(Strings.ElementInitializerMethodWithZeroArgs);
        }
        /// <summary>
        /// ArgumentException with message like "Element initializer method must be an instance method"
        /// </summary>
        internal static Exception ElementInitializerMethodStatic()
        {
            return new ArgumentException(Strings.ElementInitializerMethodStatic);
        }
        /// <summary>
        /// ArgumentException with message like "Type '{0}' is not IEnumerable"
        /// </summary>
        internal static Exception TypeNotIEnumerable(object p0)
        {
            return new ArgumentException(Strings.TypeNotIEnumerable(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Unexpected coalesce operator."
        /// </summary>
        internal static Exception UnexpectedCoalesceOperator()
        {
            return new InvalidOperationException(Strings.UnexpectedCoalesceOperator);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot cast from type '{0}' to type '{1}"
        /// </summary>
        internal static Exception InvalidCast(object p0, object p1)
        {
            return new InvalidOperationException(Strings.InvalidCast(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled binary: {0}"
        /// </summary>
        internal static Exception UnhandledBinary(object p0)
        {
            return new ArgumentException(Strings.UnhandledBinary(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled binding "
        /// </summary>
        internal static Exception UnhandledBinding()
        {
            return new ArgumentException(Strings.UnhandledBinding);
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled Binding Type: {0}"
        /// </summary>
        internal static Exception UnhandledBindingType(object p0)
        {
            return new ArgumentException(Strings.UnhandledBindingType(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled convert: {0}"
        /// </summary>
        internal static Exception UnhandledConvert(object p0)
        {
            return new ArgumentException(Strings.UnhandledConvert(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled Expression Type: {0}"
        /// </summary>
        internal static Exception UnhandledExpressionType(object p0)
        {
            return new ArgumentException(Strings.UnhandledExpressionType(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Unhandled unary: {0}"
        /// </summary>
        internal static Exception UnhandledUnary(object p0)
        {
            return new ArgumentException(Strings.UnhandledUnary(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Unknown binding type"
        /// </summary>
        internal static Exception UnknownBindingType()
        {
            return new ArgumentException(Strings.UnknownBindingType);
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must have identical parameter and return types."
        /// </summary>
        internal static Exception UserDefinedOpMustHaveConsistentTypes(object p0, object p1)
        {
            return new ArgumentException(Strings.UserDefinedOpMustHaveConsistentTypes(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must return the same type as its parameter or a derived type."
        /// </summary>
        internal static Exception UserDefinedOpMustHaveValidReturnType(object p0, object p1)
        {
            return new ArgumentException(Strings.UserDefinedOpMustHaveValidReturnType(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "The user-defined operator method '{1}' for operator '{0}' must have associated boolean True and False operators."
        /// </summary>
        internal static Exception LogicalOperatorMustHaveBooleanOperators(object p0, object p1)
        {
            return new ArgumentException(Strings.LogicalOperatorMustHaveBooleanOperators(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "No method '{0}' exists on type '{1}'."
        /// </summary>
        internal static Exception MethodDoesNotExistOnType(object p0, object p1)
        {
            return new InvalidOperationException(Strings.MethodDoesNotExistOnType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "No method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static Exception MethodWithArgsDoesNotExistOnType(object p0, object p1)
        {
            return new InvalidOperationException(Strings.MethodWithArgsDoesNotExistOnType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "No generic method '{0}' on type '{1}' is compatible with the supplied type arguments and arguments. No type arguments should be provided if the method is non-generic. "
        /// </summary>
        internal static Exception GenericMethodWithArgsDoesNotExistOnType(object p0, object p1)
        {
            return new InvalidOperationException(Strings.GenericMethodWithArgsDoesNotExistOnType(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "More than one method '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static Exception MethodWithMoreThanOneMatch(object p0, object p1)
        {
            return new InvalidOperationException(Strings.MethodWithMoreThanOneMatch(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "More than one property '{0}' on type '{1}' is compatible with the supplied arguments."
        /// </summary>
        internal static Exception PropertyWithMoreThanOneMatch(object p0, object p1)
        {
            return new InvalidOperationException(Strings.PropertyWithMoreThanOneMatch(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "An incorrect number of type args were specified for the declaration of a Func type."
        /// </summary>
        internal static Exception IncorrectNumberOfTypeArgsForFunc()
        {
            return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForFunc);
        }
        /// <summary>
        /// ArgumentException with message like "An incorrect number of type args were specified for the declaration of an Action type."
        /// </summary>
        internal static Exception IncorrectNumberOfTypeArgsForAction()
        {
            return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForAction);
        }
        /// <summary>
        /// ArgumentException with message like "Argument type cannot be System.Void."
        /// </summary>
        internal static Exception ArgumentCannotBeOfTypeVoid()
        {
            return new ArgumentException(Strings.ArgumentCannotBeOfTypeVoid);
        }
        /// <summary>
        /// ArgumentException with message like "Invalid operation: '{0}'"
        /// </summary>
        internal static Exception InvalidOperation(object p0)
        {
            return new ArgumentException(Strings.InvalidOperation(p0));
        }
        /// <summary>
        /// ArgumentOutOfRangeException with message like "{0} must be greater than or equal to {1}"
        /// </summary>
        internal static Exception OutOfRange(object p0, object p1)
        {
            return new ArgumentOutOfRangeException(Strings.OutOfRange(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot redefine label '{0}' in an inner block."
        /// </summary>
        internal static Exception LabelTargetAlreadyDefined(object p0)
        {
            return new InvalidOperationException(Strings.LabelTargetAlreadyDefined(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to undefined label '{0}'."
        /// </summary>
        internal static Exception LabelTargetUndefined(object p0)
        {
            return new InvalidOperationException(Strings.LabelTargetUndefined(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot leave a finally block."
        /// </summary>
        internal static Exception ControlCannotLeaveFinally()
        {
            return new InvalidOperationException(Strings.ControlCannotLeaveFinally);
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot leave a filter test."
        /// </summary>
        internal static Exception ControlCannotLeaveFilterTest()
        {
            return new InvalidOperationException(Strings.ControlCannotLeaveFilterTest);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to ambiguous label '{0}'."
        /// </summary>
        internal static Exception AmbiguousJump(object p0)
        {
            return new InvalidOperationException(Strings.AmbiguousJump(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot enter a try block."
        /// </summary>
        internal static Exception ControlCannotEnterTry()
        {
            return new InvalidOperationException(Strings.ControlCannotEnterTry);
        }
        /// <summary>
        /// InvalidOperationException with message like "Control cannot enter an expression--only statements can be jumped into."
        /// </summary>
        internal static Exception ControlCannotEnterExpression()
        {
            return new InvalidOperationException(Strings.ControlCannotEnterExpression);
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot jump to non-local label '{0}' with a value. Only jumps to labels defined in outer blocks can pass values."
        /// </summary>
        internal static Exception NonLocalJumpWithValue(object p0)
        {
            return new InvalidOperationException(Strings.NonLocalJumpWithValue(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Extension should have been reduced."
        /// </summary>
        internal static Exception ExtensionNotReduced()
        {
            return new InvalidOperationException(Strings.ExtensionNotReduced);
        }
        /// <summary>
        /// InvalidOperationException with message like "CompileToMethod cannot compile constant '{0}' because it is a non-trivial value, such as a live object. Instead, create an expression tree that can construct this value."
        /// </summary>
        internal static Exception CannotCompileConstant(object p0)
        {
            return new InvalidOperationException(Strings.CannotCompileConstant(p0));
        }
        /// <summary>
        /// NotSupportedException with message like "Dynamic expressions are not supported by CompileToMethod. Instead, create an expression tree that uses System.Runtime.CompilerServices.CallSite."
        /// </summary>
        internal static Exception CannotCompileDynamic()
        {
            return new NotSupportedException(Strings.CannotCompileDynamic);
        }
        /// <summary>
        /// InvalidOperationException with message like "Invalid lvalue for assignment: {0}."
        /// </summary>
        internal static Exception InvalidLvalue(object p0)
        {
            return new InvalidOperationException(Strings.InvalidLvalue(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Invalid member type: {0}."
        /// </summary>
        internal static Exception InvalidMemberType(object p0)
        {
            return new InvalidOperationException(Strings.InvalidMemberType(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "unknown lift type: '{0}'."
        /// </summary>
        internal static Exception UnknownLiftType(object p0)
        {
            return new InvalidOperationException(Strings.UnknownLiftType(p0));
        }
        /// <summary>
        /// ArgumentException with message like "Invalid output directory."
        /// </summary>
        internal static Exception InvalidOutputDir()
        {
            return new ArgumentException(Strings.InvalidOutputDir);
        }
        /// <summary>
        /// ArgumentException with message like "Invalid assembly name or file extension."
        /// </summary>
        internal static Exception InvalidAsmNameOrExtension()
        {
            return new ArgumentException(Strings.InvalidAsmNameOrExtension);
        }
        /// <summary>
        /// ArgumentException with message like "Cannot create instance of {0} because it contains generic parameters"
        /// </summary>
        internal static Exception IllegalNewGenericParams(object p0)
        {
            return new ArgumentException(Strings.IllegalNewGenericParams(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "variable '{0}' of type '{1}' referenced from scope '{2}', but it is not defined"
        /// </summary>
        internal static Exception UndefinedVariable(object p0, object p1, object p2)
        {
            return new InvalidOperationException(Strings.UndefinedVariable(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Cannot close over byref parameter '{0}' referenced in lambda '{1}'"
        /// </summary>
        internal static Exception CannotCloseOverByRef(object p0, object p1)
        {
            return new InvalidOperationException(Strings.CannotCloseOverByRef(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Unexpected VarArgs call to method '{0}'"
        /// </summary>
        internal static Exception UnexpectedVarArgsCall(object p0)
        {
            return new InvalidOperationException(Strings.UnexpectedVarArgsCall(p0));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rethrow statement is valid only inside a Catch block."
        /// </summary>
        internal static Exception RethrowRequiresCatch()
        {
            return new InvalidOperationException(Strings.RethrowRequiresCatch);
        }
        /// <summary>
        /// InvalidOperationException with message like "Try expression is not allowed inside a filter body."
        /// </summary>
        internal static Exception TryNotAllowedInFilter()
        {
            return new InvalidOperationException(Strings.TryNotAllowedInFilter);
        }
        /// <summary>
        /// InvalidOperationException with message like "When called from '{0}', rewriting a node of type '{1}' must return a non-null value of the same type. Alternatively, override '{2}' and change it to not visit children of this type."
        /// </summary>
        internal static Exception MustRewriteToSameNode(object p0, object p1, object p2)
        {
            return new InvalidOperationException(Strings.MustRewriteToSameNode(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rewriting child expression from type '{0}' to type '{1}' is not allowed, because it would change the meaning of the operation. If this is intentional, override '{2}' and change it to allow this rewrite."
        /// </summary>
        internal static Exception MustRewriteChildToSameType(object p0, object p1, object p2)
        {
            return new InvalidOperationException(Strings.MustRewriteChildToSameType(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "Rewritten expression calls operator method '{0}', but the original node had no operator method. If this is is intentional, override '{1}' and change it to allow this rewrite."
        /// </summary>
        internal static Exception MustRewriteWithoutMethod(object p0, object p1)
        {
            return new InvalidOperationException(Strings.MustRewriteWithoutMethod(p0, p1));
        }
        /// <summary>
        /// NotSupportedException with message like "TryExpression is not supported as an argument to method '{0}' because it has an argument with by-ref type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static Exception TryNotSupportedForMethodsWithRefArgs(object p0)
        {
            return new NotSupportedException(Strings.TryNotSupportedForMethodsWithRefArgs(p0));
        }
        /// <summary>
        /// NotSupportedException with message like "TryExpression is not supported as a child expression when accessing a member on type '{0}' because it is a value type. Construct the tree so the TryExpression is not nested inside of this expression."
        /// </summary>
        internal static Exception TryNotSupportedForValueTypeInstances(object p0)
        {
            return new NotSupportedException(Strings.TryNotSupportedForValueTypeInstances(p0));
        }

        /// <summary>
        /// InvalidOperationException with message like "Dynamic operations can only be performed in homogenous AppDomain."
        /// </summary>
        internal static Exception HomogenousAppDomainRequired()
        {
            return new InvalidOperationException(Strings.HomogenousAppDomainRequired);
        }
        /// <summary>
        /// ArgumentException with message like "Test value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static Exception TestValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
        {
            return new ArgumentException(Strings.TestValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
        }
        /// <summary>
        /// ArgumentException with message like "Switch value of type '{0}' cannot be used for the comparison method parameter of type '{1}'"
        /// </summary>
        internal static Exception SwitchValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
        {
            return new ArgumentException(Strings.SwitchValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
        }
        /// <summary>
        /// NotSupportedException with message like "DebugInfoGenerator created by CreatePdbGenerator can only be used with LambdaExpression.CompileToMethod."
        /// </summary>
        internal static Exception PdbGeneratorNeedsExpressionCompiler()
        {
            return new NotSupportedException(Strings.PdbGeneratorNeedsExpressionCompiler);
        }

        /// <summary>
        /// The exception that is thrown when a null reference (Nothing in Visual Basic) is passed to a method that does not accept it as a valid argument.
        /// </summary>
        internal static Exception ArgumentNull(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        /// <summary>
        /// The exception that is thrown when the value of an argument is outside the allowable range of values as defined by the invoked method.
        /// </summary>
        internal static Exception ArgumentOutOfRange(string paramName)
        {
            return new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// The exception that is thrown when an invoked method is not supported, or when there is an attempt to read, seek, or write to a stream that does not support the invoked functionality. 
        /// </summary>
        internal static Exception NotSupported()
        {
            return new NotSupportedException();
        }

#if FEATURE_COMPILE
        /// <summary>
        /// NotImplementedException with message like "The operator '{0}' is not implemented for type '{1}'"
        /// </summary>
        internal static Exception OperatorNotImplementedForType(object p0, object p1)
        {
            return NotImplemented.ByDesignWithMessage(Strings.OperatorNotImplementedForType(p0, p1));
        }
#endif

        /// <summary>
        /// ArgumentException with message like "The constructor should not be static"
        /// </summary>
        internal static Exception NonStaticConstructorRequired()
        {
            return new ArgumentException(Strings.NonStaticConstructorRequired);
        }

        /// <summary>
        /// InvalidOperationException with message like "Can't compile a NewExpression with a constructor declared on an abstract class"
        /// </summary>
        internal static Exception NonAbstractConstructorRequired()
        {
            return new InvalidOperationException(Strings.NonAbstractConstructorRequired);
        }
    }
}
