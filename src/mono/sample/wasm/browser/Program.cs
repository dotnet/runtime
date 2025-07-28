// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sample
{
    public interface IBase64Validatable<T>
    {
    }

    public readonly struct Base64CharValidatable : IBase64Validatable<char>
    {
        private static readonly string s_validBase64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    }

    public partial class Test
    {
        /*
        public static bool IsValid<T, TBase64Validatable>(TBase64Validatable validatable, ReadOnlySpan<T> base64Text, out int decodedLength)
            where TBase64Validatable : IBase64Validatable<T>
            where T : struct
        {
            int length = 0, paddingCount = 0;
            T lastChar = default;

            if (!base64Text.IsEmpty)
            {
                if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(base64Text))) 
                {
                    decodedLength = 0;
                    return false;
                }
            }

            decodedLength = base64Text.Length;
            return true;
        }

        public static bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength) =>
            IsValid(default(Base64CharValidatable), base64Text, out decodedLength);

        public unsafe static int Main(string[] args)
        {
            try
            {
                var text = "YQ==";
                var chars = text.ToArray();
                var span = (ReadOnlySpan<char>)chars;
                var t = IsValid(span, out int decodedLength);
                Console.WriteLine($"{text} -> {t}:{decodedLength} {span.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
            return 0;
        }*/

        public unsafe static int Main(string[] args)
        {
            try
            {
                var text = "YQ==";
                var chars = text.ToArray();
                var span = (ReadOnlySpan<char>)chars;
                var t = Base64.IsValid(span, out int decodedLength);
                Console.WriteLine($"{text} -> {t}:{decodedLength} {span.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
            return 0;
        }

        /*
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Buffers.Text.Base64Helper", "System.Private.CoreLib")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Buffers.Text.Base64", "System.Private.CoreLib")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Buffers.IndexOfAnyAsciiSearcher", "System.Private.CoreLib")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Buffers.IndexOfAnyAsciiSearcher.AsciiState", "System.Private.CoreLib")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Buffers.IndexOfAnyAsciiSearcher.AnyByteState", "System.Private.CoreLib")]
        public unsafe static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello from Sample.Test.Main 1");
                var utf8WithByteToBeIgnored = "YQ==";
                var expectedLength = 1;
                var utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

                //Console.WriteLine("Hello from Sample.Test.Main 2");
                //Console.WriteLine(Base64.IsValid((ReadOnlySpan<char>)utf8BytesWithByteToBeIgnored));
                //Console.WriteLine("Hello from Sample.Test.Main 3");
                var utf8BytesWithByteToBeIgnored2 = utf8WithByteToBeIgnored.ToArray();
                var utf8BytesWithByteToBeIgnored2RS = (ReadOnlySpan<char>)utf8BytesWithByteToBeIgnored2;
                fixed (char* charsPtr = &MemoryMarshal.GetReference(utf8BytesWithByteToBeIgnored2RS))
                {
                    Console.WriteLine($"Hello from Sample.Test.Main 4 {(IntPtr)charsPtr}");
                    Console.WriteLine(Base64.IsValid(utf8BytesWithByteToBeIgnored2RS, out int decodedLength));
                    Console.WriteLine($"expectedLength: {expectedLength}, decodedLength: {decodedLength} {utf8BytesWithByteToBeIgnored2.Length}");
                    
                    Console.WriteLine("Hello from Sample.Test.Main E");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Exception: {ex.StackTrace}");
                return -1;
            }
            return 0;
        }*/
    }
}
