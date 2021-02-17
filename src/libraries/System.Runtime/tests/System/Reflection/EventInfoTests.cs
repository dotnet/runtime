// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class EventInfoTests
    {
        public static IEnumerable<object[]> AddMethod_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(EventInfoTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(AddMethod_TestData))]
        public void AddMethod_Get_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.AddMethod);
        }

        private const BindingFlags AllBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        [Theory]
        [InlineData(nameof(ParameterlessMethod), null)]
        [InlineData(nameof(NoDelegateMethod), null)]
        [InlineData(nameof(DelegateMethod1), null)]
        [InlineData(nameof(DelegateMethod2), null)]
        [InlineData(nameof(EventHandlerMethod1), typeof(EventHandler))]
        [InlineData(nameof(EventHandlerMethod2), typeof(EventHandler))]
        [InlineData(nameof(MulticastDelegateMethod1), typeof(MulticastDelegate))]
        [InlineData(nameof(MulticastDelegateMethod2), typeof(MulticastDelegate))]
        public void EventHandlerType_Invoke_Success(string methodName, Type expected)
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(methodName, AllBindingFlags);
                }
            };
            Assert.Equal(expected, eventInfo.EventHandlerType);
        }

        [Fact]
        public void EventHandlerType_InvokeNullAddMethod_ThrowsNullReferenceException()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return null;
                }
            };
            Assert.Throws<NullReferenceException>(() => eventInfo.EventHandlerType);
        }

        [Theory]
        [InlineData(nameof(ParameterlessMethod), false)]
        [InlineData(nameof(NoDelegateMethod), false)]
        [InlineData(nameof(DelegateMethod1), false)]
        [InlineData(nameof(DelegateMethod2), false)]
        [InlineData(nameof(EventHandlerMethod1), true)]
        [InlineData(nameof(EventHandlerMethod2), true)]
        [InlineData(nameof(MulticastDelegateMethod1), true)]
        [InlineData(nameof(MulticastDelegateMethod2), true)]
        public void IsMulticast_Invoke_Success(string methodName, bool expected)
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(methodName, AllBindingFlags);
                }
            };
            Assert.Equal(expected, eventInfo.IsMulticast);
        }

        [Fact]
        public void IsMulticast_InvokeNullAddMethod_ThrowsNullReferenceException()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return null;
                }
            };
            Assert.Throws<NullReferenceException>(() => eventInfo.IsMulticast);
        }

        [Theory]
        [InlineData((EventAttributes)0, false)]
        [InlineData(EventAttributes.SpecialName, true)]
        [InlineData(EventAttributes.SpecialName | EventAttributes.RTSpecialName, true)]
        [InlineData(EventAttributes.RTSpecialName, false)]
        public void IsSpecialName_Get_ReturnsExpected(EventAttributes attributes, bool expected)
        {
            var eventInfo = new SubEventInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, eventInfo.IsSpecialName);
        }

        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var eventInfo = new SubEventInfo();
            Assert.Equal(MemberTypes.Event, eventInfo.MemberType);
        }

        public static IEnumerable<object[]> RaiseMethod_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(EventInfoTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(RaiseMethod_TestData))]
        public void RaiseMethod_Get_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetRaiseMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.RaiseMethod);
        }

        public static IEnumerable<object[]> RemoveMethod_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(EventInfoTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(RemoveMethod_TestData))]
        public void RemoveMethod_Get_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetRemoveMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.RemoveMethod);
        }

        [Fact]
        public void AddEventHandler_InvokeInstance_Success()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(nameof(EventInfoTests.EventHandlerMethod1), AllBindingFlags);
                }
            };
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            eventInfo.AddEventHandler(this, handler);
            Assert.Equal(0, callCount);
            Assert.Equal(1, EventHandlerMethod1CallCount);
        }

        [Fact]
        public void AddEventHandler_InvokeStatic_Success()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(nameof(EventInfoTests.StaticEventHandlerMethod), AllBindingFlags);
                }
            };
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            eventInfo.AddEventHandler(null, handler);
            Assert.Equal(0, callCount);
            Assert.Equal(1, StaticEventHandlerMethodCallCount);
        }

        [Fact]
        public void AddEventHandler_InvokeNullTargetInstance_ThrowsTargetException()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(nameof(EventInfoTests.EventHandlerMethod1), AllBindingFlags);
                }
            };
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            Assert.Throws<TargetException>(() => eventInfo.AddEventHandler(null, handler));
            Assert.Equal(0, callCount);
        }

        [Theory]
        [InlineData(nameof(EventInfoTests.ParameterlessMethod))]
        [InlineData(nameof(EventInfoTests.DelegateMethod2))]
        public void AddEventHandler_InvokeInvalidParametersLength_ThrowsTargetParameterCountException(string name)
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return typeof(EventInfoTests).GetMethod(name, AllBindingFlags);
                }
            };
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            Assert.Throws<TargetParameterCountException>(() => eventInfo.AddEventHandler(this, handler));
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void AddEventHandler_InvokeNullAddMethod_ThrowsInvalidOperationException()
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return null;
                }
            };
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            Assert.Throws<InvalidOperationException>(() => eventInfo.AddEventHandler(new object(), handler));
            Assert.Equal(0, callCount);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var eventInfo = new SubEventInfo();
            yield return new object[] { eventInfo, eventInfo, true };
            yield return new object[] { eventInfo, new SubEventInfo(), false };
            yield return new object[] { eventInfo, new object(), false };
            yield return new object[] { eventInfo, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(EventInfo eventInfo, object other, bool expected)
        {
            Assert.Equal(expected, eventInfo.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var eventInfo = new SubEventInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, eventInfo, false };
            yield return new object[] { eventInfo, eventInfo, true };
            yield return new object[] { eventInfo, new SubEventInfo(), false };
            yield return new object[] { eventInfo, null, false };

            yield return new object[] { new AlwaysEqualsEventInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsEventInfo(), false };
            yield return new object[] { new AlwaysEqualsEventInfo(), new SubEventInfo(), true };
            yield return new object[] { new SubEventInfo(), new AlwaysEqualsEventInfo(), false };
            yield return new object[] { new AlwaysEqualsEventInfo(), new AlwaysEqualsEventInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(EventInfo eventInfo1, EventInfo eventInfo2, bool expected)
        {
            Assert.Equal(expected, eventInfo1 == eventInfo2);
            Assert.Equal(!expected, eventInfo1 != eventInfo2);
        }

        [Theory]
        [MemberData(nameof(AddMethod_TestData))]
        public void GetAddMethod_Invoke_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetAddMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.GetAddMethod());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var eventInfo = new SubEventInfo();
            Assert.NotEqual(0, eventInfo.GetHashCode());
            Assert.Equal(eventInfo.GetHashCode(), eventInfo.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(RaiseMethod_TestData))]
        public void GetRaiseMethod_Invoke_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetRaiseMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.GetRaiseMethod());
        }

        [Theory]
        [MemberData(nameof(RemoveMethod_TestData))]
        public void GetRemoveMethod_Invoke_ReturnsExpected(MethodInfo result)
        {
            var eventInfo = new SubEventInfo
            {
                GetRemoveMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.GetRemoveMethod());
        }

        [Fact]
        public void GetOtherMethods_Invoke_ThrowsNotImplementedException()
        {
            var eventInfo = new SubEventInfo();
            Assert.Throws<NotImplementedException>(() => eventInfo.GetOtherMethods());
        }

        public static IEnumerable<object[]> GetOtherMethods_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MethodInfo[0] };
            yield return new object[] { new MethodInfo[] { null } };
            yield return new object[] { new MethodInfo[] { typeof(EventInfoTests).GetMethods()[0] } };
        }

        [Theory]
        [MemberData(nameof(GetOtherMethods_Custom_TestData))]
        public void GetOtherMethods_InvokeCustom_ReturnsExpected(MethodInfo[] result)
        {
            var eventInfo = new CustomEventInfo
            {
                GetOtherMethodsAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, eventInfo.GetOtherMethods());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetOtherMethods_InvokeBool_ThrowsNotImplementedException(bool nonPublic)
        {
            var eventInfo = new SubEventInfo();
            Assert.Throws<NotImplementedException>(() => eventInfo.GetOtherMethods(nonPublic));
        }

        private object ParameterlessMethod() => nameof(ParameterlessMethod);

        private object NoDelegateMethod(int x) => nameof(NoDelegateMethod);

        private object DelegateMethod1(Delegate d) => nameof(DelegateMethod1);

        private object DelegateMethod2(int x, Delegate d) => nameof(DelegateMethod2);

        private int EventHandlerMethod1CallCount = 0;

        private object EventHandlerMethod1(EventHandler d)
        {
            EventHandlerMethod1CallCount++;
            return nameof(EventHandlerMethod1);
        }

        private object EventHandlerMethod2(int x, EventHandler d) => nameof(EventHandlerMethod2);

        private object MulticastDelegateMethod1(MulticastDelegate d) => nameof(MulticastDelegateMethod1);

        private object MulticastDelegateMethod2(int x, MulticastDelegate d) => nameof(MulticastDelegateMethod1);

        private static int StaticEventHandlerMethodCallCount = 0;

        private static object StaticEventHandlerMethod(EventHandler d)
        {
            StaticEventHandlerMethodCallCount++;
            return nameof(StaticEventHandlerMethod);
        }

        private class CustomEventInfo : SubEventInfo
        {
            public Func<bool, MethodInfo[]> GetOtherMethodsAction { get; set; }

            public override MethodInfo[] GetOtherMethods(bool nonPublic)
            {
                if (GetOtherMethodsAction == null)
                {
                    return base.GetOtherMethods(nonPublic);
                }

                return GetOtherMethodsAction(nonPublic);
            }
        }

        private class AlwaysEqualsEventInfo : SubEventInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubEventInfo : EventInfo
        {
            public EventAttributes AttributesResult { get; set; }

            public override EventAttributes Attributes => AttributesResult;

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public Func<bool, MethodInfo> GetAddMethodAction { get; set; }

            public override MethodInfo GetAddMethod(bool nonPublic) => GetAddMethodAction(nonPublic);

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public Func<bool, MethodInfo> GetRaiseMethodAction { get; set; }

            public override MethodInfo GetRaiseMethod(bool nonPublic) => GetRaiseMethodAction(nonPublic);

            public Func<bool, MethodInfo> GetRemoveMethodAction { get; set; }

            public override MethodInfo GetRemoveMethod(bool nonPublic) => GetRemoveMethodAction(nonPublic);

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
