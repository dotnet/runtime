.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'generics-variant-types'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'generics-variant-types'

// MVID: {8F67D18D-2212-4FF0-A9F5-5F954C42FCFA}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00F10000

//
// A Covariant interface
//
.class interface public abstract auto ansi beforefieldinit MyIEnumerator`1<+T>
{
	.method public hidebysig abstract virtual instance bool  MoveNext() cil managed
	{
	}

	.method public hidebysig abstract virtual instance !T get_Current () cil managed
	{
	}
	
	.property !T Current()
	{
    	.get instance !T MyIEnumerator`1::get_Current()
	}
}

//
// A Contravariant interface
//
.class interface public abstract auto ansi beforefieldinit IComparer`1<-T>
{
	.method public hidebysig abstract virtual instance bool  Compare(!T x, !T y) cil managed
	{
	}
}

.class interface public abstract auto ansi beforefieldinit IKeyComparer`1<-T> implements class IComparer`1<!T>
{
}

