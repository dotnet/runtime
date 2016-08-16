// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#if SILVERLIGHT
#define FEATURE_UNTRUSTED_CALLERS

#elif REDHAWK_RUNTIME

#elif BARTOK_RUNTIME

#else // CLR
#define FEATURE_UNTRUSTED_CALLERS
#define FEATURE_RELIABILITY_CONTRACTS
#define FEATURE_SERIALIZATION
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

#if FEATURE_RELIABILITY_CONTRACTS
using System.Runtime.ConstrainedExecution;
#endif
#if FEATURE_UNTRUSTED_CALLERS
using System.Security;
using System.Security.Permissions;
#endif

namespace System.Diagnostics.Contracts {

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
        private Type _typeWithContracts;

        public ContractClassAttribute(Type typeContainingContracts)
        {
            _typeWithContracts = typeContainingContracts;
        }

        public Type TypeContainingContracts {
            get { return _typeWithContracts; }
        }
    }

    /// <summary>
    /// Types marked with this attribute specify that they are a contract for the type that is the argument of the constructor.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ContractClassForAttribute : Attribute
    {
        private Type _typeIAmAContractFor;

        public ContractClassForAttribute(Type typeContractsAreFor)
        {
            _typeIAmAContractFor = typeContractsAreFor;
        }

        public Type TypeContractsAreFor {
            get { return _typeIAmAContractFor; }
        }
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

    /*
    [Serializable]
    internal enum Mutability
    {
        Immutable,    // read-only after construction, except for lazy initialization & caches
        // Do we need a "deeply immutable" value?
        Mutable,
        HasInitializationPhase,  // read-only after some point.  
        // Do we need a value for mutable types with read-only wrapper subclasses?
    }

    // Note: This hasn't been thought through in any depth yet.  Consider it experimental.
    // We should replace this with Joe's concepts.
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Thank you very much, but we like the names we've defined for the accessors")]
    internal sealed class MutabilityAttribute : Attribute 
    {
        private Mutability _mutabilityMarker;

        public MutabilityAttribute(Mutability mutabilityMarker)
        {
            _mutabilityMarker = mutabilityMarker;
        }

        public Mutability Mutability {
            get { return _mutabilityMarker; }
        }
    }
    */

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
        private bool _value;

        public ContractVerificationAttribute(bool value) { _value = value; }

        public bool Value {
            get { return _value; }
        }
    }

    /// <summary>
    /// Allows a field f to be used in the method contracts for a method m when f has less visibility than m.
    /// For instance, if the method is public, but the field is private.
    /// </summary>
    [Conditional("CONTRACTS_FULL")]
    [AttributeUsage(AttributeTargets.Field)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Thank you very much, but we like the names we've defined for the accessors")]
    public sealed class ContractPublicPropertyNameAttribute : Attribute
    {
        private String _publicName;

        public ContractPublicPropertyNameAttribute(String name)
        {
            _publicName = name;
        }

        public String Name {
            get { return _publicName; }
        }
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
        private String _category;
        private String _setting;
        private bool _enabled;
        private String _value;

        public ContractOptionAttribute(String category, String setting, bool enabled)
        {
            _category = category;
            _setting = setting;
            _enabled = enabled;
        }

        public ContractOptionAttribute(String category, String setting, String value)
        {
            _category = category;
            _setting = setting;
            _value = value;
        }

        public String Category {
            get { return _category; }
        }

        public String Setting {
            get { return _setting; }
        }

        public bool Enabled {
            get { return _enabled; }
        }

        public String Value {
            get { return _value; }
        }
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Assume(bool condition)
        {
            if (!condition) {
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Assume(bool condition, String userMessage)
        {
            if (!condition) {
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Assert(bool condition)
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Assert(bool condition, String userMessage)
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Requires(bool condition, String userMessage)
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
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "condition")]
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [Pure]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "userMessage")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "condition")]
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [Pure]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Requires<TException>(bool condition, String userMessage) where TException : Exception
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Ensures(bool condition, String userMessage)
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
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Exception type used in tools.")]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Exception type used in tools.")]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void EnsuresOnThrow<TException>(bool condition, String userMessage) where TException : Exception
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
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Not intended to be called at runtime.")]
        [Pure]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static T Result<T>() { return default(T); }

        /// <summary>
        /// Represents the final (output) value of an out parameter when returning from a method.
        /// </summary>
        /// <typeparam name="T">Type of the out parameter.</typeparam>
        /// <param name="value">The out parameter.</param>
        /// <returns>The output value of the out parameter.</returns>
        /// <remarks>
        /// This method can only be used within the argument to the <seealso cref="Ensures(bool)"/> contract.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#", Justification = "Not intended to be called at runtime.")]
        [Pure]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static T ValueAtReturn<T>(out T value) { value = default(T); return value; }

        /// <summary>
        /// Represents the value of <paramref name="value"/> as it was at the start of the method or property.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/>.  This can be inferred.</typeparam>
        /// <param name="value">Value to represent.  This must be a field or parameter.</param>
        /// <returns>Value of <paramref name="value"/> at the start of the method or property.</returns>
        /// <remarks>
        /// This method can only be used within the argument to the <seealso cref="Ensures(bool)"/> contract.
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        [Pure]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static T OldValue<T>(T value) { return default(T); }

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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void Invariant(bool condition, String userMessage)
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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]  // Assumes predicate obeys CER rules.
#endif
        public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
#if INSIDE_CLR
                throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
#else
                throw new ArgumentException("fromInclusive must be less than or equal to toExclusive.");
#endif
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();

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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]  // Assumes predicate & collection enumerator obey CER rules.
#endif
        public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();

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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]  // Assumes predicate obeys CER rules.
#endif
        public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
#if INSIDE_CLR
                throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
#else
                throw new ArgumentException("fromInclusive must be less than or equal to toExclusive.");
#endif
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();

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
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]  // Assumes predicate & collection enumerator obey CER rules.
#endif
        public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();

            foreach (T t in collection)
                if (predicate(t)) return true;
            return false;
        }

        #endregion Exists

        #endregion Quantifiers

        #region Pointers
#if FEATURE_UNSAFE_CONTRACTS    
        /// <summary>
        /// Runtime checking for pointer bounds is not currently feasible. Thus, at runtime, we just return
        /// a very long extent for each pointer that is writable. As long as assertions are of the form
        /// WritableBytes(ptr) >= ..., the runtime assertions will not fail.
        /// The runtime value is 2^64 - 1 or 2^32 - 1.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1802", Justification = "FxCop is confused")]
        static readonly ulong MaxWritableExtent = (UIntPtr.Size == 4) ? UInt32.MaxValue : UInt64.MaxValue;

        /// <summary>
        /// Allows specifying a writable extent for a UIntPtr, similar to SAL's writable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes writable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static ulong WritableBytes(UIntPtr startAddress) { return MaxWritableExtent - startAddress.ToUInt64(); }

        /// <summary>
        /// Allows specifying a writable extent for a UIntPtr, similar to SAL's writable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes writable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static ulong WritableBytes(IntPtr startAddress) { return MaxWritableExtent - (ulong)startAddress; }
        
        /// <summary>
        /// Allows specifying a writable extent for a UIntPtr, similar to SAL's writable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes writable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
        [SecurityCritical]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif

        unsafe public static ulong WritableBytes(void* startAddress) { return MaxWritableExtent - (ulong)startAddress; }

        /// <summary>
        /// Allows specifying a readable extent for a UIntPtr, similar to SAL's readable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes readable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static ulong ReadableBytes(UIntPtr startAddress) { return MaxWritableExtent - startAddress.ToUInt64(); }

        /// <summary>
        /// Allows specifying a readable extent for a UIntPtr, similar to SAL's readable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes readable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static ulong ReadableBytes(IntPtr startAddress) { return MaxWritableExtent - (ulong)startAddress; }

        /// <summary>
        /// Allows specifying a readable extent for a UIntPtr, similar to SAL's readable extent.
        /// NOTE: this is for static checking only. No useful runtime code can be generated for this
        /// at the moment.
        /// </summary>
        /// <param name="startAddress">Start of memory region</param>
        /// <returns>The result is the number of bytes readable starting at <paramref name="startAddress"/></returns>
        [CLSCompliant(false)]
        [Pure]
        [ContractRuntimeIgnored]
        [SecurityCritical]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        unsafe public static ulong ReadableBytes(void* startAddress) { return MaxWritableExtent - (ulong)startAddress; }
#endif // FEATURE_UNSAFE_CONTRACTS
        #endregion

        #region Misc.

        /// <summary>
        /// Marker to indicate the end of the contract section of a method.
        /// </summary>
        [Conditional("CONTRACTS_FULL")]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void EndContractBlock() { }

        #endregion

        #endregion User Methods

        #region Failure Behavior

        /// <summary>
        /// Without contract rewriting, failing Assert/Assumes end up calling this method.
        /// Code going through the contract rewriter never calls this method. Instead, the rewriter produced failures call
        /// System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent, followed by 
        /// System.Runtime.CompilerServices.ContractHelper.TriggerFailure.
        /// </summary>
        static partial void ReportFailure(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException);

        /// <summary>
        /// This method is used internally to trigger a failure indicating to the "programmer" that he is using the interface incorrectly.
        /// It is NEVER used to indicate failure of actual contracts at runtime.
        /// </summary>
        static partial void AssertMustUseRewriter(ContractFailureKind kind, String contractKind);

        #endregion
    }

    public enum ContractFailureKind {
        Precondition,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Postcondition")]
        Postcondition,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Postcondition")]
        PostconditionOnException,
        Invariant,
        Assert,
        Assume,
    }


}

// Note: In .NET FX 4.5, we duplicated the ContractHelper class in the System.Runtime.CompilerServices
// namespace to remove an ugly wart of a namespace from the Windows 8 profile.  But we still need the
// old locations left around, so we can support rewritten .NET FX 4.0 libraries.  Consider removing
// these from our reference assembly in a future version.
namespace System.Diagnostics.Contracts.Internal
{
    [Obsolete("Use the ContractHelper class in the System.Runtime.CompilerServices namespace instead.")]
    public static class ContractHelper
    {
        #region Rewriter Failure Hooks

        /// <summary>
        /// Rewriter will call this method on a contract failure to allow listeners to be notified.
        /// The method should not perform any failure (assert/throw) itself.
        /// </summary>
        /// <returns>null if the event was handled and should not trigger a failure.
        ///          Otherwise, returns the localized failure message</returns>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static string RaiseContractFailedEvent(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            return System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
        }

        /// <summary>
        /// Rewriter calls this method to get the default failure behavior.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void TriggerFailure(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(kind, displayMessage, userMessage, conditionText, innerException);
        }

        #endregion Rewriter Failure Hooks
    }
}  // namespace System.Diagnostics.Contracts.Internal


namespace System.Runtime.CompilerServices
{
    public static partial class ContractHelper
    {
        #region Rewriter Failure Hooks

        /// <summary>
        /// Rewriter will call this method on a contract failure to allow listeners to be notified.
        /// The method should not perform any failure (assert/throw) itself.
        /// </summary>
        /// <returns>null if the event was handled and should not trigger a failure.
        ///          Otherwise, returns the localized failure message</returns>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static string RaiseContractFailedEvent(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            var resultFailureMessage = "Contract failed"; // default in case implementation does not assign anything.
            RaiseContractFailedEventImplementation(failureKind, userMessage, conditionText, innerException, ref resultFailureMessage);
            return resultFailureMessage;
        }


        /// <summary>
        /// Rewriter calls this method to get the default failure behavior.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void TriggerFailure(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            TriggerFailureImplementation(kind, displayMessage, userMessage, conditionText, innerException);
        }

        #endregion Rewriter Failure Hooks

        #region Implementation Stubs

        /// <summary>
        /// Rewriter will call this method on a contract failure to allow listeners to be notified.
        /// The method should not perform any failure (assert/throw) itself.
        /// This method has 3 functions:
        /// 1. Call any contract hooks (such as listeners to Contract failed events)
        /// 2. Determine if the listeneres deem the failure as handled (then resultFailureMessage should be set to null)
        /// 3. Produce a localized resultFailureMessage used in advertising the failure subsequently.
        /// </summary>
        /// <param name="resultFailureMessage">Should really be out (or the return value), but partial methods are not flexible enough.
        /// On exit: null if the event was handled and should not trigger a failure.
        ///          Otherwise, returns the localized failure message</param>
        static partial void RaiseContractFailedEventImplementation(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException, ref string resultFailureMessage);

        /// <summary>
        /// Implements the default failure behavior of the platform. Under the BCL, it triggers an Assert box.
        /// </summary>
        static partial void TriggerFailureImplementation(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException);

        #endregion Implementation Stubs
    }
}  // namespace System.Runtime.CompilerServices

