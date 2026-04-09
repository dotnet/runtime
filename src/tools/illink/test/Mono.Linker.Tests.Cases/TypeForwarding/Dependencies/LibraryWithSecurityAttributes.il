.assembly extern mscorlib
{

}
.assembly Library
{

}
.module Library.dll

.class public auto ansi beforefieldinit LibraryWithSecurityAttributes
       extends [mscorlib]System.Object
{
  .permissionset demand
             = {[Forwarder]System.Security.Permissions.SecurityPermissionAttribute = {property enum [Forwarder]System.Security.Permissions.SecurityPermissionFlag 'Flags' = int32(16383)}}
  .class auto ansi nested private beforefieldinit NestedType
         extends [mscorlib]System.Object
  {
    .permissionset demand
               = {[Forwarder]System.Security.Permissions.SecurityPermissionAttribute = {property enum [Forwarder]System.Security.Permissions.SecurityPermissionFlag 'Flags' = int32(16383)}}
    .method public hidebysig instance void 
            OnAMethodInNestedClass() cil managed
    {
      .permissionset demand
                 = {[Forwarder]System.Security.Permissions.SecurityPermissionAttribute = {property enum [Forwarder]System.Security.Permissions.SecurityPermissionFlag 'Flags' = int32(16383)}}
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method NestedType::OnAMethodInNestedClass

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method NestedType::.ctor

  } // end of class NestedType

  .method public hidebysig instance void 
          OnAMethod() cil managed
  {
    .permissionset demand
               = {[Forwarder]System.Security.Permissions.SecurityPermissionAttribute = {property enum [Forwarder]System.Security.Permissions.SecurityPermissionFlag 'Flags' = int32(16383)}}
    // Code size       15 (0xf)
    .maxstack  1
    .locals init ([0] class LibraryWithSecurityAttributes/NestedType 'nested')
    IL_0000:  nop
    IL_0001:  newobj     instance void LibraryWithSecurityAttributes/NestedType::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void LibraryWithSecurityAttributes/NestedType::OnAMethodInNestedClass()
    IL_000d:  nop
    IL_000e:  ret
  } // end of method LibraryWithSecurityAttributes::OnAMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method LibraryWithSecurityAttributes::.ctor

} // end of class LibraryWithSecurityAttributes