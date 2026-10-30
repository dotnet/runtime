// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly DelegateTests
{
}

.class public auto ansi sealed Round776ReadonlyDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance void Invoke([in] object& input, object replacement) runtime managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round776RequiredDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance void Invoke([in] object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input, object replacement) runtime managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round776SingleReadonlyDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance void Invoke([in] object& input) runtime managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round776StaticInvokeDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig static void Invoke(int32& 'value') runtime managed {}
}

.class public auto ansi sealed Round776WritableReturnDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance object& Invoke() runtime managed {}
}

.class public auto ansi beforefieldinit Round776ReadonlyBase extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual instance void Read([in] object& input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }

    .method public hidebysig instance class Round776ReadonlyDelegate Delegate.Round776ExactLdftnBase_Valid() cil managed
    {
        ldarg.0
        ldftn instance void Round776ReadonlyBase::Read(object&, object)
        newobj instance void Round776ReadonlyDelegate::.ctor(object, native int)
        ret
    }
}

.class public auto ansi sealed Round776WritableOverride extends Round776ReadonlyBase
{
    .method public hidebysig virtual instance void Read(object& input, object replacement) cil managed
    {
        ldarg.1
        ldarg.2
        stind.ref
        ret
    }
}

.class public auto ansi sealed Round776ReadonlySealedOverride extends Round776ReadonlyBase
{
    .method public hidebysig virtual instance void Read([in] object& input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
}

.class interface public abstract auto ansi IRound776Readonly
{
    .method public hidebysig newslot abstract virtual instance void Read([in] object& input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round776WritableImplementation extends [System.Runtime]System.Object implements IRound776Readonly
{
    .method public hidebysig newslot virtual final instance void Read(object& input, object replacement) cil managed
    {
        ldarg.1
        ldarg.2
        stind.ref
        ret
    }
}

.class interface public abstract auto ansi IRound776Required
{
    .method public hidebysig newslot abstract virtual instance void Read([in] object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round776RequiredImplementation extends [System.Runtime]System.Object implements IRound776Required
{
    .method public hidebysig newslot virtual final instance void Read(object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input, object replacement) cil managed
    {
        ldarg.1
        ldind.ref
        pop
        ret
    }
}

.class public auto ansi sealed Round776RequiredMismatch_InvalidType_InterfaceMethodNotImplemented
        extends [System.Runtime]System.Object implements IRound776Required
{
    .method public hidebysig newslot virtual final instance void Read(object& input, object replacement) cil managed
    {
        ret
    }
}

.class public auto ansi sealed Round776RequiredMatched_ValidType_Valid
        extends [System.Runtime]System.Object implements IRound776Required
{
    .method public hidebysig newslot virtual final instance void Read([in] object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
}

.class public auto ansi sealed Round776RequiredWriter extends [System.Runtime]System.Object implements IRound776Required
{
    .method public hidebysig newslot virtual final instance void Delegate.Round776RequiredImplementationWrite_Invalid_ReadOnlyIllegalWrite(object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input, object replacement) cil managed
    {
        .override method instance void IRound776Required::Read(object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute), object)
        ldarg.1
        ldarg.2
        stind.ref
        ret
    }
}

.class public auto ansi beforefieldinit Round776DelegateCases extends [System.Runtime]System.Object
{
    .field private static object _global

    .method public hidebysig static object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) ReadonlyReturn() cil managed
    {
        .param [0]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldsflda object Round776DelegateCases::_global
        ret
    }

    .method public hidebysig static void ClosedRead(object state, [in] object& input) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }

    .method public hidebysig static void IntTarget(int32& 'value') cil managed
    {
        ret
    }

    .method public hidebysig static void ClosedWrite(object state, object& input) cil managed
    {
        ldarg.1
        ldnull
        stind.ref
        ret
    }

    .method public hidebysig static void Write(object& input, object replacement) cil managed
    {
        ldarg.0
        ldarg.1
        stind.ref
        ret
    }

    .method public hidebysig static void Read([in] object& input, object replacement) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldind.ref
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776WritableTargetMismatch_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::Write(object&, object)
        newobj instance void Round776ReadonlyDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776RequiredWritableMismatch_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::Write(object&, object)
        newobj instance void Round776RequiredDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776ReadonlyTarget_Valid() cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::Read(object&, object)
        newobj instance void Round776ReadonlyDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776RequiredReadonlyTarget_Valid() cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::Read(object&, object)
        newobj instance void Round776RequiredDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776CovariantDelegateChain_Invalid_DelegateCtor(object[] input) cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::Write(object&, object)
        newobj instance void Round776ReadonlyDelegate::.ctor(object, native int)
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void Round776ReadonlyDelegate::Invoke(object&, object)
        ret
    }

    .method public hidebysig static void Delegate.Round776ReadonlyInvoke_Valid(class Round776RequiredDelegate action, object[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void Round776RequiredDelegate::Invoke(object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute), object)
        ret
    }

    .method public hidebysig static void Delegate.Round776VirtualPointerMismatch_Invalid_DelegateCtor(class Round776ReadonlyBase reader) cil managed
    {
        ldarg.0
        dup
        ldvirtftn instance void Round776ReadonlyBase::Read(object&, object)
        newobj instance void Round776ReadonlyDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776UnresolvedVirtualReadonly_Invalid_StackUnexpected(class Round776ReadonlyBase reader, object[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void Round776ReadonlyBase::Read(object&, object)
        ret
    }

    .method public hidebysig static void Delegate.Round776UnresolvedInterfaceReadonly_Invalid_StackUnexpected(class IRound776Readonly reader, object[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void IRound776Readonly::Read(object&, object)
        ret
    }

    .method public hidebysig static void Delegate.Round776RequiredInterfaceReadonly_Valid(class IRound776Required reader, object[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void IRound776Required::Read(object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute), object)
        ret
    }

    .method public hidebysig static void Delegate.Round776SealedReadonlyVirtual_Valid(class Round776ReadonlySealedOverride reader, object[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        callvirt instance void Round776ReadonlySealedOverride::Read(object&, object)
        ret
    }

    .method public hidebysig static void Delegate.Round776ClosedStaticReadonly_Valid() cil managed
    {
        newobj instance void [System.Runtime]System.Object::.ctor()
        ldftn void Round776DelegateCases::ClosedRead(object, object&)
        newobj instance void Round776SingleReadonlyDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776ClosedStaticWritableMismatch_Invalid_DelegateCtor() cil managed
    {
        newobj instance void [System.Runtime]System.Object::.ctor()
        ldftn void Round776DelegateCases::ClosedWrite(object, object&)
        newobj instance void Round776SingleReadonlyDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776StaticInvoke_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn void Round776DelegateCases::IntTarget(int32&)
        newobj instance void Round776StaticInvokeDelegate::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.Round776ReadonlyReturnMismatch_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn object& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) Round776DelegateCases::ReadonlyReturn()
        newobj instance void Round776WritableReturnDelegate::.ctor(object, native int)
        pop
        ret
    }
}

.class public auto ansi sealed ByteEnum
       extends [System.Runtime]System.Enum
{
    .field public specialname rtspecialname uint8 value__
    .field public static literal valuetype ByteEnum A = uint8(0)
    .field public static literal valuetype ByteEnum B = uint8(0)
}

.class private auto ansi beforefieldinit DelegateTestsType
       extends [System.Runtime]System.Object
{
    // assignment from Func<int, string> to Func<int, object> is valid
    .method private hidebysig instance class [System.Runtime]System.Func`2<int32,object>
          DelegateAssignmentReturn_Valid(class [System.Runtime]System.Func`2<int32,string> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<object, int> to Func<string, int> is valid
    .method private hidebysig instance class [System.Runtime]System.Func`2<string,int32>
          DelegateAssignmentParameter_Valid(class [System.Runtime]System.Func`2<object,int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<string, int> to Func<object, int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`2<object,int32>
          DelegateAssignmentParameter_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<string,int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<int, object> to Func<int, string> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`2<int32,string>
          DelegateAssignment_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<int32,object> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<int> to Func<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<uint8>
          AssignIntFuncToByteFunc_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<byte> to Func<int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<int32>
          AssignByteFuncToIntFunc_Invalid_StackUnexpected(class [System.Runtime]System.Func`1<uint8> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<ByteEnum> to Func<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<uint8>
          AssignByteActionToIntAction_Invalid_StackUnexpected(class [System.Runtime]System.Func`1<valuetype ByteEnum> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Action<byte> to Action<int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Action`1<int32>
          AssignByteActionToIntAction_Invalid_StackUnexpected(class [System.Runtime]System.Action`1<uint8> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Action<ByteEnum> to Action<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Action`1<uint8>
          AssignByteActionToByteEnumAction_Invalid_StackUnexpected(class [System.Runtime]System.Action`1<valuetype ByteEnum> input) cil managed
    {
        ldarg.1
        ret
    }

    .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
    {
        ldarg.0
        call       instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
