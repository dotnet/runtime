// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class AggregateExceptionTests
    {
        [Fact]
        public static void RunAggregateException_Constructor()
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
        public static void RunAggregateException_Constructor_Exception()
        {
            AggregateException ex = new AggregateException();
            Assert.Throws<ArgumentNullException>(
               () => ex = new AggregateException("message", (Exception)null));

            Assert.Throws<ArgumentNullException>(
               () => ex = new AggregateException("message", (IEnumerable<Exception>)null));

            Assert.Throws<ArgumentException>(
               () => ex = new AggregateException("message", new[] { new Exception(), null }));
        }

        [Fact]
        public static void RunAggregateException_BaseException()
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
        public static void RunAggregateException_Handle()
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
        public static void RunAggregateException_Handle_Negative()
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

        // Validates that flattening (incl recursive) works.
        [Fact]
        public static void RunAggregateException_Flatten()
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

            if (expected1.Length != flattened1.InnerExceptions.Count)
            {
                Assert.True(false, string.Format("RunAggregateException_Flatten: > error: expected count {0} differs from actual {1}",
                    expected1.Length, flattened1.InnerExceptions.Count));
            }

            for (int i = 0; i < flattened1.InnerExceptions.Count; i++)
            {
                if (expected1[i] != flattened1.InnerExceptions[i])
                {
                    Debug.WriteLine("RunAggregateException_Flatten: > error: inner exception #{0} isn't right:", i);
                    Assert.True(false, string.Format("RunAggregateException_Flatten:      expected: {0}, found   : {1}", expected1[i], flattened1.InnerExceptions[i]));
                }
            }

            // Verify flattening one with another, accounting for recursion.
            // > Flattening (with recursion)...

            AggregateException aggExceptionRecurse = new AggregateException(aggExceptionBase, aggExceptionBase);
            AggregateException flattened2 = aggExceptionRecurse.Flatten();
            Exception[] expected2 = new Exception[] {
                exceptionA, exceptionB, exceptionC, exceptionA, exceptionB, exceptionC,
            };

            if (expected2.Length != flattened2.InnerExceptions.Count)
            {
                Assert.True(false, string.Format("RunAggregateException_Flatten:  FAILED.  > error: expected count {0} differs from actual {1}",
                    expected2.Length, flattened2.InnerExceptions.Count));
            }

            for (int i = 0; i < flattened2.InnerExceptions.Count; i++)
            {
                if (expected2[i] != flattened2.InnerExceptions[i])
                {
                    Assert.True(false, string.Format("RunAggregateException_Flatten:  FAILED.  > error: inner exception #{0} isn't right: expected - {1}, found - {2}", i, expected2[i], flattened2.InnerExceptions[i]));
                }
            }
        }
    }
}
