using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class EdgeCaseTests : ReadTests
{
    [Fact]
    public void SurrogatesGetNoSpecialHandling()
    {
#if NET
        // Type is [Serializable] only on Full .NET Framework.
        // So here we use a Base64 representation of serialized typeof(object)
        const string serializedWithFullFramework = "AAEAAAD/////AQAAAAAAAAAEAQAAAB9TeXN0ZW0uVW5pdHlTZXJpYWxpemF0aW9uSG9sZGVyAwAAAAREYXRhCVVuaXR5VHlwZQxBc3NlbWJseU5hbWUBAAEIBgIAAAANU3lzdGVtLk9iamVjdAQAAAAGAwAAAEttc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODkL";

        using MemoryStream stream = new(Convert.FromBase64String(serializedWithFullFramework));
#else
        using MemoryStream stream = Serialize(typeof(object));
#endif

        ClassRecord classRecord = (ClassRecord)NrbfDecoder.Decode(stream);

        // It's a surrogate, so there is no type match.
        Assert.False(classRecord.TypeNameMatches(typeof(Type)));
        Assert.Equal("System.UnitySerializationHolder", classRecord.TypeName.FullName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", classRecord.GetString("AssemblyName"));
    }

    [Theory]
    [InlineData(FormatterTypeStyle.TypesAlways)]
    [InlineData(FormatterTypeStyle.TypesAlways | FormatterTypeStyle.XsdString)]
    public void ArraysOfStringsCanContainMemberReferences(FormatterTypeStyle typeFormat)
    {
        // it has to be the same object, not just the same value
        const string same = "same";
        string[] input = { same, same };

        using MemoryStream stream = new();
        BinaryFormatter binaryFormatter = new()
        {
            TypeFormat = typeFormat
        };
        binaryFormatter.Serialize(stream, input);
        stream.Position = 0;

        string?[] ouput = ((SZArrayRecord<string>)NrbfDecoder.Decode(stream)).GetArray();

        Assert.Equal(input, ouput);
        
        if ((typeFormat & FormatterTypeStyle.XsdString) == 0)
        {
            Assert.Same(ouput[0], ouput[1]);
        }
        else
        {
            Assert.NotSame(ouput[0], ouput[1]);
        }
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
    [InlineData(100)]
    [InlineData(64_001)]
    [InlineData(127_000)]
#if RELEASE && NET // it takes a lot of time to execute
    [InlineData(2147483591)] // Array.MaxLength
#endif
    public void CanReadArrayOfAnySize(int length)
    {
        try
        {
            byte[] input = new byte[length];
            new Random().NextBytes(input);

            // MemoryStream can not handle large array payloads as it's backed by an array.
            using FileStream stream = SerializeToFile(input);

            byte[] output = ((SZArrayRecord<byte>)NrbfDecoder.Decode(stream)).GetArray();
            Assert.Equal(input, output);
        }
        catch (OutOfMemoryException) when (length == 2147483591)
        {
            throw new SkipTestException("Not enough memory available to test max array size support");
        }
    }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
    [Theory]
    [InlineData(FormatterTypeStyle.TypesWhenNeeded)]
    [InlineData(FormatterTypeStyle.XsdString)]
    public void FormatterTypeStyleOtherThanTypesAlwaysAreNotSupportedByDesign(FormatterTypeStyle typeFormat)
    {
        using MemoryStream ms = new();
        BinaryFormatter binaryFormatter = new()
        {
            TypeFormat = typeFormat
        };
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        binaryFormatter.Serialize(ms, true);
        ms.Position = 0;

        Assert.Throws<NotSupportedException>(() => NrbfDecoder.Decode(ms));
    }
}
