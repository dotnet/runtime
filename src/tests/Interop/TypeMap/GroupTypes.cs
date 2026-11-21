// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class C1
{
    public class I1 {}
    public class I2<T> {}
}

public struct S1 {}

public class C2<T>
{
    public class I1 {}
    public class I2<U> {}
}

public class TypicalUseCase { }

public class DuplicateTypeNameKey { }

public class InvalidTypeNameKey { }

public class MultipleTypeMapAssemblies { }

public class DuplicateTypeMapEntriesAcrossAssemblies { }

public class UnknownAssemblyReference { }

public class BlobOnlyAttributeTypeNames { }

public class TypedValueUseCase { }

public struct TypedValue
{
    public int Number;
    public string? Text;
}

public class TypedValueTarget : System.Runtime.InteropServices.ITypeMapValue<TypedValue>, System.Runtime.InteropServices.ITypeMapValue<string>
{
    static TypedValue System.Runtime.InteropServices.ITypeMapValue<TypedValue>.Value => new TypedValue { Number = 42, Text = nameof(TypedValue) };
    static string System.Runtime.InteropServices.ITypeMapValue<string>.Value => nameof(System.String);
}

public struct TypedValueStructTarget : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 7, Text = nameof(TypedValueStructTarget) };
}

public class TypedValueSource { }

public class TypedValueGenericTarget<T> : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 55, Text = nameof(TypedValueGenericTarget<T>) };
}

public class TypedValueGenericSource<T> { }
