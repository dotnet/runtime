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

    .method public newslot virtual hidebysig instance void MethodDef() cil managed
    {
        ret
    }
    
    .method public newslot virtual hidebysig instance vararg void MethodRef1(
        int32,
        uint16*,
        int64&,
        object[]) cil managed
    {
        ret
    }

    .method public newslot virtual hidebysig instance vararg void MethodRef2(
        valuetype [System.Runtime]System.Guid,
        string[][1...3,-1...,..., 4] modreq(A1) modopt([System.Runtime]System.Object) pinned,
        method void * (int32)) cil managed
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

    .method public virtual hidebysig instance void MethodDef() cil managed
    {
        .override A1::MethodDef
        ret
    }

    // We don't actually use these two methods, but we need them to provide the MemberRef tokens
    // that we use in the FindMethod tests.
    .method public virtual hidebysig instance vararg void MethodRefWithoutVarargsArguments(
        int32,
        uint16*,
        int64&,
        object[]) cil managed
    {
        .override method instance vararg void A1::MethodRef1(
            int32,
            uint16*,
            int64&,
            object[])
        ret
    }

    .method public virtual hidebysig instance vararg void MethodRefWithVarargsArguments(
        valuetype [System.Runtime]System.Guid,
        string[][1...3,-1...,..., 4] modreq(A1) modopt([System.Runtime]System.Object) pinned,
        method void * (int32)) cil managed
    {
        .override method instance vararg void A1::MethodRef2(
            valuetype [System.Runtime]System.Guid,
            string[][1...3,-1...,..., 4] modreq(A1) modopt([System.Runtime]System.Object) pinned,
            method void * (int32),
            ...,
            int32)
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

    .field public int32 Field1

    .field public int64 Field2
}