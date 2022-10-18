using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BinaryFormat
{
    public partial class Generator : ISourceGenerator
    {
        private const string AttributeSource = @"
#nullable disable

[System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
internal sealed class GenerateReaderWriterAttribute : System.Attribute
{
    public GenerateReaderWriterAttribute() {}
}

[System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
internal sealed class BigEndianAttribute : System.Attribute
{
    public BigEndianAttribute() {}
}

[System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
internal sealed class LittleEndianAttribute : System.Attribute
{
    public LittleEndianAttribute() {}
}
";
    }
}
