// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public static class ExceptionTests
    {
        private const int COR_E_AMBIGUOUSMATCH = unchecked((int)0x8000211D);
        private const int COR_E_INVALIDFILTERCRITERIA = unchecked((int)0x80131601);
        private const int COR_E_TARGET = unchecked((int)0x80131603);
        private const int COR_E_TARGETINVOCATION = unchecked((int)0x80131604);
        private const int COR_E_TARGETPARAMCOUNT = unchecked((int)0x8002000E);

        [Fact]
        public static void TargetException()
        {
            TargetException ex = new TargetException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_TARGET, ex.HResult);
        }

        [Fact]
        public static void TargetException_Message()
        {
            string message = "message";
            TargetException ex = new TargetException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_TARGET, ex.HResult);
        }

        [Fact]
        public static void TargetException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            TargetException ex = new TargetException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_TARGET, ex.HResult);
        }

        [Fact]
        public static void TargetInvocationException_InnerException()
        {
            Exception inner = new Exception();
            TargetInvocationException ex = new TargetInvocationException(inner);

            Assert.NotEmpty(ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_TARGETINVOCATION, ex.HResult);
        }

        [Fact]
        public static void TargetInvocationException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            TargetInvocationException ex = new TargetInvocationException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_TARGETINVOCATION, ex.HResult);
        }

        [Fact]
        public static void TargetParameterCountException()
        {
            TargetParameterCountException ex = new TargetParameterCountException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_TARGETPARAMCOUNT, ex.HResult);
        }

        [Fact]
        public static void TargetParameterCountException_Message()
        {
            string message = "message";
            TargetParameterCountException ex = new TargetParameterCountException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_TARGETPARAMCOUNT, ex.HResult);
        }

        [Fact]
        public static void TargetParameterCountException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            TargetParameterCountException ex = new TargetParameterCountException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_TARGETPARAMCOUNT, ex.HResult);
        }

        [Fact]
        public static void AmbiguousMatchException()
        {
            AmbiguousMatchException ex = new AmbiguousMatchException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_AMBIGUOUSMATCH, ex.HResult);
        }

        [Fact]
        public static void AmbiguousMatchException_Message()
        {
            string message = "message";
            AmbiguousMatchException ex = new AmbiguousMatchException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_AMBIGUOUSMATCH, ex.HResult);
        }

        [Fact]
        public static void AmbiguousMatchException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            AmbiguousMatchException ex = new AmbiguousMatchException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_AMBIGUOUSMATCH, ex.HResult);
        }

        [Fact]
        public static void InvalidFilterCriteriaException()
        {
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_INVALIDFILTERCRITERIA, ex.HResult);
        }

        [Fact]
        public static void InvalidFilterCriteriaException_Message()
        {
            string message = "message";
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(COR_E_INVALIDFILTERCRITERIA, ex.HResult);
        }

        [Fact]
        public static void InvalidFilterCriteriaException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(COR_E_INVALIDFILTERCRITERIA, ex.HResult);
        }
    }
}
