
namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicates that method will be generated at compile time and invoke into an unmanaged library entry point
    /// </summary>
    /// <remarks>
    /// IL linker/trimming currently has special handling of P/Invokes (pinvokeimpl):
    ///   - https://github.com/mono/linker/blob/bfab847356063d21eb15e79f2b6c03df5bd6ef3d/src/linker/Linker.Steps/MarkStep.cs#L2623
    /// We may want to make the linker aware of this attribute as well.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratedDllImportAttribute : Attribute
    {
        public bool BestFitMapping;
        public CallingConvention CallingConvention;
        public CharSet CharSet;
        public string? EntryPoint;
        public bool ExactSpelling;
        public bool PreserveSig;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;

        public GeneratedDllImportAttribute(string dllName)
        {
            this.Value = dllName;
        }

        public string Value { get; private set; }
    }
}
