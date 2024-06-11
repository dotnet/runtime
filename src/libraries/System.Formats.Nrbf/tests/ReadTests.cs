using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Formats.Nrbf.Tests;

[ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
public abstract class ReadTests
{
    protected static MemoryStream Serialize<T>(T instance) where T : notnull
    {
        MemoryStream ms = new();

        CreateBinaryFormatter().Serialize(ms, instance);

        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Useful for very large inputs
    /// </summary>
    protected static FileStream SerializeToFile<T>(T instance) where T : notnull
    {
        FileStream fs = new(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, 
            FileShare.None, bufferSize: 100_000, FileOptions.DeleteOnClose);

        CreateBinaryFormatter().Serialize(fs, instance);

        fs.Flush();
        fs.Position = 0;
        return fs;
    }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
    protected static BinaryFormatter CreateBinaryFormatter()
        => new()
        {
#if DEBUG // Ensure both valid formats are covered by the tests
            TypeFormat = FormatterTypeStyle.TypesAlways | FormatterTypeStyle.XsdString,
#else
            TypeFormat = FormatterTypeStyle.TypesAlways
#endif
        };
#pragma warning restore SYSLIB0011 // Type or member is obsolete

    protected static void WriteSerializedStreamHeader(BinaryWriter writer, int major = 1, int minor = 0)
    {
        writer.Write((byte)SerializationRecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }

    protected static void WriteBinaryLibrary(BinaryWriter writer, int objectId, string libraryName)
    {
        writer.Write((byte)SerializationRecordType.BinaryLibrary);
        writer.Write(objectId);
        writer.Write(libraryName);
    }
}
