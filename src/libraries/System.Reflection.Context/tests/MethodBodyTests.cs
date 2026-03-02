// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test type with try-catch for MethodBody/ExceptionHandlingClause coverage
    internal class TypeWithTryCatch
    {
        public static int MethodWithTryCatch(int value)
        {
            int result = 0;
            try
            {
                result = value * 2;
                if (value < 0)
                {
                    throw new ArgumentException("Negative value");
                }
            }
            catch (ArgumentException)
            {
                result = -1;
            }
            finally
            {
                result += 1;
            }
            return result;
        }

        public int MethodWithLocals()
        {
            int a = 1;
            int b = 2;
            int c = a + b;
            return c;
        }
    }

    public class MethodBodyTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly MethodInfo _methodWithTryCatch;
        private readonly MethodInfo _methodWithLocals;
        private readonly MethodBody _methodBody;

        public MethodBodyTests()
        {
            TypeInfo typeInfo = typeof(TypeWithTryCatch).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _methodWithTryCatch = customTypeInfo.GetMethod("MethodWithTryCatch");
            _methodWithLocals = customTypeInfo.GetMethod("MethodWithLocals");
            _methodBody = _methodWithTryCatch.GetMethodBody();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void GetMethodBody_ReturnsProjectedMethodBody()
        {
            Assert.NotNull(_methodBody);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void ExceptionHandlingClauses_ReturnsProjectedClauses()
        {
            IList<ExceptionHandlingClause> clauses = _methodBody.ExceptionHandlingClauses;
            Assert.NotNull(clauses);
            // The method has try-catch-finally, so should have clauses
            Assert.True(clauses.Count >= 2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void LocalVariables_ReturnsProjectedVariables()
        {
            IList<LocalVariableInfo> locals = _methodBody.LocalVariables;
            Assert.NotNull(locals);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void InitLocals_ReturnsValue()
        {
            bool initLocals = _methodBody.InitLocals;
            Assert.True(initLocals);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void LocalSignatureMetadataToken_ReturnsValue()
        {
            int token = _methodBody.LocalSignatureMetadataToken;
            Assert.True(token >= 0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void MaxStackSize_ReturnsValue()
        {
            int maxStack = _methodBody.MaxStackSize;
            Assert.True(maxStack >= 0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void GetILAsByteArray_ReturnsBytes()
        {
            byte[] il = _methodBody.GetILAsByteArray();
            Assert.NotNull(il);
            Assert.NotEmpty(il);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void ToString_ReturnsValue()
        {
            string str = _methodBody.ToString();
            Assert.NotNull(str);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void MethodWithLocals_HasLocalVariables()
        {
            MethodBody body = _methodWithLocals.GetMethodBody();
            Assert.NotNull(body);
            IList<LocalVariableInfo> locals = body.LocalVariables;
            Assert.NotEmpty(locals);
        }
    }

    public class ExceptionHandlingClauseTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly ExceptionHandlingClause _catchClause;
        private readonly ExceptionHandlingClause _finallyClause;

        public ExceptionHandlingClauseTests()
        {
            TypeInfo typeInfo = typeof(TypeWithTryCatch).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customTypeInfo.GetMethod("MethodWithTryCatch");
            MethodBody body = method.GetMethodBody();
            IList<ExceptionHandlingClause> clauses = body.ExceptionHandlingClauses;

            _catchClause = clauses.FirstOrDefault(c => c.Flags == ExceptionHandlingClauseOptions.Clause);
            _finallyClause = clauses.FirstOrDefault(c => c.Flags == ExceptionHandlingClauseOptions.Finally);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void CatchClause_Exists()
        {
            Assert.NotNull(_catchClause);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void FinallyClause_Exists()
        {
            Assert.NotNull(_finallyClause);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void CatchType_ReturnsProjectedType()
        {
            if (_catchClause != null)
            {
                Type catchType = _catchClause.CatchType;
                Assert.NotNull(catchType);
                Assert.Equal(ProjectionConstants.CustomType, catchType.GetType().FullName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void Flags_ReturnsValue()
        {
            if (_catchClause != null)
            {
                ExceptionHandlingClauseOptions flags = _catchClause.Flags;
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, flags);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void HandlerLength_ReturnsValue()
        {
            if (_catchClause != null)
            {
                int length = _catchClause.HandlerLength;
                Assert.True(length > 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void HandlerOffset_ReturnsValue()
        {
            if (_catchClause != null)
            {
                int offset = _catchClause.HandlerOffset;
                Assert.True(offset >= 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void TryLength_ReturnsValue()
        {
            if (_catchClause != null)
            {
                int length = _catchClause.TryLength;
                Assert.True(length > 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void TryOffset_ReturnsValue()
        {
            if (_catchClause != null)
            {
                int offset = _catchClause.TryOffset;
                Assert.True(offset >= 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void ToString_ReturnsValue()
        {
            if (_catchClause != null)
            {
                string str = _catchClause.ToString();
                Assert.NotNull(str);
            }
        }
    }

    public class LocalVariableInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly LocalVariableInfo _localVariable;

        public LocalVariableInfoTests()
        {
            TypeInfo typeInfo = typeof(TypeWithTryCatch).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customTypeInfo.GetMethod("MethodWithLocals");
            MethodBody body = method.GetMethodBody();
            _localVariable = body.LocalVariables.FirstOrDefault();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void LocalVariable_Exists()
        {
            Assert.NotNull(_localVariable);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void LocalType_ReturnsProjectedType()
        {
            if (_localVariable != null)
            {
                Type localType = _localVariable.LocalType;
                Assert.NotNull(localType);
                Assert.Equal(ProjectionConstants.CustomType, localType.GetType().FullName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void LocalIndex_ReturnsValue()
        {
            if (_localVariable != null)
            {
                int index = _localVariable.LocalIndex;
                Assert.True(index >= 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void IsPinned_ReturnsFalse()
        {
            if (_localVariable != null)
            {
                bool isPinned = _localVariable.IsPinned;
                Assert.False(isPinned);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public void ToString_ReturnsValue()
        {
            if (_localVariable != null)
            {
                string str = _localVariable.ToString();
                Assert.NotNull(str);
            }
        }
    }
}
