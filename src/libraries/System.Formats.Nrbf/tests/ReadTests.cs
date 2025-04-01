using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Formats.Nrbf.Tests;

[ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
public abstract class ReadTests
{
    public static bool IsPatched
#if NET
        => true;
#else
        => s_isPatched.Value;

    private static readonly Lazy<bool> s_isPatched = new(GetIsPatched);

    private static bool GetIsPatched()
    {
        Tuple<IComparable, object> tuple = new Tuple<IComparable, object>(42, new byte[] { 1, 2, 3, 4 });
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        using MemoryStream stream = new();

        // This particular scenario is going to throw on Full Framework
        // if given machine has not installed the July 2024 cumulative update preview:
        // https://learn.microsoft.com/dotnet/framework/release-notes/2024/07-25-july-preview-cumulative-update

        try
        {
            formatter.Serialize(stream, tuple);
            stream.Position = 0;
            Tuple<IComparable, object> deserialized = (Tuple<IComparable, object>)formatter.Deserialize(stream);
            return tuple.Item1.Equals(deserialized.Item1);
        }
        catch (Exception)
        {
            return false;
        }
    }
#endif

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

    protected static void WriteSerializedStreamHeader(BinaryWriter writer, int major = 1, int minor = 0, int rootId = 1)
    {
        writer.Write((byte)SerializationRecordType.SerializedStreamHeader);
        writer.Write(rootId); // root ID
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
