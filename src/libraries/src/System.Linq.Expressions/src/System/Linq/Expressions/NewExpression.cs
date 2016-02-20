// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a constructor call.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.NewExpressionProxy))]
    public class NewExpression : Expression, IArgumentProvider
    {
        private readonly ConstructorInfo _constructor;
        private IList<Expression> _arguments;
        private readonly ReadOnlyCollection<MemberInfo> _members;

        internal NewExpression(ConstructorInfo constructor, IList<Expression> arguments, ReadOnlyCollection<MemberInfo> members)
        {
            _constructor = constructor;
            _arguments = arguments;
            _members = members;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public override Type Type
        {
            get { return _constructor.DeclaringType; }
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression" />. (Inherited from <see cref="Expression" />.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.New; }
        }

        /// <summary>
        /// Gets the called constructor.
        /// </summary>
        public ConstructorInfo Constructor
        {
            get { return _constructor; }
        }

        /// <summary>
        /// Gets the arguments to the constructor.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments
        {
            get { return ReturnReadOnly(ref _arguments); }
        }

        public Expression GetArgument(int index)
        {
            return _arguments[index];
        }

        public int ArgumentCount
        {
            get
            {
                return _arguments.Count;
            }
        }

        /// <summary>
        /// Gets the members that can retrieve the values of the fields that were initialized with constructor arguments.
        /// </summary>
        public ReadOnlyCollection<MemberInfo> Members
        {
            get { return _members; }
        }

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
        /// <param name="arguments">The <see cref="Arguments" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public NewExpression Update(IEnumerable<Expression> arguments)
        {
            if (arguments == Arguments)
            {
                return this;
            }
            if (Members != null)
            {
                return Expression.New(Constructor, arguments, Members);
            }
            return Expression.New(Constructor, arguments);
        }
    }

    internal class NewValueTypeExpression : NewExpression
    {
        private readonly Type _valueType;

        internal NewValueTypeExpression(Type type, ReadOnlyCollection<Expression> arguments, ReadOnlyCollection<MemberInfo> members)
            : base(null, arguments, members)
        {
            _valueType = type;
        }

        public sealed override Type Type
        {
            get { return _valueType; }
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments. 
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="P:Constructor"/> property equal to.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="P:New"/> and the <see cref="P:Constructor"/> property set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor)
        {
            return New(constructor, (IEnumerable<Expression>)null);
        }


        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments. 
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="P:Constructor"/> property equal to.</param>
        /// <param name="arguments">An array of <see cref="Expression"/> objects to use to populate the Arguments collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="P:New"/> and the <see cref="P:Constructor"/> and <see cref="P:Arguments"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, params Expression[] arguments)
        {
            return New(constructor, (IEnumerable<Expression>)arguments);
        }


        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor that takes no arguments. 
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="P:Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the Arguments collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="P:New"/> and the <see cref="P:Constructor"/> and <see cref="P:Arguments"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNull(constructor.DeclaringType, nameof(constructor) + "." + nameof(constructor.DeclaringType));
            TypeUtils.ValidateType(constructor.DeclaringType);
            ValidateConstructor(constructor);
            var argList = arguments.ToReadOnly();
            ValidateArgumentTypes(constructor, ExpressionType.New, ref argList);

            return new NewExpression(constructor, argList, null);
        }


        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified. 
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="P:Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the Arguments collection.</param>
        /// <param name="members">An <see cref="IEnumerable{T}"/> of <see cref="MemberInfo"/> objects to use to populate the Members collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="P:New"/> and the <see cref="P:Constructor"/>, <see cref="P:Arguments"/> and <see cref="P:Members"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression> arguments, IEnumerable<MemberInfo> members)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ValidateConstructor(constructor);
            var memberList = members.ToReadOnly();
            var argList = arguments.ToReadOnly();
            ValidateNewArgs(constructor, ref argList, ref memberList);
            return new NewExpression(constructor, argList, memberList);
        }


        /// <summary>
        /// Creates a new <see cref="NewExpression"/> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified. 
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> to set the <see cref="P:Constructor"/> property equal to.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> of <see cref="Expression"/> objects to use to populate the Arguments collection.</param>
        /// <param name="members">An Array of <see cref="MemberInfo"/> objects to use to populate the Members collection.</param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="P:New"/> and the <see cref="P:Constructor"/>, <see cref="P:Arguments"/> and <see cref="P:Members"/> properties set to the specified value.</returns>
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression> arguments, params MemberInfo[] members)
        {
            return New(constructor, arguments, (IEnumerable<MemberInfo>)members);
        }


        /// <summary>
        /// Creates a <see cref="NewExpression"/> that represents calling the parameterless constructor of the specified type. 
        /// </summary>
        /// <param name="type">A <see cref="Type"/> that has a constructor that takes no arguments. </param>
        /// <returns>A <see cref="NewExpression"/> that has the <see cref="NodeType"/> property equal to New and the Constructor property set to the ConstructorInfo that represents the parameterless constructor of the specified type.</returns>
        public static NewExpression New(Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid();
            }
            ConstructorInfo ci = null;
            if (!type.GetTypeInfo().IsValueType)
            {
                ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(c => c.GetParameters().Length == 0).SingleOrDefault();
                if (ci == null)
                {
                    throw Error.TypeMissingDefaultConstructor(type);
                }
                return New(ci);
            }
            return new NewValueTypeExpression(type, EmptyReadOnlyCollection<Expression>.Instance, null);
        }



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
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
                Expression[] newArguments = null;
                MemberInfo[] newMembers = null;
                for (int i = 0, n = arguments.Count; i < n; i++)
                {
                    Expression arg = arguments[i];
                    RequiresCanRead(arg, "argument");
                    MemberInfo member = members[i];
                    ContractUtils.RequiresNotNull(member, nameof(member));
                    if (!TypeUtils.AreEquivalent(member.DeclaringType, constructor.DeclaringType))
                    {
                        throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType.Name);
                    }
                    Type memberType;
                    ValidateAnonymousTypeMember(ref member, out memberType);
                    if (!TypeUtils.AreReferenceAssignable(memberType, arg.Type))
                    {
                        if (!TryQuote(memberType, ref arg))
                        {
                            throw Error.ArgumentTypeDoesNotMatchMember(arg.Type, memberType);
                        }
                    }
                    ParameterInfo pi = pis[i];
                    Type pType = pi.ParameterType;
                    if (pType.IsByRef)
                    {
                        pType = pType.GetElementType();
                    }
                    if (!TypeUtils.AreReferenceAssignable(pType, arg.Type))
                    {
                        if (!TryQuote(pType, ref arg))
                        {
                            throw Error.ExpressionTypeDoesNotMatchConstructorParameter(arg.Type, pType);
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


        private static void ValidateAnonymousTypeMember(ref MemberInfo member, out Type memberType)
        {
            FieldInfo field = member as FieldInfo;
            if (field != null)
            {
                if (field.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember();
                }
                memberType = field.FieldType;
                return;
            }

            PropertyInfo pi = member as PropertyInfo;
            if (pi != null)
            {
                if (!pi.CanRead)
                {
                    throw Error.PropertyDoesNotHaveGetter(pi);
                }
                if (pi.GetGetMethod().IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember();
                }
                memberType = pi.PropertyType;
                return;
            }

            MethodInfo method = member as MethodInfo;
            if (method != null)
            {
                if (method.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember();
                }

                PropertyInfo prop = GetProperty(method);
                member = prop;
                memberType = prop.PropertyType;
                return;
            }
            throw Error.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod();
        }

        private static void ValidateConstructor(ConstructorInfo constructor)
        {
            if (constructor.IsStatic)
                throw Error.NonStaticConstructorRequired();
        }
    }
}
