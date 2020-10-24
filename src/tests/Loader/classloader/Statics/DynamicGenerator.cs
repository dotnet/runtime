// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
// this class tries to return a delegate wrapping up a dynamic method created through reflection.emit or LCG
// the dynamic method is doing a ldsfld operation
// calling the method should return expected field value back
public interface IDynGen
{
    Delegate CreateDynGetValue(FieldInfo fi, bool useLCG);
    Delegate CreateDynSetValue(FieldInfo fi, bool useLCG);
}
public class DynamicGenerator<G> : IDynGen
{
    public delegate G DynGetValue();
    public delegate void DynSetValue(G g);

    AssemblyBuilder asmb = null;
    ModuleBuilder modb = null;
    TypeBuilder typeb = null;
    TypeBuilder typeb2 = null;
    static int tcount = 0;
    static int mcount = 0;
    public DynamicGenerator()
    {
        asmb = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("tempasm" + (tcount++)), AssemblyBuilderAccess.Run);
        modb = asmb.DefineDynamicModule("tempasm.dll");
        typeb = modb.DefineType("t" + mcount);
        typeb2 = modb.DefineType("t2" + mcount);
        mcount = 0;
    }
    public Delegate CreateDynGetValue(FieldInfo fi, bool useLCG)
    {
        ILGenerator ilgen = null;
        MethodInfo dm = null;
        if (useLCG)
        {
            dm = new DynamicMethod("dmset", typeof(G), new Type[] {}, typeof(Test).Module, true);
            ilgen = ((DynamicMethod)dm).GetILGenerator();
        }
        else
        {
            dm = typeb.DefineMethod("mset_" + mcount, MethodAttributes.Public | MethodAttributes.Static, typeof(G), new Type[] {});
            ilgen = ((MethodBuilder)dm).GetILGenerator();
        }
        ilgen.Emit(OpCodes.Ldsfld, fi);
        ilgen.Emit(OpCodes.Ret);

        if (dm is DynamicMethod)
            return ((DynamicMethod)dm).CreateDelegate(typeof(DynGetValue));
        else
        {
            Type t = typeb.CreateType();
            MethodInfo mi = t.GetMethod("mset_" + (mcount++));
            return Delegate.CreateDelegate(typeof(DynGetValue), mi);
        }
    }
    public Delegate CreateDynSetValue(FieldInfo fi, bool useLCG)
    {
        ILGenerator ilgen = null;
        MethodInfo dm = null;
        if (useLCG)
        {
            dm = new DynamicMethod("dmget", typeof(void), new Type[] {typeof(G)}, typeof(Test).Module, true);
            ilgen = ((DynamicMethod)dm).GetILGenerator();
        }
        else
        {
            dm = typeb2.DefineMethod("mget_" + mcount, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] {typeof(G)});
            ilgen = ((MethodBuilder)dm).GetILGenerator();
        }
        ilgen.Emit(OpCodes.Ldnull);
        ilgen.Emit(OpCodes.Ldarg_0);
        ilgen.Emit(OpCodes.Stfld, fi);
        ilgen.Emit(OpCodes.Ret);

        if (dm is DynamicMethod)
            return ((DynamicMethod)dm).CreateDelegate(typeof(DynSetValue));
        else
        {
            Type t = typeb2.CreateType();
            MethodInfo mi = t.GetMethod("mget_" + (mcount++));
            return Delegate.CreateDelegate(typeof(DynSetValue), mi);
        }

    }
}
