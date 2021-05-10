// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class InteractionTests
    {

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void AppActivate_ProcessId()
        {
            InvokeMissingMethod(() => Interaction.AppActivate(42));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void AppActivate_Title()
        {
            InvokeMissingMethod(() => Interaction.AppActivate("MyProcess"));
        }

        [Theory]
        [MemberData(nameof(CallByName_TestData))]
        public void CallByName(object instance, string methodName, CallType useCallType, object[] args, Func<object, object> getResult, object expected)
        {
            Assert.Equal(getResult is null ? expected : null, Interaction.CallByName(instance, methodName, useCallType, args));
            if (getResult != null)
            {
                Assert.Equal(expected, getResult(instance));
            }
        }

        [Theory]
        [MemberData(nameof(CallByName_ArgumentException_TestData))]
        public void CallByName_ArgumentException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<ArgumentException>(() => Interaction.CallByName(instance, methodName, useCallType, args));
        }

        [Theory]
        [MemberData(nameof(CallByName_MissingMemberException_TestData))]
        public void CallByName_MissingMemberException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<MissingMemberException>(() => Interaction.CallByName(instance, methodName, useCallType, args));
        }

        public static IEnumerable<object[]> CallByName_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[] { 1, 2 }, null, 3 };
            yield return new object[] { new Class(), "Method", CallType.Get, new object[] { 2, 3 }, null, 5 };
            yield return new object[] { new Class(), "P", CallType.Get, new object[0], null, 0 };
            yield return new object[] { new Class(), "Item", CallType.Get, new object[] { 2 }, null, 2 };
            yield return new object[] { new Class(), "P", CallType.Set, new object[] { 3 }, new Func<object, object>(obj => ((Class)obj).Value), 3 };
            yield return new object[] { new Class(), "Item", CallType.Let, new object[] { 4, 5 }, new Func<object, object>(obj => ((Class)obj).Value), 9 };
        }

        public static IEnumerable<object[]> CallByName_ArgumentException_TestData()
        {
            yield return new object[] { null, null, default(CallType), new object[0] };
            yield return new object[] { new Class(), "Method", default(CallType), new object[] { 1, 2 } };
            yield return new object[] { new Class(), "Method", (CallType)int.MaxValue, new object[] { 1, 2 } };
        }

        public static IEnumerable<object[]> CallByName_MissingMemberException_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[0] };
            yield return new object[] { new Class(), "Q", CallType.Get, new object[0] };
        }

        private sealed class Class
        {
            public int Value;
            public int Method(int x, int y) => x + y;
            public int P
            {
                get { return Value; }
                set { Value = value; }
            }
            public object this[object index]
            {
                get { return Value + (int)index; }
                set { Value = (int)value + (int)index; }
            }
        }

        [Fact]
        public void Choose()
        {
            object[] x = { "Choice1", "Choice2", "Choice3", "Choice4", "Choice5", "Choice6" };
            Assert.Null(Interaction.Choose(5));
            Assert.Null(Interaction.Choose(0, x)); // < 1
            Assert.Null(Interaction.Choose(x.Length + 1, x)); // > UpperBound
            Assert.Equal(2, Interaction.Choose(2, 1, 2, 3));
            Assert.Equal("Choice3", Interaction.Choose(3, x[0], x[1], x[2]));
            for (int i = 1; i <= x.Length; i++)
            {
                Assert.Equal(x[i - 1], Interaction.Choose(i, x));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50572", TestPlatforms.Android)]
        public void Command()
        {
            var expected = Environment.CommandLine;
            var actual = Interaction.Command();
            Assert.False(string.IsNullOrEmpty(actual));
            Assert.EndsWith(actual, expected);
        }

        [Fact]
        public void CreateObject()
        {
            Assert.Throws<NullReferenceException>(() => Interaction.CreateObject(null));
            Assert.Throws<Exception>(() => Interaction.CreateObject(""));
            // Not tested: valid ProgID.
        }

        [Theory]
        [MemberData(nameof(IIf_TestData))]
        public void IIf(bool expression, object truePart, object falsePart, object expected)
        {
            Assert.Equal(expected, Interaction.IIf(expression, truePart, falsePart));
        }

        public static IEnumerable<object[]> IIf_TestData()
        {
            yield return new object[] { false, 1, null, null };
            yield return new object[] { true, 1, null, 1 };
            yield return new object[] { false, null, 2, 2 };
            yield return new object[] { true, null, 2, null };
            yield return new object[] { false, 3, "str", "str" };
            yield return new object[] { true, 3, "str", 3 };
        }
        
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2139", TestRuntimes.Mono)]
        public void DeleteSetting()
        {
            if (!PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<ArgumentException>(() => Interaction.DeleteSetting(AppName: "", Section: null, Key: null));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Interaction.DeleteSetting(AppName: "", Section: null, Key: null));
            }
            // Not tested: valid arguments.
        }

        [Fact]
        public void Environ_Index()
        {
            var pairs = GetEnvironmentVariables();
            int n = Math.Min(pairs.Length, 255);

            // Exception.ToString() called to verify message is constructed successfully.
            _ = Assert.Throws<ArgumentException>(() => Interaction.Environ(0)).ToString();
            _ = Assert.Throws<ArgumentException>(() => Interaction.Environ(256)).ToString();

            for (int i = 0; i < n; i++)
            {
                var pair = pairs[i];
                Assert.Equal($"{pair.Item1}={pair.Item2}", Interaction.Environ(i + 1));
            }

            for (int i = n; i < 255; i++)
            {
                Assert.Equal("", Interaction.Environ(i + 1));
            }
        }
        
        [Fact]
        public void Environ_Name()
        {
            var pairs = GetEnvironmentVariables();

            // Exception.ToString() called to verify message is constructed successfully.
            _ = Assert.Throws<ArgumentException>(() => Interaction.Environ("")).ToString();
            _ = Assert.Throws<ArgumentException>(() => Interaction.Environ(" ")).ToString();

            foreach (var (key, value) in pairs)
            {
                Assert.Equal(value, Interaction.Environ(key));
            }

            if (pairs.Length > 0)
            {
                var (key, value) = pairs[pairs.Length - 1];
                Assert.Equal(value, Interaction.Environ("  " + key));
                Assert.Equal(value, Interaction.Environ(key + "  "));
                Assert.Null(Interaction.Environ(key + "z"));
            }
        }

        private static (string, string)[] GetEnvironmentVariables()
        {
            var pairs = new List<(string, string)>();
            var vars = Environment.GetEnvironmentVariables();
            foreach (var key in vars.Keys) {
                pairs.Add(((string)key, (string)vars[key]));
            }
            return pairs.OrderBy(pair => pair.Item1).ToArray();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2139", TestRuntimes.Mono)]
        public void GetAllSettings()
        {
            if (!PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<ArgumentException>(() => Interaction.GetAllSettings(AppName: "", Section: ""));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Interaction.GetAllSettings(AppName: "", Section: ""));
            }
            // Not tested: valid arguments.
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2139", TestRuntimes.Mono)]
        public void GetSetting()
        {
            if (!PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<ArgumentException>(() => Interaction.GetSetting(AppName: "", Section: "", Key: "", Default: ""));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Interaction.GetSetting(AppName: "", Section: "", Key: "", Default: ""));
            }
            // Not tested: valid arguments.
        }

        [Fact]
        public void GetObject()
        {
            Assert.Throws<Exception>(() => Interaction.GetObject());
            Assert.Throws<Exception>(() => Interaction.GetObject("", null));
            Assert.Throws<Exception>(() => Interaction.GetObject(null, ""));
            Assert.Throws<Exception>(() => Interaction.GetObject("", ""));
            // Not tested: valid arguments.
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void InputBox()
        {
            InvokeMissingMethod(() => Interaction.InputBox("Prompt", Title: "", DefaultResponse: "", XPos: -1, YPos: -1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void MsgBox()
        {
            InvokeMissingMethod(() => Interaction.MsgBox("Prompt", Buttons: MsgBoxStyle.ApplicationModal, Title: null));
        }

        [Theory]
        [InlineData(0, 1, 2, 1, " :0")]
        [InlineData(1, 1, 2, 1, "1:1")]
        [InlineData(2, 1, 2, 1, "2:2")]
        [InlineData(3, 1, 2, 1, "3: ")]
        [InlineData(10, 1, 9, 1, "10:  ")]
        [InlineData(-1, 0, 1, 1, "  :-1")]
        [InlineData(-50, 0, 1, 1, "  :-1")]
        [InlineData(0, 1, 100, 10, "   :  0")]
        [InlineData(1, 1, 100, 10, "  1: 10")]
        [InlineData(15, 1, 100, 10, " 11: 20")]
        [InlineData(25, 1, 100, 10, " 21: 30")]
        [InlineData(35, 1, 100, 10, " 31: 40")]
        [InlineData(45, 1, 100, 10, " 41: 50")]
        [InlineData(50, 40, 100, 10, " 50: 59")]
        [InlineData(120, 100, 200, 10, "120:129")]
        [InlineData(150, 100, 120, 10, "121:   ")]
        [InlineData(5001, 1, 10000, 100, " 5001: 5100")]
        [InlineData(1, 0, 1, long.MaxValue, " 0: 1")]
        [InlineData(1, 0, long.MaxValue - 1, long.MaxValue, "                  0:9223372036854775806")]
        [InlineData(long.MaxValue, 0, long.MaxValue - 1, 1, "9223372036854775807:                   ")]
        [InlineData(long.MaxValue - 1, 0, long.MaxValue - 1, 1, "9223372036854775806:9223372036854775806")]
        public void Partition(long Number, long Start, long Stop, long Interval, string expected)
        {
            Assert.Equal(expected, Interaction.Partition(Number, Start, Stop, Interval));
        }

        [Theory]
        [InlineData(0, -1, 100, 10)] // Start < 0
        [InlineData(0, 100, 100, 10)] // Stop <= Start
        [InlineData(0, 1, 100, 0)] // Interval < 1
        public void Partition_Invalid(long Number, long Start, long Stop, long Interval)
        {
            Assert.Throws<ArgumentException>(() => Interaction.Partition(Number, Start, Stop, Interval));
        }

        [Theory]
        [InlineData(1, 0, long.MaxValue, 1)] // Stop + 1
        [InlineData(1, 0, long.MaxValue, long.MaxValue)]
        [InlineData(2, 1, 2, long.MaxValue)] // Lower + Interval
        [InlineData(long.MaxValue - 1, long.MaxValue - 1, long.MaxValue, 1)]
        public void Partition_Overflow(long Number, long Start, long Stop, long Interval)
        {
            Assert.Throws<OverflowException>(() => Interaction.Partition(Number, Start, Stop, Interval));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2139", TestRuntimes.Mono)]
        public void SaveSetting()
        {
            if (!PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<ArgumentException>(() => Interaction.SaveSetting(AppName: "", Section: "", Key: "", Setting: ""));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Interaction.SaveSetting(AppName: "", Section: "", Key: "", Setting: ""));
            }
            // Not tested: valid arguments.
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void Shell()
        {
            InvokeMissingMethod(() => Interaction.Shell("MyPath", Style: AppWinStyle.NormalFocus, Wait: false, Timeout: -1));
        }

        [Theory]
        [InlineData(null, null)] // empty
        [InlineData(new object[0], null)] // empty
        [InlineData(new object[] { false, "red", false, "green", false, "blue" }, null)] // none
        [InlineData(new object[] { true, "red", false, "green", false, "blue" }, "red")]
        [InlineData(new object[] { false, "red", true, "green", false, "blue" }, "green")]
        [InlineData(new object[] { false, "red", false, "green", true, "blue" }, "blue")]
        public void Switch(object[] VarExpr, object expected)
        {
            Assert.Equal(expected, Interaction.Switch(VarExpr));
        }

        [Fact]
        public void Switch_Invalid()
        {
            Assert.Throws<ArgumentException>(() => Interaction.Switch(true, "a", false));
        }

        // Methods that rely on reflection of missing assembly.
        private static void InvokeMissingMethod(Action action)
        {
            // Exception.ToString() called to verify message is constructed successfully.
            _ = Assert.Throws<PlatformNotSupportedException>(action).ToString();
        }
    }
}
