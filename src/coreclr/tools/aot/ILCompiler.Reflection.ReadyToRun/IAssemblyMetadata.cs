using System;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// This interface represents MSIL information for a single component assembly.
    /// </summary>
    public interface IAssemblyMetadata
    {
        void GetSectionData(int relativeVirtualAddress, Action<BlobReader> action);
        MetadataReader MetadataReader { get;  }
    }
}
