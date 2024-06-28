// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace BinaryFormatTests.FormatterTests;

[ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
public partial class BinaryFormatterTests
{
    [Theory]
    [MemberData(nameof(SerializableObjects_MemberData))]
    public void ValidateAgainstBlobs(object obj, TypeSerializableValue[] blobs)
        => ValidateAndRoundtrip(obj, blobs, isEqualityComparer: false);

    [Theory]
    [MemberData(nameof(SerializableEqualityComparers_MemberData))]
    public void ValidateEqualityComparersAgainstBlobs(object obj, TypeSerializableValue[] blobs)
        => ValidateAndRoundtrip(obj, blobs, isEqualityComparer: true);

    private static void ValidateAndRoundtrip(object obj, TypeSerializableValue[] blobs, bool isEqualityComparer)
    {
        // EqualityExtensions.CheckEquals enumerates over all properties of given type.
        // Some of the properties might be lazy evaluated and stored in a field.
        // BF is storing all the fields. It means that serializing given object instance
        // before and after passing it to EqualityExtensions.CheckEquals may produce different blobs!
        // Since this test is comparing these blobs, we call this method up-front to ensure
        // all properties (and fields) get pre-evaluated before doing the serialize-deserialize roundtrip.
        EqualityExtensions.CheckEquals(obj, obj);

        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj), "The serializable object must not be null");
        }

        if (blobs is null || blobs.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"Type {obj} has no blobs to deserialize and test equality against. Blob: " +
                BinaryFormatterHelpers.ToBase64String(obj, FormatterAssemblyStyle.Full));
        }

        // Check if the passed in value in a serialization entry is assignable by the passed in type.
        if (obj is ISerializable serializable)
        {
            CheckObjectTypeIntegrity(serializable);
        }

        // TimeZoneInfo objects have three properties (DisplayName, StandardName, DaylightName)
        // that are localized.  Since the blobs were generated from the invariant culture, they
        // will have English strings embedded.  Thus, we can only test them against English
        // language cultures or the invariant culture.
        if (obj is TimeZoneInfo && (
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "en" ||
            CultureInfo.CurrentUICulture.Name.Length != 0))
        {
            return;
        }

        SanityCheckBlob(obj, blobs);

        if (!isEqualityComparer)
        {
            // Test moved to BasicObjectTests
            return;
        }

        // ReflectionTypeLoadException and LicenseException aren't deserializable from Desktop --> Core.
        // Therefore we remove the second blob which is the one from Desktop.
        if (obj is ReflectionTypeLoadException or LicenseException)
        {
            var tmpList = new List<TypeSerializableValue>(blobs);
            tmpList.RemoveAt(1);

            int index = tmpList.FindIndex(b => b.Platform.ToString().StartsWith("netfx", StringComparison.Ordinal));
            if (index >= 0)
                tmpList.RemoveAt(index);

            blobs = [.. tmpList];
        }

        // We store our framework blobs in index 1
        int platformBlobIndex = blobs.GetPlatformIndex();
        for (int i = 0; i < blobs.Length; i++)
        {
            // Check if the current blob is from the current running platform.
            bool isSamePlatform = i == platformBlobIndex;

            ValidateEqualityComparer(BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Simple));
            ValidateEqualityComparer(BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Full));
        }
    }

    [Fact]
    public void RegexExceptionSerializable()
    {
        try
        {
#pragma warning disable RE0001 // Regex issue: {0}
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
            _ = new Regex("*"); // parsing "*" - Quantifier {x,y} following nothing.
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
#pragma warning restore RE0001 // Regex issue: {0}
        }
        catch (ArgumentException ex)
        {
            Assert.Equal("RegexParseException", ex.GetType().Name);
            ArgumentException clone = BinaryFormatterHelpers.Clone(ex);
            Assert.IsType<ArgumentException>(clone);
        }
    }

    [Fact]
    public void ArraySegmentDefaultCtor()
    {
        // This is workaround for Xunit bug which tries to pretty print test case name and enumerate this object.
        // When inner array is not initialized it throws an exception when this happens.
        object obj = new ArraySegment<int>();
        string corefxBlob = "AAEAAAD/////AQAAAAAAAAAEAQAAAHJTeXN0ZW0uQXJyYXlTZWdtZW50YDFbW1N5c3RlbS5JbnQzMiwgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0DAAAABl9hcnJheQdfb2Zmc2V0Bl9jb3VudAcAAAgICAoAAAAAAAAAAAs=";
        string netfxBlob = "AAEAAAD/////AQAAAAAAAAAEAQAAAHJTeXN0ZW0uQXJyYXlTZWdtZW50YDFbW1N5c3RlbS5JbnQzMiwgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0DAAAABl9hcnJheQdfb2Zmc2V0Bl9jb3VudAcAAAgICAoAAAAAAAAAAAs=";
        EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(corefxBlob, FormatterAssemblyStyle.Full), isSamePlatform: true);
        EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(netfxBlob, FormatterAssemblyStyle.Full), isSamePlatform: true);
    }

    [Theory]
    [MemberData(nameof(NonSerializableTypes_MemberData))]
    public void ValidateNonSerializableTypes(object obj, FormatterAssemblyStyle assemblyFormat, FormatterTypeStyle typeFormat)
    {
        var f = new BinaryFormatter()
        {
            AssemblyFormat = assemblyFormat,
            TypeFormat = typeFormat
        };

        using var s = new MemoryStream();
        Assert.Throws<SerializationException>(() => f.Serialize(s, obj));
    }

    [Fact]
    public void SerializeDeserialize_InvalidArguments_ThrowsException()
    {
        var f = new BinaryFormatter();
        Assert.Throws<ArgumentNullException>(() => f.Serialize(null!, new object()));
        Assert.Throws<ArgumentNullException>(() => f.Deserialize(null!));
        Assert.Throws<SerializationException>(() => f.Deserialize(new MemoryStream())); // seekable, 0-length
    }

    [Fact]
    public void ObjectReference_RealObjectSerialized()
    {
        var obj = new ObjRefReturnsObj { Real = 42 };
        object real = BinaryFormatterHelpers.Clone<object>(obj);
        Assert.Equal(42, real);
    }

    [Fact]
    public void Deserialize_EndOfStream_ThrowsException()
    {
        var f = new BinaryFormatter();
        var s = new MemoryStream();
        f.Serialize(s, 1024);

        for (long i = s.Length - 1; i >= 0; i--)
        {
            s.Position = 0;
            byte[] data = new byte[i];
            Assert.Equal(data.Length, s.Read(data, 0, data.Length));
            Assert.Throws<SerializationException>(() => f.Deserialize(new MemoryStream(data)));
        }
    }

    private static void ValidateEqualityComparer(object obj)
    {
        Type objType = obj.GetType();
        Assert.True(objType.IsGenericType, $"Type `{objType.FullName}` must be generic.");
        Assert.Equal("System.Collections.Generic.ObjectEqualityComparer`1", objType.GetGenericTypeDefinition().FullName);
        Assert.Equal(obj.GetType().GetGenericArguments()[0], objType.GetGenericArguments()[0]);
    }

    private static void CheckObjectTypeIntegrity(ISerializable serializable)
    {
        SerializationInfo testData = new(serializable.GetType(), new FormatterConverter());
        serializable.GetObjectData(testData, new StreamingContext(StreamingContextStates.Other));

        foreach (SerializationEntry entry in testData)
        {
            if (entry.Value is not null)
            {
                Assert.IsAssignableFrom(entry.ObjectType, entry.Value);
            }
        }
    }

    private static void SanityCheckBlob(object obj, TypeSerializableValue[] blobs)
    {
        // These types are unstable during serialization and produce different blobs.
        string name = obj.GetType().FullName!;
        if (obj is WeakReference<Point>
            || obj is System.Collections.Specialized.HybridDictionary
            || obj is Color
            || name == "System.Collections.SortedList+SyncSortedList"

            // Due to non-deterministic field ordering the types below will fail when using IL Emit-based Invoke.
            // The types above may also be failing for the same reason.
            // Remove these cases once https://github.com/dotnet/runtime/issues/46272 is fixed.
            || name == "System.Collections.Comparer"
            || name == "System.Collections.Hashtable"
            || name == "System.Collections.SortedList"
            || name == "System.Collections.Specialized.ListDictionary"
            || name == "System.CultureAwareComparer"
            || name == "System.Globalization.CompareInfo"
            || name == "System.Net.Cookie"
            || name == "System.Net.CookieCollection"
            || name == "System.Net.CookieContainer")
        {
            return;
        }

        // The blobs aren't identical because of different implementations on Unix vs. Windows.
        if (obj is Bitmap or Icon or Metafile)
        {
            return;
        }

        // In most cases exceptions in Core have a different layout than in Desktop,
        // therefore we are skipping the string comparison of the blobs.
        if (obj is Exception)
        {
            return;
        }

        // Check if runtime generated blob is the same as the stored one
        int frameworkBlobNumber = blobs.GetPlatformIndex();
        if (frameworkBlobNumber < blobs.Length)
        {
            string runtimeBlob = BinaryFormatterHelpers.ToBase64String(obj, FormatterAssemblyStyle.Full);

            string storedComparableBlob = CreateComparableBlobInfo(blobs[frameworkBlobNumber].Base64Blob);
            string runtimeComparableBlob = CreateComparableBlobInfo(runtimeBlob);

            if (storedComparableBlob != runtimeComparableBlob)
            {
                Debug.WriteLine($"NEW BLOB {obj.GetType().FullName}: {runtimeBlob}");
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }

            Assert.True(storedComparableBlob == runtimeComparableBlob, $"""
                The stored blob with index {frameworkBlobNumber} for type {obj.GetType().FullName} is outdated and needs to be updated.

                -------------------- Stored blob ---------------------
                Encoded: {blobs[frameworkBlobNumber].Base64Blob}
                Decoded: {storedComparableBlob}

                --------------- Runtime generated blob ---------------
                Encoded: {runtimeBlob}
                Decoded: {runtimeComparableBlob}
                """);
        }
    }

    private static string CreateComparableBlobInfo(string base64Blob)
    {
        string lineSeparator = ((char)0x2028).ToString();
        string paragraphSeparator = ((char)0x2029).ToString();

        byte[] data = Convert.FromBase64String(base64Blob);
        base64Blob = Encoding.UTF8.GetString(data);

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        return Regex.Replace(base64Blob, @"Version=\d.\d.\d.\d.", "Version=0.0.0.0", RegexOptions.Multiline)
            // Ignore the old Test key and Open public keys.
            .Replace("PublicKeyToken=cc7b13ffcd2ddd51", "PublicKeyToken=null")
            .Replace("PublicKeyToken=9d77cc7ad39b68eb", "PublicKeyToken=null")
            .Replace("\r\n", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Replace(lineSeparator, string.Empty)
            .Replace(paragraphSeparator, string.Empty);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    }

    private static (int blobs, int foundBlobs, int updatedBlobs) UpdateCoreTypeBlobs(string testDataFilePath, string[] blobs)
    {
        // Replace existing test data blobs with updated ones
        string[] testDataLines = File.ReadAllLines(testDataFilePath);
        List<string> updatedTestDataLines = [];
        int numberOfBlobs = 0;
        int numberOfFoundBlobs = 0;
        int numberOfUpdatedBlobs = 0;

        for (int i = 0; i < testDataLines.Length; i++)
        {
            string testDataLine = testDataLines[i];
            if (!testDataLine.Trim().StartsWith("yield", StringComparison.Ordinal) || numberOfBlobs >= blobs.Length)
            {
                updatedTestDataLines.Add(testDataLine);
                continue;
            }

            string? replacement;
            string? pattern;

            if (PlatformDetection.IsNetFramework)
            {
                pattern = ", \"AAEAAAD[^\"]+\"(?!,)";
                replacement = $", \"{blobs[numberOfBlobs]}\"";
            }
            else
            {
                pattern = "\"AAEAAAD[^\"]+\",";
                replacement = $"\"{blobs[numberOfBlobs]}\",";
            }

            Regex regex = new(pattern);
            if (regex.IsMatch(testDataLine))
            {
                numberOfFoundBlobs++;
            }

            string updatedLine = regex.Replace(testDataLine, replacement);
            if (testDataLine != updatedLine)
            {
                numberOfUpdatedBlobs++;
            }

            testDataLine = updatedLine;

            updatedTestDataLines.Add(testDataLine);
            numberOfBlobs++;
        }

        // Check if all blobs were recognized and write updates to file
        Assert.Equal(numberOfBlobs, blobs.Length);
        File.WriteAllLines(testDataFilePath, updatedTestDataLines);

        return (numberOfBlobs, numberOfFoundBlobs, numberOfUpdatedBlobs);
    }

    public struct MyStruct
    {
        public int A;
    }
}
