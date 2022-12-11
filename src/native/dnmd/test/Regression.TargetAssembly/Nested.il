.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern Regression.TargetAssembly.Ref { }
.assembly Regression.TargetAssembly { }

.class public auto ansi beforefieldinit A1
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ret
    }
}

.class public auto ansi beforefieldinit B1
    extends A1
{
    .class nested public auto ansi beforefieldinit C
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            ret
        }
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ret
    }
}

.class public auto ansi beforefieldinit B2
    extends [Regression.TargetAssembly.Ref]A2
{
    .class nested public auto ansi beforefieldinit C
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            ret
        }
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ret
    }
}