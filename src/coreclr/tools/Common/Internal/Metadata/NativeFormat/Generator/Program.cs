// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class Program
{
    static void Main(string[] args)
    {
        using (var writer = new PublicGen(@"NativeFormatReaderCommonGen.cs"))
        {
            writer.EmitSource();
        }

        using (var writer = new ReaderGen(@"NativeFormatReaderGen.cs"))
        {
            writer.EmitSource();
        }

        using (var writer = new WriterGen(@"..\..\..\..\aot\ILCompiler.MetadataTransform\Internal\Metadata\NativeFormat\Writer\NativeFormatWriterGen.cs"))
        {
            writer.EmitSource();
        }

        using (var writer = new MdBinaryReaderGen(@"MdBinaryReaderGen.cs"))
        {
            writer.EmitSource();
        }

        using (var writer = new MdBinaryWriterGen(@"..\..\..\..\aot\ILCompiler.MetadataTransform\Internal\Metadata\NativeFormat\Writer\MdBinaryWriterGen.cs"))
        {
            writer.EmitSource();
        }
    }
}
