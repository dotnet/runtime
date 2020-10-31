// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Security;

[assembly: InternalsVisibleTo("ClassFriend, PublicKey=00240000048000009400000006020000002400005253413100040000010001000fc5993e0f511ad5e16e8b226553493e09067afc41039f70daeb94a968d664f40e69a46b617d15d3d5328be7dbedd059eb98495a3b03cb4ea4ba127444671c3c84cbc1fdc393d7e10b5ee3f31f5a29f005e5eed7e3c9c8af74f413f0004f0c2cabb22f9dd4f75a6f599784e1bab70985ef8174ca6c684278be82ce055a03ebaf")]
[assembly: InternalsVisibleTo("StaticClassFriend, PublicKey=00240000048000009400000006020000002400005253413100040000010001000fc5993e0f511ad5e16e8b226553493e09067afc41039f70daeb94a968d664f40e69a46b617d15d3d5328be7dbedd059eb98495a3b03cb4ea4ba127444671c3c84cbc1fdc393d7e10b5ee3f31f5a29f005e5eed7e3c9c8af74f413f0004f0c2cabb22f9dd4f75a6f599784e1bab70985ef8174ca6c684278be82ce055a03ebaf")]
[assembly: InternalsVisibleTo("StructFriend, PublicKey=00240000048000009400000006020000002400005253413100040000010001000fc5993e0f511ad5e16e8b226553493e09067afc41039f70daeb94a968d664f40e69a46b617d15d3d5328be7dbedd059eb98495a3b03cb4ea4ba127444671c3c84cbc1fdc393d7e10b5ee3f31f5a29f005e5eed7e3c9c8af74f413f0004f0c2cabb22f9dd4f75a6f599784e1bab70985ef8174ca6c684278be82ce055a03ebaf")]


//
// This file can be compiled in two ways: ALL public (ALL_PUB), or Restricted.
//  The intention of the ALL public (ALL_PUB) is to allow a C# caller to build.
//  The restricted version is intended to be used at runtime, causing some
//   calls to fail.
//

//
// Enum
//
#if ALL_PUB
public
#endif
       enum DefaultEnum { VALUE1, VALUE3 }
public enum PublicEnum { VALUE1, VALUE3 }

#if ALL_PUB
public enum InternalEnum  { VALUE1, VALUE3 }
#else
internal enum InternalEnum  { VALUE1, VALUE3 }
#endif



//
// Class
//
#if ALL_PUB
	public
#endif
       class DefaultClass
{
}

public class PublicClass
{
	//
	// Ctors
	//
	public            PublicClass() {}
#if ALL_PUB
	public
#endif
	                   PublicClass(int i1) {}
	public             PublicClass(int i1, int i2) {}
#if ALL_PUB
	public             PublicClass(int i1, int i2, int i3) {}
	public             PublicClass(int i1, int i2, int i3, int i4) {}
	public             PublicClass(int i1, int i2, int i3, int i4, int i5) {}
	public             PublicClass(int i1, int i2, int i3, int i4, int i5, int i6) {}
#else
	protected          PublicClass(int i1, int i2, int i3) {}
	internal           PublicClass(int i1, int i2, int i3, int i4) {}
	protected internal PublicClass(int i1, int i2, int i3, int i4, int i5) {}
	private            PublicClass(int i1, int i2, int i3, int i4, int i5, int i6) {}
#endif


	//
	// static fields
	//
#pragma warning disable 169
#if ALL_PUB
	public
#endif
	                   static int defaultStaticField;
	public             static int publicStaticField = 0;
#if ALL_PUB
	public             static int protectedStaticField = 0;
	public             static int internalStaticField = 0;
	public             static int protectedInternalStaticField = 0;
	public             static int privateStaticField;
#else
	protected          static int protectedStaticField = 0;
	internal           static int internalStaticField = 0;
	protected internal static int protectedInternalStaticField = 0;
	private            static int privateStaticField;
#endif


	//
	// instance fields
	//
#if ALL_PUB
	public
#endif
	                   int defaultField;
	public             int publicField;
#if ALL_PUB
	public             int protectedField;
	public             int internalField;
	public             int protectedInternalField;
	public             int privateField;
#else
	protected          int protectedField;
	internal           int internalField;
	protected internal int protectedInternalField;
	private            int privateField;
#endif

#pragma warning restore 169
    //
	// static Properties
	//
#if ALL_PUB
	public
#endif
	                   static int DefaultStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int PublicStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#if ALL_PUB
	public             static int ProtectedStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int InternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int ProtectedInternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int PrivateStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#else
	protected          static int ProtectedStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	internal           static int InternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	protected internal static int ProtectedInternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	private            static int PrivateStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#endif

	//
	// instance Properties
	//
#if ALL_PUB
	public
#endif
	                   int DefaultProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int PublicProperty
	{
		get { return 0; }
		set { ; }
	}
#if ALL_PUB
	public             int ProtectedProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int InternalProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int ProtectedInternalProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int PrivateProperty
	{
		get { return 0; }
		set { ; }
	}
#else
	protected          int ProtectedProperty
	{
		get { return 0; }
		set { ; }
	}
	internal           int InternalProperty
	{
		get { return 0; }
		set { ; }
	}
	protected internal int ProtectedInternalProperty
	{
		get { return 0; }
		set { ; }
	}
	private            int PrivateProperty
	{
		get { return 0; }
		set { ; }
	}
#endif


	//
	// static Methods
	//
#if ALL_PUB
	public
#endif
	                   static int DefaultStaticMethod() { return 1; }
	public             static int PublicStaticMethod() { return 1; }
#if ALL_PUB
	public             static int ProtectedStaticMethod() { return 1; }
	public             static int InternalStaticMethod() { return 1; }
	public             static int ProtectedInternalStaticMethod() { return 1; }
	public             static int PrivateStaticMethod() { return 1; }
#else
	protected          static int ProtectedStaticMethod() { return 1; }
	internal           static int InternalStaticMethod() { return 1; }
	protected internal static int ProtectedInternalStaticMethod() { return 1; }
	private            static int PrivateStaticMethod() { return 1; }
#endif


	//
	// instance Methods
	//
#if ALL_PUB
	public
#endif
	                   int DefaultMethod() { return 1; }
	public             int PublicMethod() { return 1; }
#if ALL_PUB
	public             int ProtectedMethod() { return 1; }
	public             int InternalMethod() { return 1; }
	public             int ProtectedInternalMethod() { return 1; }
	public             int PrivateMethod() { return 1; }
#else
	protected          int ProtectedMethod() { return 1; }
	internal           int InternalMethod() { return 1; }
	protected internal int ProtectedInternalMethod() { return 1; }
	private            int PrivateMethod() { return 1; }
#endif
}

#if ALL_PUB
public   class InternalClass
#else
internal class InternalClass
#endif
{
}


//
// Static Class
//
#if ALL_PUB
public
#endif
         static class DefaultStaticClass
{
	public static int field = 0;
}

public   static class PublicStaticClass
{
	public static int field = 0;
}

#if ALL_PUB
public   static class InternalStaticClass
#else
internal static class InternalStaticClass
#endif
{
	public static int field = 0;
}


//
// Interface
//
#if ALL_PUB
public
#endif
       interface DefaultInterface
{
	bool Foo();
}


public interface PublicInterface
{
	bool Foo();
}

#if ALL_PUB
public   interface InternalInterface
#else
internal interface InternalInterface
#endif
{
	bool Foo();
}


//
// Struct
//
#if ALL_PUB
	public
#endif
       struct DefaultStruct {}
public struct PublicStruct
{
	//
	// Ctors
	//
#if ALL_PUB
	public
#endif
	                   PublicStruct(int i1) {defaultField=publicField=internalField=privateField=0;}
	public             PublicStruct(int i1, int i2) {defaultField=publicField=internalField=privateField=0;}
#if ALL_PUB
	public             PublicStruct(int i1, int i2, int i3) {defaultField=publicField=internalField=privateField=0;}
	public             PublicStruct(int i1, int i2, int i3, int i4) {defaultField=publicField=internalField=privateField=0;}
#else
	internal           PublicStruct(int i1, int i2, int i3) {defaultField=publicField=internalField=privateField=0;}
	private            PublicStruct(int i1, int i2, int i3, int i4) {defaultField=publicField=internalField=privateField=0;}
#endif

#pragma warning disable 414
    //
	// static fields
	//
#if ALL_PUB
	public
#endif
	                   static int defaultStaticField = 0;
	public             static int publicStaticField = 0;
#if ALL_PUB
	public             static int internalStaticField = 0;
	public             static int privateStaticField = 0;
#else
	internal           static int internalStaticField = 0;
	private            static int privateStaticField = 0;
#endif


	//
	// instance fields
	//
#if ALL_PUB
	public
#endif
	                   int defaultField;
	public             int publicField;
#if ALL_PUB
	public             int internalField;
	public             int privateField;
#else
	internal           int internalField;
	private            int privateField;
#endif

#pragma warning restore 414
    //
	// static Properties
	//
#if ALL_PUB
	public
#endif
	                   static int DefaultStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int PublicStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#if ALL_PUB
	public             static int InternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	public             static int PrivateStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#else
	internal           static int InternalStaticProperty
	{
		get { return 0; }
		set { ; }
	}
	private            static int PrivateStaticProperty
	{
		get { return 0; }
		set { ; }
	}
#endif

	//
	// instance Properties
	//
#if ALL_PUB
	public
#endif
	                   int DefaultProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int PublicProperty
	{
		get { return 0; }
		set { ; }
	}
#if ALL_PUB
	public             int InternalProperty
	{
		get { return 0; }
		set { ; }
	}
	public             int PrivateProperty
	{
		get { return 0; }
		set { ; }
	}
#else
	internal           int InternalProperty
	{
		get { return 0; }
		set { ; }
	}
	private            int PrivateProperty
	{
		get { return 0; }
		set { ; }
	}
#endif


	//
	// static Methods
	//
#if ALL_PUB
	public
#endif
	                   static int DefaultStaticMethod() { return 1; }
	public             static int PublicStaticMethod() { return 1; }
#if ALL_PUB
	public             static int InternalStaticMethod() { return 1; }
	public             static int PrivateStaticMethod() { return 1; }
#else
	internal           static int InternalStaticMethod() { return 1; }
	private            static int PrivateStaticMethod() { return 1; }
#endif


	//
	// instance Methods
	//
#if ALL_PUB
	public
#endif
	                   int DefaultMethod() { return 1; }
	public             int PublicMethod() { return 1; }
#if ALL_PUB
	public             int InternalMethod() { return 1; }
	public             int PrivateMethod() { return 1; }
#else
	internal           int InternalMethod() { return 1; }
	private            int PrivateMethod() { return 1; }
#endif
}

#if ALL_PUB
public   struct InternalStruct {};
#else
internal struct InternalStruct {};
#endif
       

