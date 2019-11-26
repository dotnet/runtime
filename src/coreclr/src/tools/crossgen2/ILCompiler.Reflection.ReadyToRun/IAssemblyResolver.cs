using System;
using System.Collections.Generic;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public interface IAssemblyResolver
    {
        string FindAssembly(string name, string filename);
        // TODO (refactoring) - signature formatting options should be independent of assembly resolver
        bool Naked { get; }
        bool SignatureBinary { get; }
        bool InlineSignatureBinary { get; }
    }
}
