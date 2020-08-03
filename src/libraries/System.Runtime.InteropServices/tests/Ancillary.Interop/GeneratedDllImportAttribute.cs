#nullable enable

namespace System.Runtime.InteropServices
{
    // [TODO] Remove once the attribute has been added to the BCL
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
