#define CORE_ASSEMBLY "System.Runtime"

// Metadata version: v4.0.30319
.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}

/*  ILasm currently complains when seeing a [netstandard]System.Object
    temporarily reference System.Runtime until we can get a fix in ilasm.
#define CORE_ASSEMBLY "netstandard"

// Metadata version: v4.0.30319
.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 2:0:0:0
}
*/