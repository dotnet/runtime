// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file makes NetStandard Reflection's "subclassing" surface area look as much like NetCore as possible so the rest of the code can be written without #if's.

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Another layer of base types. Empty for NetCore.
    /// </summary>
    internal abstract class LeveledTypeInfo : TypeInfo
    {
        protected LeveledTypeInfo() : base() { }
    }

    internal abstract class LeveledAssembly : Assembly
    {
    }

    internal abstract class LeveledConstructorInfo : ConstructorInfo
    {
    }

    internal abstract class LeveledMethodInfo : MethodInfo
    {
    }

    internal abstract class LeveledEventInfo : EventInfo
    {
    }

    internal abstract class LeveledFieldInfo : FieldInfo
    {
    }

    internal abstract class LeveledParameterInfo : ParameterInfo
    {
    }

    internal abstract class LeveledPropertyInfo : PropertyInfo
    {
    }

    internal abstract class LeveledCustomAttributeData : CustomAttributeData
    {
    }
}
