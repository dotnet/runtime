using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

[TestFixture]
public class EmbeddingApiTests
{

    // TODO : See if the string test can be fixed, otherwise remove.
    // const string kHelloString = "Hello";
    // private const string kHelloWorldString = "Hello, World!";
    // private const string kHelloWorldStringWithEmbeddedNull = "Hello\0World";
    // private const string kHelloWorldStringWithUnicode = "Hello, 団結!";

    // [Test]
    // public void FirstAttempt()
    // {
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 13), kHelloWorldString, 13);
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 5), kHelloString, 5);
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldStringWithEmbeddedNull, 11), kHelloWorldStringWithEmbeddedNull, 11);
    //     var byteArray = Encoding.Unicode.GetBytes(kHelloString);
    //     var utf8 = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, byteArray);
    //     // var utf16 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, utf8);
    //     // var finalStr = Encoding.Unicode.GetString(utf16);
    //     unsafe
    //     {
    //         var ptr = CoreCLRHost.string_new_len(null, (sbyte*)&utf8, (uint)utf8.Length);
    //         var target = Unsafe.As<IntPtr, string>(ref ptr);
    //         var utf16 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(target));
    //         var finalStr = Encoding.Unicode.GetString(utf16);
    //     }
    // }

    [Test]
    public void GCHandleNewAndGetTarget()
    {
        var obj = new object();
        var handle1 = CoreCLRHostTestingWrappers.gchandle_new_v2(obj, false);
        Assert.That(handle1, Is.Not.EqualTo(0));
        var result = CoreCLRHostTestingWrappers.gchandle_get_target_v2(handle1);
        Assert.That(obj, Is.EqualTo(result));

        var obj2 = new object();
        var handle2 = CoreCLRHostTestingWrappers.gchandle_new_v2(obj2, true);
        Assert.That(handle2, Is.Not.EqualTo(0));
        var result2 = CoreCLRHostTestingWrappers.gchandle_get_target_v2(handle2);
        Assert.That(obj2, Is.EqualTo(result2));

        Assert.That(handle1, Is.Not.EqualTo(handle2));

        GCHandle.FromIntPtr(handle1).Free();
        GCHandle.FromIntPtr(handle2).Free();
    }
}
