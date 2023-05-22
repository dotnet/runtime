// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract class ILGenerator
    {
        protected ILGenerator()
        {
        }

        protected abstract void EnsureCapacity(int size);
        protected abstract void ILEmit(OpCode opcode);
        protected abstract void WriteByte(byte arg);
        protected abstract void WriteShort(short arg);
        protected abstract void WriteInt(int arg);
        protected abstract void WriteLong(long arg);
        protected abstract void WriteSingle(float arg);
        protected abstract void WriteDouble(double arg);

        #region Public Members

        #region Emit
        public virtual void Emit(OpCode opcode)
        {
            EnsureCapacity(3);
            ILEmit(opcode);
        }

        public virtual void Emit(OpCode opcode, byte arg)
        {
            EnsureCapacity(4);
            ILEmit(opcode);
            WriteByte(arg);
        }

        public virtual void Emit(OpCode opcode, short arg)
        {
            // Puts opcode onto the stream of instructions followed by arg
            EnsureCapacity(5);
            ILEmit(opcode);
            WriteShort(arg);
        }

        public virtual void Emit(OpCode opcode, long arg)
        {
            EnsureCapacity(11);
            ILEmit(opcode);
            WriteLong(arg);
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            EnsureCapacity(7);
            ILEmit(opcode);
            WriteSingle(arg);
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            EnsureCapacity(11);
            ILEmit(opcode);
            WriteDouble(arg);
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            // Special-case several opcodes that have shorter variants for common values.
            if (opcode.Equals(OpCodes.Ldc_I4))
            {
                if (arg >= -1 && arg <= 8)
                {
                    opcode = arg switch
                    {
                        -1 => OpCodes.Ldc_I4_M1,
                        0 => OpCodes.Ldc_I4_0,
                        1 => OpCodes.Ldc_I4_1,
                        2 => OpCodes.Ldc_I4_2,
                        3 => OpCodes.Ldc_I4_3,
                        4 => OpCodes.Ldc_I4_4,
                        5 => OpCodes.Ldc_I4_5,
                        6 => OpCodes.Ldc_I4_6,
                        7 => OpCodes.Ldc_I4_7,
                        _ => OpCodes.Ldc_I4_8,
                    };
                    Emit(opcode);
                    return;
                }

                if (arg >= -128 && arg <= 127)
                {
                    Emit(OpCodes.Ldc_I4_S, (sbyte)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarg))
            {
                if ((uint)arg <= 3)
                {
                    Emit(arg switch
                    {
                        0 => OpCodes.Ldarg_0,
                        1 => OpCodes.Ldarg_1,
                        2 => OpCodes.Ldarg_2,
                        _ => OpCodes.Ldarg_3,
                    });
                    return;
                }

                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarg, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarga))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarga_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarga, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Starg))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Starg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Starg, (short)arg);
                    return;
                }
            }

            // For everything else, put the opcode followed by the arg onto the stream of instructions.
            EnsureCapacity(7);
            ILEmit(opcode);
            WriteInt(arg);
        }

        public abstract void Emit(OpCode opcode, MethodInfo meth);

        public abstract void EmitCalli(OpCode opcode, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes);

        public abstract void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes);

        public abstract void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes);

        public abstract void Emit(OpCode opcode, SignatureHelper signature);

        public abstract void Emit(OpCode opcode, ConstructorInfo con);

        public abstract void Emit(OpCode opcode, Type cls);

        public abstract void Emit(OpCode opcode, Label label);

        public abstract void Emit(OpCode opcode, Label[] labels);

        public abstract void Emit(OpCode opcode, FieldInfo field);

        public abstract void Emit(OpCode opcode, string str);

        public abstract void Emit(OpCode opcode, LocalBuilder local);
        #endregion

        #region Exceptions
        public abstract Label BeginExceptionBlock();

        public abstract void EndExceptionBlock();

        public abstract void BeginExceptFilterBlock();

        public abstract void BeginCatchBlock(Type? exceptionType);

        public abstract void BeginFaultBlock();

        public abstract void BeginFinallyBlock();

        #endregion

        #region Labels
        public abstract Label DefineLabel();

        public abstract void MarkLabel(Label loc);

        #endregion

        #region IL Macros
        public virtual void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
        {
            // Emits the il to throw an exception
            ArgumentNullException.ThrowIfNull(excType);

            if (!excType.IsSubclassOf(typeof(Exception)) && excType != typeof(Exception))
            {
                throw new ArgumentException(SR.Argument_NotExceptionType, nameof(excType));
            }
            ConstructorInfo? con = excType.GetConstructor(Type.EmptyTypes);
            if (con == null)
            {
                throw new ArgumentException(SR.Arg_NoDefCTorWithoutTypeName, nameof(excType));
            }
            Emit(OpCodes.Newobj, con);
            Emit(OpCodes.Throw);
        }

        private const string ConsoleTypeFullName = "System.Console, System.Console";
        private static readonly Type[] s_parameterTypes = new Type[] { typeof(string) };

        public virtual void EmitWriteLine(string value)
        {
            // Emits the IL to call Console.WriteLine with a string.
            Emit(OpCodes.Ldstr, value);
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo mi = consoleType.GetMethod("WriteLine", s_parameterTypes)!;
            Emit(OpCodes.Call, mi);
        }

        public virtual void EmitWriteLine(LocalBuilder localBuilder)
        {
            // Emits the IL necessary to call WriteLine with lcl.  It is
            // an error to call EmitWriteLine with a lcl which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the locals.

            /*if (m_methodBuilder == null)
            {
                throw new ArgumentException(SR.InvalidOperation_BadILGeneratorUsage);
            }*/

            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo prop = consoleType.GetMethod("get_Out")!;
            Emit(OpCodes.Call, prop);
            Emit(OpCodes.Ldloc, localBuilder);
            Type[] parameterTypes = new Type[1];
            Type cls = localBuilder.LocalType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new ArgumentException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = cls;
            MethodInfo? mi = typeof(System.IO.TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(SR.Argument_EmitWriteLineType, nameof(localBuilder));
            }

            Emit(OpCodes.Callvirt, mi);
        }

        public virtual void EmitWriteLine(FieldInfo fld)
        {
            ArgumentNullException.ThrowIfNull(fld);

            // Emits the IL necessary to call WriteLine with fld.  It is
            // an error to call EmitWriteLine with a fld which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the fields.
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo prop = consoleType.GetMethod("get_Out")!;
            Emit(OpCodes.Call, prop);

            if ((fld.Attributes & FieldAttributes.Static) != 0)
            {
                Emit(OpCodes.Ldsfld, fld);
            }
            else
            {
                Emit(OpCodes.Ldarg_0); // Load the this ref.
                Emit(OpCodes.Ldfld, fld);
            }
            Type[] parameterTypes = new Type[1];
            Type cls = fld.FieldType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new NotSupportedException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = cls;
            MethodInfo? mi = typeof(System.IO.TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(SR.Argument_EmitWriteLineType, nameof(fld));
            }

            Emit(OpCodes.Callvirt, mi);
        }

        #endregion

        #region Debug API
        public virtual LocalBuilder DeclareLocal(Type localType)
        {
            return DeclareLocal(localType, false);
        }

        public abstract LocalBuilder DeclareLocal(Type localType, bool pinned);

        public abstract void UsingNamespace(string usingNamespace);

        public abstract void BeginScope();

        public abstract void EndScope();

        public abstract int ILOffset { get; }

        [CLSCompliant(false)]
        public void Emit(OpCode opcode, sbyte arg) => Emit(opcode, (byte)arg);

        #endregion

        #endregion
    }
}
