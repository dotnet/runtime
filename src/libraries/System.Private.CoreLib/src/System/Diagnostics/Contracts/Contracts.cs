// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: The contract class allows for expressing preconditions,
** postconditions, and object invariants about methods in source
** code for runtime checking & static analysis.
**
** Two classes (Contract and ContractHelper) are split into partial classes
** in order to share the public front for multiple platforms (this file)
** while providing separate implementation details for each platform.
**
===========================================================*/
#define DEBUG // The behavior of this contract library should be consistent regardless of build type.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Contracts
{
    #region Attributes

    /// <summary>
    /// Methods and classes marked with this attribute can be used within calls to Contract methods. Such methods not make any visible state changes.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class PureAttribute : Attribute
    {
    }

    /// <summary>
    /// Types marked with this attribute specify that a separate type contains the contracts for this type.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class ContractClassAttribute : Attribute
    {
        private readonly Type _typeWithContracts;

        public ContractClassAttribute(Type typeContainingContracts)
        {
            _typeWithContracts = typeContainingContracts;
        }

        public Type TypeContainingContracts => _typeWithContracts;
    }

    /// <summary>
    /// Types marked with this attribute specify that they are a contract for the type that is the argument of the constructor.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ContractClassForAttribute : Attribute
    {
        private readonly Type _typeIAmAContractFor;

        public ContractClassForAttribute(Type typeContractsAreFor)
        {
            _typeIAmAContractFor = typeContractsAreFor;
        }

        public Type TypeContractsAreFor => _typeIAmAContractFor;
    }

    /// <summary>
    /// This attribute is used to mark a method as being the invariant
    /// method for a class. The method can have any name, but it must
    /// return "void" and take no parameters. The body of the method
    /// must consist solely of one or more calls to the method
    /// Contract.Invariant. A suggested name for the method is
    /// "ObjectInvariant".
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ContractInvariantMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute that specifies that an assembly is a reference assembly with contracts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ContractReferenceAssemblyAttribute : Attribute
    {
    }

    /// <summary>
    /// Methods (and properties) marked with this attribute can be used within calls to Contract methods, but have no runtime behavior associated with them.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ContractRuntimeIgnoredAttribute : Attribute
    {
    }

    /// <summary>
    /// Instructs downstream tools whether to assume the correctness of this assembly, type or member without performing any verification or not.
    /// Can use [ContractVerification(false)] to explicitly mark assembly, type or member as one to *not* have verification performed on it.
    /// Most specific element found (member, type, then assembly) takes precedence.
    /// (That is useful if downstream tools allow a user to decide which polarity is the default, unmarked case.)
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a type to apply to all members of the type, including nested types.
    /// Apply this attribute to an assembly to apply to all types and members of the assembly.
    /// Apply this attribute to a property to apply to both the getter and setter.
    /// </remarks>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
    public sealed class ContractVerificationAttribute : Attribute
    {
        private readonly bool _value;

        public ContractVerificationAttribute(bool value) { _value = value; }

        public bool Value => _value;
    }

    /// <summary>
    /// Allows a field f to be used in the method contracts for a method m when f has less visibility than m.
    /// For instance, if the method is public, but the field is private.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ContractPublicPropertyNameAttribute : Attribute
    {
        private readonly string _publicName;

        public ContractPublicPropertyNameAttribute(string name)
        {
            _publicName = name;
        }

        public string Name => _publicName;
    }

    /// <summary>
    /// Enables factoring legacy if-then-throw into separate methods for reuse and full control over
    /// thrown exception and arguments
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Conditional("CONTRACTS_FULL")]
    public sealed class ContractArgumentValidatorAttribute : Attribute
    {
    }

    /// <summary>
    /// Enables writing abbreviations for contracts that get copied to other methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Conditional("CONTRACTS_FULL")]
    public sealed class ContractAbbreviatorAttribute : Attribute
    {
    }

    /// <summary>
    /// Allows setting contract and tool options at assembly, type, or method granularity.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [Conditional("CONTRACTS_FULL")]
    public sealed class ContractOptionAttribute : Attribute
    {
        private readonly string _category;
        private readonly string _setting;
        private readonly bool _enabled;
        private readonly string? _value;

        public ContractOptionAttribute(string category, string setting, bool enabled)
        {
            _category = category;
            _setting = setting;
            _enabled = enabled;
        }

        public ContractOptionAttribute(string category, string setting, string value)
        {
            _category = category;
            _setting = setting;
            _value = value;
        }

        public string Category => _category;

        public string Setting => _setting;

        public bool Enabled => _enabled;

        public string? Value => _value;
    }

    #endregion Attributes

    /// <summary>
    /// Contains static methods for representing program contracts such as preconditions, postconditions, and invariants.
    /// </summary>
    /// <remarks>
    /// WARNING: A binary rewriter must be used to insert runtime enforcement of these contracts.
    /// Otherwise some contracts like Ensures can only be checked statically and will not throw exceptions during runtime when contracts are violated.
    /// Please note this class uses conditional compilation to help avoid easy mistakes.  Defining the preprocessor
    /// symbol CONTRACTS_PRECONDITIONS will include all preconditions expressed using Contract.Requires in your
    /// build.  The symbol CONTRACTS_FULL will include postconditions and object invariants, and requires the binary rewriter.
    /// </remarks>
    public static partial class Contract
    {
        #region User Methods

        #region Assume

        /// <summary>
        /// Instructs code analysis tools to assume the expression <paramref name="condition"/> is true even if it can not be statically proven to always be true.
        /// </summary>
        /// <param name="condition">Expression to assume will always be true.</param>
        /// <remarks>
        /// At runtime this is equivalent to an <seealso cref="System.Diagnostics.Contracts.Contract.Assert(bool)"/>.
        /// </remarks>
        [Pure]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        public static void Assume([DoesNotReturnIf(false)] bool condition)
        {
            if (!condition)
            {
                ReportFailure(ContractFailureKind.Assume, null, null, null);
            }
        }

        /// <summary>
        /// Instructs code analysis tools to assume the expression <paramref name="condition"/> is true even if it can not be statically proven to always be true.
        /// </summary>
        /// <param name="condition">Expression to assume will always be true.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// At runtime this is equivalent to an <seealso cref="System.Diagnostics.Contracts.Contract.Assert(bool)"/>.
        /// </remarks>
        [Pure]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        public static void Assume([DoesNotReturnIf(false)] bool condition, string? userMessage)
        {
            if (!condition)
            {
                ReportFailure(ContractFailureKind.Assume, userMessage, null, null);
            }
        }

        #endregion Assume

        #region Assert

        /// <summary>
        /// In debug builds, perform a runtime check that <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Expression to check to always be true.</param>
        [Pure]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        public static void Assert([DoesNotReturnIf(false)] bool condition)
        {
            if (!condition)
                ReportFailure(ContractFailureKind.Assert, null, null, null);
        }

        /// <summary>
        /// In debug builds, perform a runtime check that <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Expression to check to always be true.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        [Pure]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? userMessage)
        {
            if (!condition)
                ReportFailure(ContractFailureKind.Assert, userMessage, null, null);
        }

        #endregion Assert

        #region Requires

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> must be true before the enclosing method or property is invoked.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// Use this form when backward compatibility does not force you to throw a particular exception.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Requires(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
        }

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> must be true before the enclosing method or property is invoked.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// Use this form when backward compatibility does not force you to throw a particular exception.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Requires(bool condition, string? userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
        }

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> must be true before the enclosing method or property is invoked.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// Use this form when you want to throw a particular exception.
        /// </remarks>
        [Pure]
        public static void Requires<TException>(bool condition) where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
        }

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> must be true before the enclosing method or property is invoked.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// Use this form when you want to throw a particular exception.
        /// </remarks>
        [Pure]
        public static void Requires<TException>(bool condition, string? userMessage) where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
        }

        #endregion Requires

        #region Ensures

        /// <summary>
        /// Specifies a public contract such that the expression <paramref name="condition"/> will be true when the enclosing method or property returns normally.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.  May include <seealso cref="OldValue"/> and <seealso cref="Result"/>.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this postcondition.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Ensures(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
        }

        /// <summary>
        /// Specifies a public contract such that the expression <paramref name="condition"/> will be true when the enclosing method or property returns normally.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.  May include <seealso cref="OldValue"/> and <seealso cref="Result"/>.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference members at least as visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this postcondition.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Ensures(bool condition, string? userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
        }

        /// <summary>
        /// Specifies a contract such that if an exception of type <typeparamref name="TException"/> is thrown then the expression <paramref name="condition"/> will be true when the enclosing method or property terminates abnormally.
        /// </summary>
        /// <typeparam name="TException">Type of exception related to this postcondition.</typeparam>
        /// <param name="condition">Boolean expression representing the contract.  May include <seealso cref="OldValue"/> and <seealso cref="Result"/>.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference types and members at least as visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this postcondition.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition) where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
        }

        /// <summary>
        /// Specifies a contract such that if an exception of type <typeparamref name="TException"/> is thrown then the expression <paramref name="condition"/> will be true when the enclosing method or property terminates abnormally.
        /// </summary>
        /// <typeparam name="TException">Type of exception related to this postcondition.</typeparam>
        /// <param name="condition">Boolean expression representing the contract.  May include <seealso cref="OldValue"/> and <seealso cref="Result"/>.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// This call must happen at the beginning of a method or property before any other code.
        /// This contract is exposed to clients so must only reference types and members at least as visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this postcondition.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition, string? userMessage) where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
        }

        #region Old, Result, and Out Parameters

        /// <summary>
        /// Represents the result (a.k.a. return value) of a method or property.
        /// </summary>
        /// <typeparam name="T">Type of return value of the enclosing method or property.</typeparam>
        /// <returns>Return value of the enclosing method or property.</returns>
        /// <remarks>
        /// This method can only be used within the argument to the <seealso cref="Ensures(bool)"/> contract.
        /// </remarks>
        [Pure]
        public static T Result<T>() { return default!; }

        /// <summary>
        /// Represents the final (output) value of an out parameter when returning from a method.
        /// </summary>
        /// <typeparam name="T">Type of the out parameter.</typeparam>
        /// <param name="value">The out parameter.</param>
        /// <returns>The output value of the out parameter.</returns>
        /// <remarks>
        /// This method can only be used within the argument to the <seealso cref="Ensures(bool)"/> contract.
        /// </remarks>
        [Pure]
        public static T ValueAtReturn<T>(out T value) { value = default!; return value; }

        /// <summary>
        /// Represents the value of <paramref name="value"/> as it was at the start of the method or property.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/>.  This can be inferred.</typeparam>
        /// <param name="value">Value to represent.  This must be a field or parameter.</param>
        /// <returns>Value of <paramref name="value"/> at the start of the method or property.</returns>
        /// <remarks>
        /// This method can only be used within the argument to the <seealso cref="Ensures(bool)"/> contract.
        /// </remarks>
        [Pure]
        public static T OldValue<T>(T value) { return default!; }

        #endregion Old, Result, and Out Parameters

        #endregion Ensures

        #region Invariant

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> will be true after every method or property on the enclosing class.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <remarks>
        /// This contact can only be specified in a dedicated invariant method declared on a class.
        /// This contract is not exposed to clients so may reference members less visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this invariant.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Invariant(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
        }

        /// <summary>
        /// Specifies a contract such that the expression <paramref name="condition"/> will be true after every method or property on the enclosing class.
        /// </summary>
        /// <param name="condition">Boolean expression representing the contract.</param>
        /// <param name="userMessage">If it is not a constant string literal, then the contract may not be understood by tools.</param>
        /// <remarks>
        /// This contact can only be specified in a dedicated invariant method declared on a class.
        /// This contract is not exposed to clients so may reference members less visible as the enclosing method.
        /// The contract rewriter must be used for runtime enforcement of this invariant.
        /// </remarks>
        [Pure]
        [Conditional("CONTRACTS_FULL")]
        public static void Invariant(bool condition, string? userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
        }

        #endregion Invariant

        #region Quantifiers

        #region ForAll

        /// <summary>
        /// Returns whether the <paramref name="predicate"/> returns <c>true</c>
        /// for all integers starting from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.
        /// </summary>
        /// <param name="fromInclusive">First integer to pass to <paramref name="predicate"/>.</param>
        /// <param name="toExclusive">One greater than the last integer to pass to <paramref name="predicate"/>.</param>
        /// <param name="predicate">Function that is evaluated from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.</param>
        /// <returns><c>true</c> if <paramref name="predicate"/> returns <c>true</c> for all integers
        /// starting from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.</returns>
        /// <seealso cref="System.Collections.Generic.List&lt;T&gt;.TrueForAll"/>
        [Pure]
        public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
                throw new ArgumentException(SR.Argument_ToExclusiveLessThanFromExclusive);
            ArgumentNullException.ThrowIfNull(predicate);

            for (int i = fromInclusive; i < toExclusive; i++)
                if (!predicate(i)) return false;
            return true;
        }


        /// <summary>
        /// Returns whether the <paramref name="predicate"/> returns <c>true</c>
        /// for all elements in the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection from which elements will be drawn from to pass to <paramref name="predicate"/>.</param>
        /// <param name="predicate">Function that is evaluated on elements from <paramref name="collection"/>.</param>
        /// <returns><c>true</c> if and only if <paramref name="predicate"/> returns <c>true</c> for all elements in
        /// <paramref name="collection"/>.</returns>
        /// <seealso cref="System.Collections.Generic.List&lt;T&gt;.TrueForAll"/>
        [Pure]
        public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(predicate);

            foreach (T t in collection)
                if (!predicate(t)) return false;
            return true;
        }

        #endregion ForAll

        #region Exists

        /// <summary>
        /// Returns whether the <paramref name="predicate"/> returns <c>true</c>
        /// for any integer starting from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.
        /// </summary>
        /// <param name="fromInclusive">First integer to pass to <paramref name="predicate"/>.</param>
        /// <param name="toExclusive">One greater than the last integer to pass to <paramref name="predicate"/>.</param>
        /// <param name="predicate">Function that is evaluated from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.</param>
        /// <returns><c>true</c> if <paramref name="predicate"/> returns <c>true</c> for any integer
        /// starting from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/> - 1.</returns>
        /// <seealso cref="System.Collections.Generic.List&lt;T&gt;.Exists"/>
        [Pure]
        public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
                throw new ArgumentException(SR.Argument_ToExclusiveLessThanFromExclusive);
            ArgumentNullException.ThrowIfNull(predicate);

            for (int i = fromInclusive; i < toExclusive; i++)
                if (predicate(i)) return true;
            return false;
        }

        /// <summary>
        /// Returns whether the <paramref name="predicate"/> returns <c>true</c>
        /// for any element in the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection from which elements will be drawn from to pass to <paramref name="predicate"/>.</param>
        /// <param name="predicate">Function that is evaluated on elements from <paramref name="collection"/>.</param>
        /// <returns><c>true</c> if and only if <paramref name="predicate"/> returns <c>true</c> for an element in
        /// <paramref name="collection"/>.</returns>
        /// <seealso cref="System.Collections.Generic.List&lt;T&gt;.Exists"/>
        [Pure]
        public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(predicate);

            foreach (T t in collection)
                if (predicate(t)) return true;
            return false;
        }

        #endregion Exists

        #endregion Quantifiers

        #region Misc.

        /// <summary>
        /// Marker to indicate the end of the contract section of a method.
        /// </summary>
        [Conditional("CONTRACTS_FULL")]
        public static void EndContractBlock() { }

        #endregion

        #endregion User Methods

        #region Private Methods

        /// <summary>
        /// This method is used internally to trigger a failure indicating to the "programmer" that they are using the interface incorrectly.
        /// It is NEVER used to indicate failure of actual contracts at runtime.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "StackFrame.GetMethod is only used to help diagnosing incorrect use of contracts. " +
                "It handles missing or incomplete metadata.")]
        private static void AssertMustUseRewriter(ContractFailureKind kind, string contractKind)
        {
            // For better diagnostics, report which assembly is at fault.  Walk up stack and
            // find the first non-mscorlib assembly.
            Assembly thisAssembly = typeof(Contract).Assembly;  // In case we refactor mscorlib, use Contract class instead of Object.
            StackTrace stack = new StackTrace();
            Assembly? probablyNotRewritten = null;
            for (int i = 0; i < stack.FrameCount; i++)
            {
                Assembly? caller = stack.GetFrame(i)!.GetMethod()?.DeclaringType?.Assembly;
                if (caller != null && caller != thisAssembly)
                {
                    probablyNotRewritten = caller;
                    break;
                }
            }

            probablyNotRewritten ??= thisAssembly;
            string? simpleName = probablyNotRewritten.GetName().Name;
            ContractHelper.TriggerFailure(kind, SR.Format(SR.MustUseCCRewrite, contractKind, simpleName), null, null, null);
        }

        #endregion Private Methods

        #region Failure Behavior

        /// <summary>
        /// Without contract rewriting, failing Assert/Assumes end up calling this method.
        /// Code going through the contract rewriter never calls this method. Instead, the rewriter produced failures call
        /// ContractHelper.RaiseContractFailedEvent, followed by ContractHelper.TriggerFailure.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private static void ReportFailure(ContractFailureKind failureKind, string? userMessage, string? conditionText, Exception? innerException)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), nameof(failureKind));

            // displayMessage == null means: yes we handled it. Otherwise it is the localized failure message
            string? displayMessage = ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
            if (displayMessage == null)
                return;

            ContractHelper.TriggerFailure(failureKind, displayMessage, userMessage, conditionText, innerException);
        }

        /// <summary>
        /// Allows a managed application environment such as an interactive interpreter (IronPython)
        /// to be notified of contract failures and
        /// potentially "handle" them, either by throwing a particular exception type, etc.  If any of the
        /// event handlers sets the Cancel flag in the ContractFailedEventArgs, then the Contract class will
        /// not pop up an assert dialog box or trigger escalation policy.  Hooking this event requires
        /// full trust, because it will inform you of bugs in the appdomain and because the event handler
        /// could allow you to continue execution.
        /// </summary>
        public static event EventHandler<ContractFailedEventArgs>? ContractFailed
        {
            add
            {
                ContractHelper.InternalContractFailed += value;
            }
            remove
            {
                ContractHelper.InternalContractFailed -= value;
            }
        }

        #endregion Failure Behavior
    }

    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public enum ContractFailureKind
    {
        Precondition,
        Postcondition,
        PostconditionOnException,
        Invariant,
        Assert,
        Assume,
    }
}  // namespace System.Runtime.CompilerServices
