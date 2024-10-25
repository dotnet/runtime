using System.Text.RegularExpressions;

namespace WebAssemblyInfo
{

        public class WasmContext
        {
                public int VerboseLevel;
                public bool Verbose { get { return VerboseLevel > 0; } }
                public bool Verbose2 { get { return VerboseLevel > 1; } }
                public bool AotStats;
                public bool Disassemble;
                public Regex? FunctionFilter;
                public long FunctionOffset = -1;
                public bool PrintOffsets;
                public bool ShowFunctionSize;
                public bool ShowConstLoad = true;
        }
}
