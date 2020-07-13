using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DllImportGenerator.Test
{
    internal static class CodeSnippets
    {
        /// <summary>
        /// Trivial declaration of GeneratedDllImport usage
        /// </summary>
        public static readonly string TrivialClassDeclarations = @"
using System.Runtime.InteropServices;
partial class Basic
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2();

    [System.Runtime.InteropServices.GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method3();

    [System.Runtime.InteropServices.GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method4();
}
";
        /// <summary>
        /// Trivial declaration of GeneratedDllImport usage
        /// </summary>
        public static readonly string TrivialStructDeclarations = @"
using System.Runtime.InteropServices;
partial struct Basic
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2();

    [System.Runtime.InteropServices.GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method3();

    [System.Runtime.InteropServices.GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method4();
}
";

        /// <summary>
        /// Declaration with multiple attributes
        /// </summary>
        public static readonly string MultipleAttributes = @"
using System;
using System.Runtime.InteropServices;

sealed class DummyAttribute : Attribute
{
    public DummyAttribute() { }
}

sealed class Dummy2Attribute : Attribute
{
    public Dummy2Attribute(string input) { }
}

partial class Test
{
    [DummyAttribute]
    [GeneratedDllImport(""DoesNotExist""), Dummy2Attribute(""string value"")]
    public static partial void Method();
}
";

        /// <summary>
        /// Validate nested namespaces are handled
        /// </summary>
        public static readonly string NestedNamespace = @"
using System.Runtime.InteropServices;
namespace NS
{
    namespace InnerNS
    {
        partial class Test
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method1();
        }
    }
}
namespace NS.InnerNS
{
    partial class Test
    {
        [GeneratedDllImport(""DoesNotExist"")]
        public static partial void Method2();
    }
}
";

        /// <summary>
        /// Validate nested types are handled.
        /// </summary>
        public static readonly string NestedTypes = @"
using System.Runtime.InteropServices;
namespace NS
{
    partial class OuterClass
    {
        partial class InnerClass
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial struct OuterStruct
    {
        partial struct InnerStruct
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial class OuterClass
    {
        partial struct InnerStruct
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial struct OuterStruct
    {
        partial class InnerClass
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
}
";

        /// <summary>
        /// Declaration with user defined EntryPoint.
        /// </summary>
        public static readonly string UserDefinedEntryPoint = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint=""UserDefinedEntryPoint"")]
    public static partial void NotAnExport();
}
";

        /// <summary>
        /// Declaration with basic parameters.
        /// </summary>
        public static readonly string BasicParametersAndModifiers = @"
using System;
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method1(string s, IntPtr i, UIntPtr u);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2(in string s, ref IntPtr i, out UIntPtr u);
}
";

        /// <summary>
        /// Declaration with default parameters.
        /// </summary>
        public static readonly string DefaultParameters = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(int t = 0);
}
";
    }
}
