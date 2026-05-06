// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Configuration
{
    public class CallBackValidatorAttributeTests
    {
        [Fact]
        public void GetValidatorInstance_DefaultConstructorThrows()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute();
            Assert.Throws<ArgumentNullException>(() => testCallBackValidatorAttribute.ValidatorInstance);
        }

        [Fact]
        public void GetValidatorInstance_CallBackMethodNullThrows()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(double)
            };
            Assert.Throws<ArgumentException>(() => testCallBackValidatorAttribute.ValidatorInstance);
        }

        [Fact]
        public void GetValidatorInstance_MethdoInfoNotNullThrows()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(double),
                CallbackMethodName = "Test"
            };
            Assert.Throws<ArgumentException>(() => testCallBackValidatorAttribute.ValidatorInstance);
        }

        [Fact]
        public void GetValidatorInstance_Success()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(CallBackValidatorAttributeTests),
                CallbackMethodName = "CallBackValidatorTestMethod"
            };
            var response = testCallBackValidatorAttribute.ValidatorInstance;
            Assert.IsType<CallbackValidator>(response);
        }

        //Calls twice to test both branches of the _callbackmethod == null if statement
        [Fact]
        public void SuccessfulCallback_CallTwice()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(CallBackValidatorAttributeTests),
                CallbackMethodName = "CallBackValidatorTestMethod"
            };
            _ = testCallBackValidatorAttribute.ValidatorInstance;
            Assert.IsType<CallbackValidator>(testCallBackValidatorAttribute.ValidatorInstance);
        }

        [Fact]
        public void GetValidatorInstance_MethodHasTooManyParameters()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(CallBackValidatorAttributeTests),
                CallbackMethodName = "CallBackValidatorTestMethodNumberTwo"
            };
            Assert.Throws<ArgumentException>(() => testCallBackValidatorAttribute.ValidatorInstance);
        }

        [Fact]
        public void TypeIsExpected()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                Type = typeof(double)
            };
            Assert.Equal(typeof(double), testCallBackValidatorAttribute.Type);
        }

        [Fact]
        public void MethodNameIsExpected()
        {
            var testCallBackValidatorAttribute = new CallbackValidatorAttribute
            {
                CallbackMethodName = "12345"
            };
            Assert.Equal("12345", testCallBackValidatorAttribute.CallbackMethodName);
        }

#pragma warning disable xUnit1013 // Required to be public for CallbackValidatorAttribute to work
        public static void CallBackValidatorTestMethod(object o)
        {
        }

        public static void CallBackValidatorTestMethodNumberTwo(object o, object p)
        {
        }
#pragma warning restore xUnit1013
    }
}
