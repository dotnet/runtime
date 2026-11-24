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

public class HolderUseCase { }

public class HolderDuplicateUseCase { }

public class HolderSource { }

public class HolderTarget1 : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 1, Text = nameof(HolderTarget1) };
}

public class HolderTarget2 : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 2, Text = nameof(HolderTarget2) };
}

public class HolderTarget3 : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 3, Text = nameof(HolderTarget3) };
}

public class HolderTarget4 : System.Runtime.InteropServices.ITypeMapValue<TypedValue>
{
    public static TypedValue Value => new TypedValue { Number = 4, Text = nameof(HolderTarget4) };
}

// Holder types in a different assembly than the one that links them.
[System.Runtime.InteropServices.TypeMap<HolderUseCase>("lib", typeof(HolderTarget1))]
[System.Runtime.InteropServices.TypeMapAssociation<HolderUseCase>(typeof(HolderSource), typeof(HolderTarget1))]
[System.Runtime.InteropServices.TypeMapTypeTarget<HolderUseCase>(typeof(HolderLibChained))]
public class HolderLib { }

[System.Runtime.InteropServices.TypeMap<HolderUseCase>("chained", typeof(HolderTarget2))]
public class HolderLibChained { }
