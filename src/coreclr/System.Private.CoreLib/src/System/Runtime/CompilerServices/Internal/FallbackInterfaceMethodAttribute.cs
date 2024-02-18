namespace System.Runtime.CompilerServices.Internal
{
    /// <summary>
    /// INTERNAL: Make default Interface methods have "low priority" in case there are multiple 
    /// possible implementations (the "Diamond dependency problem"), 
    /// ensuring that any other conflicting implementaion will be selected at runtime.
    /// </summary>
    /// <remarks>
    /// This allows adding default method implementations for existing interfaces without 
    /// making it a binary breaking change. (It can still be a source breaking change)
    /// <para>
    /// Should preferably only be used in the same assembly which defines 
    /// the interface method beeing overridden.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    sealed class FallbackInterfaceMethodAttribute : Attribute
    {
    }
}
