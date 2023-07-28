// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace ILCompiler.Dataflow
{
    internal sealed class CompilerGeneratedNames
    {
        private static bool IsGeneratedMemberName(string memberName)
        {
            return memberName.Length > 0 && memberName[0] == '<';
        }

        internal static bool IsLambdaDisplayClass(string className)
        {
            if (!IsGeneratedMemberName(className))
                return false;

            // This is true for static lambdas (which are emitted into a class like <>c)
            // and for instance lambdas (which are emitted into a class like <>c__DisplayClass1_0)
            return className.StartsWith("<>c");
        }

        internal static bool IsStateMachineType(string typeName)
        {
            if (!IsGeneratedMemberName(typeName))
                return false;

            // State machines are generated into types with names like <OwnerMethodName>d__0
            // Or if its nested in a local function the name will look like <<OwnerMethodName>g__Local>d and so on
            int i = typeName.LastIndexOf('>');
            if (i == -1)
                return false;

            return typeName.Length > i + 1 && typeName[i + 1] == 'd';
        }

        internal static bool IsStateMachineCurrentField(string fieldName)
        {
            if (!IsGeneratedMemberName(fieldName))
                return false;

            int i = fieldName.LastIndexOf('>');
            if (i == -1)
                return false;

            // Current field is <>2__current
            return fieldName.Length > i + 1 && fieldName[i + 1] == '2';
        }

        internal static bool IsStateMachineOrDisplayClass(string name) => IsStateMachineType(name) || IsLambdaDisplayClass(name);

        internal static bool IsLambdaOrLocalFunction(string methodName) => IsLambdaMethod(methodName) || IsLocalFunction(methodName);

        // Lambda methods have generated names like "<UserMethod>b__0_1" where "UserMethod" is the name
        // of the original user code that contains the lambda method declaration.
        internal static bool IsLambdaMethod(string methodName)
        {
            if (!IsGeneratedMemberName(methodName))
                return false;

            int i = methodName.IndexOf('>', 1);
            if (i == -1)
                return false;

            // Ignore the method ordinal/generation and lambda ordinal/generation.
            return methodName.Length > i + 1 && methodName[i + 1] == 'b';
        }

        // Local functions have generated names like "<UserMethod>g__LocalFunction|0_1" where "UserMethod" is the name
        // of the original user code that contains the lambda method declaration, and "LocalFunction" is the name of
        // the local function.
        internal static bool IsLocalFunction(string methodName)
        {
            if (!IsGeneratedMemberName(methodName))
                return false;

            int i = methodName.IndexOf('>', 1);
            if (i == -1)
                return false;

            // Ignore the method ordinal/generation and local function ordinal/generation.
            return methodName.Length > i + 1 && methodName[i + 1] == 'g';
        }
    }
}
