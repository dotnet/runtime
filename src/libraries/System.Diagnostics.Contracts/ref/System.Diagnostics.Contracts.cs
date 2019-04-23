// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics.Contracts
{
    public static partial class Contract
    {
        public static event System.EventHandler<System.Diagnostics.Contracts.ContractFailedEventArgs> ContractFailed { add { } remove { } }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assert(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assert(bool condition, string userMessage) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assume(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assume(bool condition, string userMessage) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EndContractBlock() { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Ensures(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Ensures(bool condition, string userMessage) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition) where TException : System.Exception { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition, string userMessage) where TException : System.Exception { }
        public static bool Exists(int fromInclusive, int toExclusive, System.Predicate<int> predicate) { throw null; }
        public static bool Exists<T>(System.Collections.Generic.IEnumerable<T> collection, System.Predicate<T> predicate) { throw null; }
        public static bool ForAll(int fromInclusive, int toExclusive, System.Predicate<int> predicate) { throw null; }
        public static bool ForAll<T>(System.Collections.Generic.IEnumerable<T> collection, System.Predicate<T> predicate) { throw null; }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Invariant(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Invariant(bool condition, string userMessage) { }
        public static T OldValue<T>(T value) { throw null; }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Requires(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Requires(bool condition, string userMessage) { }
        public static void Requires<TException>(bool condition) where TException : System.Exception { }
        public static void Requires<TException>(bool condition, string userMessage) where TException : System.Exception { }
        public static T Result<T>() { throw null; }
        public static T ValueAtReturn<T>(out T value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractAbbreviatorAttribute : System.Attribute
    {
        public ContractAbbreviatorAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractArgumentValidatorAttribute : System.Attribute
    {
        public ContractArgumentValidatorAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Delegate | System.AttributeTargets.Interface, AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    [System.Diagnostics.ConditionalAttribute("DEBUG")]
    public sealed partial class ContractClassAttribute : System.Attribute
    {
        public ContractClassAttribute(System.Type typeContainingContracts) { }
        public System.Type TypeContainingContracts { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractClassForAttribute : System.Attribute
    {
        public ContractClassForAttribute(System.Type typeContractsAreFor) { }
        public System.Type TypeContractsAreFor { get { throw null; } }
    }
    public sealed partial class ContractFailedEventArgs : System.EventArgs
    {
        public ContractFailedEventArgs(System.Diagnostics.Contracts.ContractFailureKind failureKind, string message, string condition, System.Exception originalException) { }
        public string Condition { get { throw null; } }
        public System.Diagnostics.Contracts.ContractFailureKind FailureKind { get { throw null; } }
        public bool Handled { get { throw null; } }
        public string Message { get { throw null; } }
        public System.Exception OriginalException { get { throw null; } }
        public bool Unwind { get { throw null; } }
        public void SetHandled() { }
        public void SetUnwind() { }
    }
    public enum ContractFailureKind
    {
        Precondition = 0,
        Postcondition = 1,
        PostconditionOnException = 2,
        Invariant = 3,
        Assert = 4,
        Assume = 5,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractInvariantMethodAttribute : System.Attribute
    {
        public ContractInvariantMethodAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All, AllowMultiple=true, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractOptionAttribute : System.Attribute
    {
        public ContractOptionAttribute(string category, string setting, bool enabled) { }
        public ContractOptionAttribute(string category, string setting, string value) { }
        public string Category { get { throw null; } }
        public bool Enabled { get { throw null; } }
        public string Setting { get { throw null; } }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractPublicPropertyNameAttribute : System.Attribute
    {
        public ContractPublicPropertyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly)]
    public sealed partial class ContractReferenceAssemblyAttribute : System.Attribute
    {
        public ContractReferenceAssemblyAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractRuntimeIgnoredAttribute : System.Attribute
    {
        public ContractRuntimeIgnoredAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Struct)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractVerificationAttribute : System.Attribute
    {
        public ContractVerificationAttribute(bool value) { }
        public bool Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Delegate | System.AttributeTargets.Event | System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class PureAttribute : System.Attribute
    {
        public PureAttribute() { }
    }
}
namespace System.Runtime.CompilerServices
{
    public static partial class ContractHelper
    {
        public static string RaiseContractFailedEvent(System.Diagnostics.Contracts.ContractFailureKind failureKind, string userMessage, string conditionText, System.Exception innerException) { throw null; }
        public static void TriggerFailure(System.Diagnostics.Contracts.ContractFailureKind kind, string displayMessage, string userMessage, string conditionText, System.Exception innerException) { }
    }
}
