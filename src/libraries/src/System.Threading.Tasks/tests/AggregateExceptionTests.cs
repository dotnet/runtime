// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class AggregateExceptionTests
    {
        [Fact]
        public static void ConstructorBasic()
        {
            AggregateException ex = new AggregateException();
            Assert.Equal(ex.InnerExceptions.Count, 0);
            Assert.True(ex.Message != null, "RunAggregateException_Constructor:  FAILED. Message property is null when the default constructor is used, expected a default message");

            ex = new AggregateException("message");
            Assert.Equal(ex.InnerExceptions.Count, 0);
            Assert.True(ex.Message != null, "RunAggregateException_Constructor:  FAILED. Message property is  null when the default constructor(string) is used");

            ex = new AggregateException("message", new Exception());
            Assert.Equal(ex.InnerExceptions.Count, 1);
            Assert.True(ex.Message != null, "RunAggregateException_Constructor:  FAILED. Message property is  null when the default constructor(string, Exception) is used");
        }

        [Fact]
        public static void ConstructorInvalidArguments()
        {
            AggregateException ex = new AggregateException();
            Assert.Throws<ArgumentNullException>(() => new AggregateException("message", (Exception)null));

            Assert.Throws<ArgumentNullException>(() => new AggregateException("message", (IEnumerable<Exception>)null));

            AssertExtensions.Throws<ArgumentException>(null, () => ex = new AggregateException("message", new[] { new Exception(), null }));
        }

        [Fact]
        public static void BaseExceptions()
        {
            AggregateException ex = new AggregateException();
            Assert.Equal(ex.GetBaseException(), ex);

            Exception[] innerExceptions = new Exception[0];
            ex = new AggregateException(innerExceptions);
            Assert.Equal(ex.GetBaseException(), ex);

            innerExceptions = new Exception[1] { new AggregateException() };
            ex = new AggregateException(innerExceptions);
            Assert.Equal(ex.GetBaseException(), innerExceptions[0]);

            innerExceptions = new Exception[2] { new AggregateException(), new AggregateException() };
            ex = new AggregateException(innerExceptions);
            Assert.Equal(ex.GetBaseException(), ex);
        }

        [Fact]
        public static void Handle()
        {
            AggregateException ex = new AggregateException();
            ex = new AggregateException(new[] { new ArgumentException(), new ArgumentException(), new ArgumentException() });
            int handledCount = 0;
            ex.Handle((e) =>
            {
                if (e is ArgumentException)
                {
                    handledCount++;
                    return true;
                }
                return false;
            });
            Assert.Equal(handledCount, ex.InnerExceptions.Count);
        }

        [Fact]
        public static void HandleInvalidCases()
        {
            AggregateException ex = new AggregateException();
            Assert.Throws<ArgumentNullException>(() => ex.Handle(null));

            ex = new AggregateException(new[] { new Exception(), new ArgumentException(), new ArgumentException() });
            int handledCount = 0;
            Assert.Throws<AggregateException>(
               () => ex.Handle((e) =>
               {
                   if (e is ArgumentException)
                   {
                       handledCount++;
                       return true;
                   }
                   return false;
               }));
        }

        // Validates that flattening (including recursive) works.
        [Fact]
        public static void Flatten()
        {
            Exception exceptionA = new Exception("A");
            Exception exceptionB = new Exception("B");
            Exception exceptionC = new Exception("C");

            AggregateException aggExceptionBase = new AggregateException(exceptionA, exceptionB, exceptionC);

            // Verify flattening one with another.
            // > Flattening (no recursion)...

            AggregateException flattened1 = aggExceptionBase.Flatten();
            Exception[] expected1 = new Exception[] {
                exceptionA, exceptionB, exceptionC
            };

            Assert.Equal(expected1, flattened1.InnerExceptions);

            // Verify flattening one with another, accounting for recursion.
            AggregateException aggExceptionRecurse = new AggregateException(aggExceptionBase, aggExceptionBase);
            AggregateException flattened2 = aggExceptionRecurse.Flatten();
            Exception[] expected2 = new Exception[] {
                exceptionA, exceptionB, exceptionC, exceptionA, exceptionB, exceptionC,
            };

            Assert.Equal(expected2, flattened2.InnerExceptions);
        }
    }
}
