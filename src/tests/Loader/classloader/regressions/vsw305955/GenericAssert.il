// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly GenericAssert
{
	.hash algorithm 0x00008004
	.ver 0:0:0:0
      	.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 )  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
	.permissionset reqrefuse
		"<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\">" +
			"<IPermission class=\"System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600, Culture=neutral, PublicKeyToken=b77a5c561934e089\" version=\"1\"" +
				"Flags=\"SkipVerification\"/>" +
		"</PermissionSet>"
}
.module GenericAssert

.class interface public ContraVariantInterface<-T> { }
.class public interface StandardInterface { }

.class public A extends [mscorlib]System.Object implements StandardInterface
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed
	{
		.maxstack		1
		ldarg.0
		call			instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class public GenericAssert
{
	.method public hidebysig static int32 Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack 8

		.try
		{
			// get the type of ContraVariantInterface<A>
			ldtoken		class ContraVariantInterface<class A>
			call		class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			
			// now get the type of StandardInterface
			ldtoken		class StandardInterface
			call		class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)

			// make sure they're compatible
			callvirt	instance bool [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
			leave		PASS
		}
		catch [mscorlib]System.Exception
		{
			
			leave		FAIL
		}
		
PASS:
		ldc.i4			100
		ldstr			"Pass"
		call			void [System.Console]System.Console::WriteLine(string)
		ret
		
FAIL:
		ldc.i4			101
		ldstr			"FAIL"
		call			void [System.Console]System.Console::WriteLine(string)
		ret
	}
}

