// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a constructor call.
    /// </summary>
    [DebuggerTypeProxy(typeof(NewExpressionProxy))]
    public class NewExpression : Expression, IArgumentProvider
    {
        private IReadOnlyList<Expression> _arguments;

        internal NewExpression(ConstructorInfo? constructor, IReadOnlyList<Expression> arguments, ReadOnlyCollection<MemberInfo>? members)
        {
            Constructor = constructor;
            _arguments = arguments;
            Members = members;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public override Type Type => Constructor!.DeclaringType!;

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.New;

        /// <summary>
        /// Gets the called constructor.
        /// </summary>
        public ConstructorInfo? Constructor { get; }

        /// <summary>
        /// Gets the arguments to the constructor.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments => ExpressionUtils.ReturnReadOnly(ref _arguments);

        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        public Expression GetArgument(int index) => _arguments[index];

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        public int ArgumentCount => _arguments.Count;

        /// <summary>
        /// Gets the members that can retrieve the values of the fields that were initialized with constructor arguments.
        /// </summary>
        public ReadOnlyCollection<MemberInfo>? Members { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitNew(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="arguments">The <see cref="NewExpression.Arguments"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "A NewExpression has already been created. The original creator will get a warning that it is not trim compatible.")]
        public NewExpression Update(IEnumerable<Expression>? arguments)
        {
            if (ExpressionUtils.SameElements(ref arguments, Arguments))
            {
                return this;
            }

            return Members != null ? New(Constructor!, arguments, Members) : New(Constructor!, arguments);
        }
    }

    internal sealed class NewValueTypeExpression : NewExpression
    {
        internal NewValueTypeExpression(Type type, ReadOnlyCollection<Expression> arguments, ReadOnlyCollection<MemberInfo>? members)
            : base(null, arguments, members)
        {
            Type = type;
        }

        public sealed override Type Type { get; }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="NewExpression.Constructor"/> property equal to.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/> property set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor)
        {
            return New(constructor, (IEnumerable<Expression>?)null);
        }

        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="NewExpression.Constructor"/> property equal to.</param>
        /// <param name="arguments">An array of <see cref="Expression"/> objects to use to populate the Arguments collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/> and <see cref="NewExpression.Arguments"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, params Expression[]? arguments)
        {
            return New(constructor, (IEnumerable<Expression>?)arguments);
        }

        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="NewExpression.Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the <see cref="NewExpression.Arguments"/> collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/> and <see cref="NewExpression.Arguments"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNull(constructor.DeclaringType!, nameof(constructor) + "." + nameof(constructor.DeclaringType));
            TypeUtils.ValidateType(constructor.DeclaringType!, nameof(constructor), allowByRef: true, allowPointer: true);
            ValidateConstructor(constructor, nameof(constructor));
            ReadOnlyCollection<Expression> argList = arguments.ToReadOnly();
            ValidateArgumentTypes(constructor, ExpressionType.New, ref argList, nameof(constructor));

            return new NewExpression(constructor, argList, null);
        }

        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="NewExpression.Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the <see cref="NewExpression.Arguments"/> collection.</param>
        /// <param name="members">An <see cref="IEnumerable{T}"/> of <see cref="MemberInfo"/> objects to use to populate the <see cref="NewExpression.Members"/> collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/>, <see cref="NewExpression.Arguments"/> and <see cref="NewExpression.Members"/> properties set to the specified value.</returns>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments, IEnumerable<MemberInfo>? members)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNull(constructor.DeclaringType!, nameof(constructor) + "." + nameof(constructor.DeclaringType));
            TypeUtils.ValidateType(constructor.DeclaringType!, nameof(constructor), allowByRef: true, allowPointer: true);
            ValidateConstructor(constructor, nameof(constructor));
            ReadOnlyCollection<MemberInfo> memberList = members.ToReadOnly();
            ReadOnlyCollection<Expression> argList = arguments.ToReadOnly();
            ValidateNewArgs(constructor, ref argList, ref memberList);
            return new NewExpression(constructor, argList, memberList);
        }

        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="NewExpression.Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the <see cref="NewExpression.Arguments"/> collection.</param>
        /// <param name="members">An Array of <see cref="MemberInfo"/> objects to use to populate the <see cref="NewExpression.Members"/> collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/>, <see cref="NewExpression.Arguments"/> and <see cref="NewExpression.Members"/> properties set to the specified value.</returns>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments, params MemberInfo[]? members)
        {
            return New(constructor, arguments, (IEnumerable<MemberInfo>?)members);
        }

        /// <summary>
        /// Creates a <see cref="NewExpression"/> that represents calling the parameterless constructor of the specified type.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> that has a constructor that takes no arguments.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.New"/> and the <see cref="NewExpression.Constructor"/> property set to the <see cref="ConstructorInfo"/> that represents the parameterless constructor of the specified type.</returns>
        public static NewExpression New(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }
            TypeUtils.ValidateType(type, nameof(type));

            if (!type.IsValueType)
            {
                ConstructorInfo? ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault(c => c.GetParametersCached().Length == 0);
                if (ci == null)
                {
                    throw Error.TypeMissingDefaultConstructor(type, nameof(type));
                }
                return New(ci);
            }
            return new NewValueTypeExpression(type, EmptyReadOnlyCollection<Expression>.Instance, null);
        }

        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        private static void ValidateNewArgs(ConstructorInfo constructor, ref ReadOnlyCollection<Expression> arguments, ref ReadOnlyCollection<MemberInfo> members)
        {
            ParameterInfo[] pis;
            if ((pis = constructor.GetParametersCached()).Length > 0)
            {
                if (arguments.Count != pis.Length)
                {
                    throw Error.IncorrectNumberOfConstructorArguments();
                }
                if (arguments.Count != members.Count)
                {
                    throw Error.IncorrectNumberOfArgumentsForMembers();
                }
                Expression[]? newArguments = null;
                MemberInfo[]? newMembers = null;
                for (int i = 0, n = arguments.Count; i < n; i++)
                {
                    Expression arg = arguments[i];
                    ExpressionUtils.RequiresCanRead(arg, nameof(arguments), i);
                    MemberInfo member = members[i];
                    ContractUtils.RequiresNotNull(member, nameof(members), i);
                    if (!TypeUtils.AreEquivalent(member.DeclaringType, constructor.DeclaringType))
                    {
                        throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType!.Name, nameof(members), i);
                    }
                    Type memberType;
                    ValidateAnonymousTypeMember(ref member, out memberType, nameof(members), i);
                    if (!TypeUtils.AreReferenceAssignable(memberType, arg.Type))
                    {
                        if (!TryQuote(memberType, ref arg))
                        {
                            throw Error.ArgumentTypeDoesNotMatchMember(arg.Type, memberType, nameof(arguments), i);
                        }
                    }
                    ParameterInfo pi = pis[i];
                    Type pType = pi.ParameterType;
                    if (pType.IsByRef)
                    {
                        pType = pType.GetElementType()!;
                    }
                    if (!TypeUtils.AreReferenceAssignable(pType, arg.Type))
                    {
                        if (!TryQuote(pType, ref arg))
                        {
                            throw Error.ExpressionTypeDoesNotMatchConstructorParameter(arg.Type, pType, nameof(arguments), i);
                        }
                    }
                    if (newArguments == null && arg != arguments[i])
                    {
                        newArguments = new Expression[arguments.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newArguments[j] = arguments[j];
                        }
                    }
                    if (newArguments != null)
                    {
                        newArguments[i] = arg;
                    }

                    if (newMembers == null && member != members[i])
                    {
                        newMembers = new MemberInfo[members.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newMembers[j] = members[j];
                        }
                    }
                    if (newMembers != null)
                    {
                        newMembers[i] = member;
                    }
                }
                if (newArguments != null)
                {
                    arguments = new TrueReadOnlyCollection<Expression>(newArguments);
                }
                if (newMembers != null)
                {
                    members = new TrueReadOnlyCollection<MemberInfo>(newMembers);
                }
            }
            else if (arguments != null && arguments.Count > 0)
            {
                throw Error.IncorrectNumberOfConstructorArguments();
            }
            else if (members != null && members.Count > 0)
            {
                throw Error.IncorrectNumberOfMembersForGivenConstructor();
            }
        }

        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        private static void ValidateAnonymousTypeMember(ref MemberInfo member, out Type memberType, string paramName, int index)
        {
            if (member is FieldInfo field)
            {
                if (field.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }
                memberType = field.FieldType;
                return;
            }

            if (member is PropertyInfo pi)
            {
                if (!pi.CanRead)
                {
                    throw Error.PropertyDoesNotHaveGetter(pi, paramName, index);
                }
                if (pi.GetGetMethod()!.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }
                memberType = pi.PropertyType;
                return;
            }

            if (member is MethodInfo method)
            {
                if (method.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }

                PropertyInfo prop = GetProperty(method, paramName, index);
                member = prop;
                memberType = prop.PropertyType;
                return;
            }
            throw Error.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(paramName, index);
        }

        private static void ValidateConstructor(ConstructorInfo constructor, string paramName)
        {
            if (constructor.IsStatic)
                throw Error.NonStaticConstructorRequired(paramName);
        }
    }
}
