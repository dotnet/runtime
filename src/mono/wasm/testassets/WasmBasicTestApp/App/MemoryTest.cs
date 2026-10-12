// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public partial class MemoryTest
{
    private static readonly List<string> s_errors = new();

    [JSImport("countChars", "main.js")]
    internal static partial int CountChars(string testArray);

    [JSImport("echoString", "main.js")]
    internal static partial string EchoString(string value);

    [JSImport("createBytes", "main.js")]
    internal static partial byte[] CreateBytes(int length);

    [JSImport("sumBytes", "main.js")]
    internal static partial int SumBytes(byte[] value);

    [JSImport("sumMemoryView", "main.js")]
    internal static partial int SumSpan([JSMarshalAs<JSType.MemoryView>] Span<byte> value);

    [JSImport("sumMemoryView", "main.js")]
    internal static partial int SumSegment([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> value);

    [JSImport("echoInt32Array", "main.js")]
    internal static partial int[] EchoInt32Array(int[] value);

    [JSImport("echoDoubleArray", "main.js")]
    internal static partial double[] EchoDoubleArray(double[] value);

    [JSImport("createObject", "main.js")]
    internal static partial JSObject CreateObject(string name, int value);

    [JSImport("throwError", "main.js")]
    internal static partial void ThrowError(string message);

    [JSImport("delayedSum", "main.js")]
    internal static partial Task<int> DelayedSum(int a, int b);

    [JSImport("callManagedExports", "main.js")]
    internal static partial string CallManagedExports();

    [JSExport]
    internal static int SumBytesManaged(byte[] value)
    {
        int sum = 0;
        foreach (byte b in value)
        {
            sum += b;
        }

        return sum;
    }

    [JSExport]
    internal static byte[] CreateBytesManaged(int length)
    {
        byte[] result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = unchecked((byte)(i * 3));
        }

        return result;
    }

    [JSExport]
    internal static string EchoStringManaged(string value) => value;

    [JSExport]
    internal static async Task RunAsync()
    {
        ReportAllocationAddress();
        AllocateManagedArrays();
        TierUpInterop();

        Run("string round trip", StringRoundTrip);
        Run("large string marshaling", LargeStringMarshaling);
        Run("byte[] JS to C#", BytesFromJs);
        Run("byte[] C# to JS", BytesToJs);
        Run("int[] round trip", Int32ArrayRoundTrip);
        Run("double[] round trip", DoubleArrayRoundTrip);
        Run("Span<byte> memory view", SpanMemoryView);
        Run("ArraySegment<byte> memory view", ArraySegmentMemoryView);
        Run("JSObject properties", JSObjectProperties);
        Run("exception propagation", ExceptionPropagation);
        Run("crypto RandomNumberGenerator", CryptoRandomBytes);
        Run("globalization", Globalization);
        Run("JSExport round trips", ManagedExports);

        await RunAsync("Task<int> round trip", TaskRoundTrip);

        if (s_errors.Count != 0)
        {
            string message = string.Join(Environment.NewLine, s_errors);
            TestOutput.WriteLine(message);
            throw new Exception(message);
        }

        TestOutput.WriteLine("Great success, MemoryTest finished without errors.");
    }

    private static void ReportAllocationAddress()
    {
        byte[] pinned = GC.AllocateArray<byte>(1024, pinned: true);
        ulong address;
        unsafe
        {
            fixed (byte* p = pinned)
            {
                address = (ulong)(nuint)p;
            }
        }

        TestOutput.WriteLine($"Pinned buffer allocated at 0x{address:X}");
        if (address < 0x8000_0000)
        {
            s_errors.Add($"Allocations are below the 2GB boundary (0x{address:X}), the test is not exercising >2GB pointers.");
        }
    }

    private static void AllocateManagedArrays()
    {
        // Allocate 250MB managed space above 2GB already wasted before startup
        const int arrayCnt = 10;
        int[][] arrayHolder = new int[arrayCnt][];
        TestOutput.WriteLine("Starting managed array allocation");
        for (int i = 0; i < arrayCnt; i++)
        {
            try
            {
                arrayHolder[i] = new int[1024 * 1024 * 25];
            }
            catch (Exception ex)
            {
                s_errors.Add($"Exception {ex} was thrown on i={i}");
            }
        }

        TestOutput.WriteLine("Finished managed array allocation");
    }

    private static void TierUpInterop()
    {
        // call a method many times to trigger tier-up optimization
        string randomString = GenerateRandomString(1000);
        try
        {
            for (int i = 0; i < 1000; i++)
            {
                int count = CountChars(randomString);
                if (count != randomString.Length)
                {
                    s_errors.Add($"CountChars returned {count} instead of {randomString.Length} for {i}-th string.");
                }
            }
        }
        catch (Exception ex)
        {
            s_errors.Add($"Exception {ex} was thrown when CountChars was called in a loop");
        }
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            TestOutput.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            s_errors.Add($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            TestOutput.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            s_errors.Add($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static void StringRoundTrip()
    {
        foreach (string value in new[] { "", "a", "Příliš žluťoučký kůň", "🔥 emoji \u00e9\u00e8" })
        {
            string result = EchoString(value);
            Assert(result == value, $"echo mismatch for '{value}', got '{result}'");
        }
    }

    private static void LargeStringMarshaling()
    {
        string value = string.Concat(new string('ě', 500_000), new string('a', 500_000));
        string result = EchoString(value);
        Assert(result.Length == value.Length, $"expected {value.Length} chars, got {result.Length}");
        Assert(result == value, "large string content mismatch");
    }

    private static void BytesFromJs()
    {
        const int length = 64 * 1024;
        byte[] bytes = CreateBytes(length);
        Assert(bytes.Length == length, $"expected {length} bytes, got {bytes.Length}");
        for (int i = 0; i < length; i++)
        {
            Assert(bytes[i] == unchecked((byte)i), $"byte[{i}] expected {unchecked((byte)i)}, got {bytes[i]}");
        }
    }

    private static void BytesToJs()
    {
        byte[] bytes = new byte[64 * 1024];
        int expected = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(i * 7));
            expected += bytes[i];
        }

        int actual = SumBytes(bytes);
        Assert(actual == expected, $"expected sum {expected}, got {actual}");
    }

    private static void Int32ArrayRoundTrip()
    {
        int[] values = new int[10_000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i * 3 - 5;
        }

        int[] result = EchoInt32Array(values);
        Assert(result.Length == values.Length, $"expected {values.Length} items, got {result.Length}");
        for (int i = 0; i < values.Length; i++)
        {
            Assert(result[i] == values[i], $"int[{i}] expected {values[i]}, got {result[i]}");
        }
    }

    private static void DoubleArrayRoundTrip()
    {
        double[] values = new double[10_000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i / 3.0;
        }

        double[] result = EchoDoubleArray(values);
        Assert(result.Length == values.Length, $"expected {values.Length} items, got {result.Length}");
        for (int i = 0; i < values.Length; i++)
        {
            Assert(result[i] == values[i], $"double[{i}] expected {values[i]}, got {result[i]}");
        }
    }

    private static void SpanMemoryView()
    {
        byte[] bytes = new byte[32 * 1024];
        int expected = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(i * 11));
            expected += bytes[i];
        }

        int actual = SumSpan(bytes.AsSpan());
        Assert(actual == expected, $"expected sum {expected}, got {actual}");
    }

    private static void ArraySegmentMemoryView()
    {
        byte[] bytes = new byte[32 * 1024];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(i * 13));
        }

        var segment = new ArraySegment<byte>(bytes, 1024, 4096);
        int expected = 0;
        for (int i = 0; i < segment.Count; i++)
        {
            expected += segment[i];
        }

        int actual = SumSegment(segment);
        Assert(actual == expected, $"expected sum {expected}, got {actual}");
    }

    private static void JSObjectProperties()
    {
        using JSObject obj = CreateObject("answer", 42);
        Assert(obj.GetPropertyAsString("name") == "answer", "name property mismatch");
        Assert(obj.GetPropertyAsInt32("value") == 42, "value property mismatch");
        obj.SetProperty("value", 43);
        Assert(obj.GetPropertyAsInt32("value") == 43, "value property was not updated");
    }

    private static void ExceptionPropagation()
    {
        try
        {
            ThrowError("boom");
        }
        catch (JSException ex)
        {
            Assert(ex.Message.Contains("boom"), $"unexpected message: {ex.Message}");
            return;
        }

        throw new Exception("expected a JSException");
    }

    private static void CryptoRandomBytes()
    {
        // the buffer is filled by JavaScript, writing to a wrong offset leaves it silently zeroed
        byte[] buffer = new byte[4096];
        RandomNumberGenerator.Fill(buffer);

        int zeros = 0;
        foreach (byte b in buffer)
        {
            if (b == 0)
            {
                zeros++;
            }
        }

        Assert(zeros < buffer.Length / 4, $"buffer looks unfilled, {zeros} zero bytes out of {buffer.Length}");
    }

    private static void Globalization()
    {
        var culture = new CultureInfo("cs-CZ");
        Assert(string.Compare("a", "b", culture, CompareOptions.None) < 0, "unexpected compare result");
        Assert(new DateTime(2026, 1, 2).ToString("MMMM", culture).Length > 0, "empty month name");
        Assert(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("žluťoučký")) == "žluťoučký", "UTF8 round trip failed");
    }

    private static void ManagedExports()
    {
        string errors = CallManagedExports();
        Assert(string.IsNullOrEmpty(errors), errors);
    }

    private static async Task TaskRoundTrip()
    {
        int result = await DelayedSum(20, 22);
        Assert(result == 42, $"expected 42, got {result}");
    }

    private static Random random = new Random();

    private static string GenerateRandomString(int stringLength)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var stringBuilder = new StringBuilder(stringLength);
        for (int i = 0; i < stringLength; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }
}
