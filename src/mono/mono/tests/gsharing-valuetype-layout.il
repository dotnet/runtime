.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'gsharing-valuetype-layout'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'gsharing-valuetype-layout.exe'


.namespace TestMonoAsyncGenerics
{
	.class private auto ansi beforefieldinit TestMonoAsyncGenerics extends [mscorlib]System.Object
	{

	.field public static int32 ldfld_res

	.method public static hidebysig  default class [mscorlib]System.Threading.Tasks.Task`1<!!METHOD_ARG> AsyncWithAwait<METHOD_ARG> (int32 a)  cil managed 
	{
		.maxstack 2
		.locals init (
			valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!METHOD_ARG> V_0,
			valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!!METHOD_ARG> V_1)
		ldloca.s 0
		ldarg.0 
		stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>::special_field	
		ldloca.s 0
		call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!!METHOD_ARG>::Create()
		stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>::'<>t__builder'
		ldloca.s 0
		ldc.i4.m1 
		stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>::'<>1__state'
		ldloca.s 0
		ldfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>::'<>t__builder'
		stloc.1 
		ldloca.s 1
		ldloca.s 0
		call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!!METHOD_ARG>::Start<valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>> ([out] !!0&)
		ldloca.s 0
		ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!!0>::'<>t__builder'
		call instance class [mscorlib]System.Threading.Tasks.Task`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!!METHOD_ARG>::get_Task()
		ret 
	}

	.method private static hidebysig default int32 Main (string[] args) cil managed 
	{
		.entrypoint
		.maxstack 8
		ldc.i4.s 0x30
		call class [mscorlib]System.Threading.Tasks.Task`1<!!0> class TestMonoAsyncGenerics.TestMonoAsyncGenerics::AsyncWithAwait<object> (int32)
		pop

		ldsfld int32 TestMonoAsyncGenerics.TestMonoAsyncGenerics::ldfld_res
		ldc.i4.s 0x30
		ceq
		ldc.i4.1
		sub
		ret
	}

	.method public hidebysig specialname rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.class nested private auto ansi sealed beforefieldinit '<AsyncWithAwait>d__0`1'<TYPE_ARG> extends [mscorlib]System.ValueType
		implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine  {

		.field  public  int32 '<>1__state'
		.field  public  valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> '<>t__builder'

		//we store/load from this field from very different contexts and thus triggering the runtime codepath that gives them
		//different runtime layouts.
		.field  public  int32 special_field
		.field  private  valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter '<>u__$awaiter1'
		.field  private  object '<>t__stack'

		.method private final virtual hidebysig newslot instance default void MoveNext ()  cil managed 
		{
			.override class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext
			.maxstack 3
			.locals init (
				bool V_0,
				!TYPE_ARG V_1,
				class [mscorlib]System.Exception V_2,
				int32 V_3,
				valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter V_4,
				valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter V_5,
				!TYPE_ARG V_6)
			.try {
				ldc.i4.1 
				stloc.0 
				ldarg.0 
				ldfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>1__state'
				stloc.3 
				ldloc.3 
				ldc.i4.0 
				beq.s LABEL_1

				ldstr "AsyncWithAwait {0}"
				ldarg.0 
				ldfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::special_field
				dup
				stsfld int32 TestMonoAsyncGenerics.TestMonoAsyncGenerics::ldfld_res
				box [mscorlib]System.Int32
				call void class [mscorlib]System.Console::WriteLine(string, object)
				ldc.i4.s 0x64
				call class [mscorlib]System.Threading.Tasks.Task class [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter class [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				stloc.s 4
				ldloca.s 4
				call instance bool valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				brtrue.s LABEL_2

				ldarg.0 
				ldc.i4.0 
				stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>1__state'
				ldarg.0 
				ldloc.s 4
				stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>u__$awaiter1'
				ldarg.0 
				ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>t__builder'
				ldloca.s 4
				ldarg.0 
				call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!TYPE_ARG>::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>> ([out] !!0&, [out] !!1&)
				ldc.i4.0 
				stloc.0 
				leave.s LABEL_3
LABEL_1:
				ldarg.0 
				ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>u__$awaiter1'
				stloc.s 4
				ldarg.0 
				ldloca.s 5
				initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				ldloc.s 5
				stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>u__$awaiter1'
				ldarg.0 
				ldc.i4.m1 
				stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>1__state'
LABEL_2:
				ldloca.s 4
				call instance void valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				ldloca.s 4
				initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				ldloca.s 6
				initobj !0
				ldloc.s 6
				stloc.1 
				leave.s LABEL_3
			}
			catch class [mscorlib]System.Exception {
				stloc.2 
				ldarg.0 
				ldc.i4.s 0xfffffffe
				stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>1__state'
				ldarg.0 
				ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>t__builder'
				ldloc.2 
				call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!TYPE_ARG>::SetException(class [mscorlib]System.Exception)
				leave.s LABEL_3
			}
			ldarg.0 
			ldc.i4.s 0xfffffffe
			stfld int32 valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>1__state'
			ldarg.0 
			ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>t__builder'
			ldloc.1 
			call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!TYPE_ARG>::SetResult(!0)
LABEL_3:
			ret
		}

		.method private final virtual hidebysig newslot instance default void SetStateMachine (class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine param0)  cil managed 
		{
			.override class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine
			.maxstack 8
			ldarg.0 
			ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype TestMonoAsyncGenerics.TestMonoAsyncGenerics/'<AsyncWithAwait>d__0`1'<!0>::'<>t__builder'
			ldarg.1 
			call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!TYPE_ARG>::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
			ret
		}
		}
	}
}
