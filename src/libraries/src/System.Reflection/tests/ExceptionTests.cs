using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class ExceptionTests
    {

        public static void TargetException()
        {
            Exception ex = new TargetException();
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(TargetException));

            string s = "My exception";
            ex = new TargetException();
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(TargetException));
            Assert.Equal(s, ex.Message);

            s = "My exception";
            Exception innerException = new Exception();
            ex = new TargetException(s, innerException);
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(TargetException));
            Assert.Equal(innerException, ex.InnerException);
            Assert.Equal(s, ex.Message);

            // Throw the exception from a method.
            try
            {
                ThrowTargetException(s, innerException);
                Assert.True(false);
            }
            catch (TargetException tex)
            {
                Assert.Equal(innerException, tex.InnerException);
                Assert.Equal(s, tex.Message);
            }
            catch (Exception)
            {
                Assert.True(false);
            }
        }

        private static void ThrowTargetException(string s, Exception innerException)
        {
            throw new TargetException(s, innerException);
        }

        private static void ThrowInvalidFilterCriteriaException(string s, Exception innerException)
        {
            throw new InvalidFilterCriteriaException(s, innerException);
        }



        public static void InvalidFilterCriteriaException()
        {
            Exception ex = new InvalidFilterCriteriaException();
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(InvalidFilterCriteriaException));

            string s = "My exception";
            ex = new InvalidFilterCriteriaException();
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(InvalidFilterCriteriaException));
            Assert.Equal(s, ex.Message);

            s = "My exception";
            Exception innerException = new Exception();
            ex = new InvalidFilterCriteriaException(s, innerException);
            Assert.NotNull(ex);
            Assert.Equal(ex.GetType(), typeof(InvalidFilterCriteriaException));
            Assert.Equal(innerException, ex.InnerException);
            Assert.Equal(s, ex.Message);

            // Throw the exception from a method.
            try
            {
                ThrowInvalidFilterCriteriaException(s, innerException);
                Assert.True(false);
            }
            catch (InvalidFilterCriteriaException tex)
            {
                Assert.Equal(innerException, tex.InnerException);
                Assert.Equal(s, tex.Message);
            }
            catch (Exception)
            {
                Assert.True(false);
            }
        }
    }
}