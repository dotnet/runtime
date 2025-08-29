// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using JObject = nint;
using JString = nint;
using JObjectArray = nint;
using JSize = int;

namespace MonoDroid.NativeAOT;

#pragma warning disable IDE0060 // Remove unused parameter
internal static unsafe partial class MonoDroidExports
{
    // void Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value);
    [UnmanagedCallersOnly(EntryPoint = "Java_net_dot_MonoRunner_setEnv", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetEnv(JNIEnv* env, JObject thiz, JString j_key, JString j_value)
    {
        string? key = env->GetStringUTFChars(j_key);
        string? value = env->GetStringUTFChars(j_value);
        Console.WriteLine($"SetEnv: {key ?? "null"} = {value ?? "null"}");
        if (key != null && value != null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    // int Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_entryPointLibName, long current_local_time);
    [UnmanagedCallersOnly(EntryPoint = "Java_net_dot_MonoRunner_initRuntime", CallConvs = [typeof(CallConvCdecl)])]
    public static int InitRuntime(JNIEnv* env, JObject thiz, JString j_files_dir, JString j_entryPointLibName, long current_local_time)
    {
        Console.WriteLine("Initializing Android crypto native library");
        // The NativeAOT runtime does not need to be initialized, but the crypto library does.
        JavaVM* javaVM = env->GetJavaVM();
        AndroidCryptoNative_InitLibraryOnLoad(javaVM, null);
        return 0;
    }

    [LibraryImport("System.Security.Cryptography.Native.Android")]
    internal static partial int AndroidCryptoNative_InitLibraryOnLoad(JavaVM* vm, void* reserved);

    // int Java_net_dot_MonoRunner_execEntryPoint (JNIEnv* env, jobject thiz, jstring j_entryPointLibName, jobjectArray j_args);
    [UnmanagedCallersOnly(EntryPoint = "Java_net_dot_MonoRunner_execEntryPoint", CallConvs = [typeof(CallConvCdecl)])]
    public static int ExecEntryPoint(JNIEnv* env, JObject thiz, JString j_entryPointLibName, JObjectArray j_args)
    {
        int argc = env->GetArrayLength(j_args);
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            JObject j_arg = env->GetObjectArrayElement(j_args, i);
            args[i] = env->GetStringUTFChars((JString)j_arg);
        }

#if SINGLE_FILE_TEST_RUNNER
        string excludesFile = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "xunit-excludes.txt");
        if (File.Exists(excludesFile))
        {
            args = args.Concat(File.ReadAllLines(excludesFile).SelectMany(trait => new string[]{"-notrait", trait})).ToArray();
        }
        // SingleFile unit tests
        return SingleFileTestRunner.Main(args);
#else
        // Functional tests
        return Program.Main(args);
#endif
    }

    // void Java_net_dot_MonoRunner_freeNativeResources (JNIEnv* env, jobject thiz);
    [UnmanagedCallersOnly(EntryPoint = "Java_net_dot_MonoRunner_freeNativeResources", CallConvs = [typeof(CallConvCdecl)])]
    public static void FreeNativeResources(JNIEnv* env, JObject thiz)
    {
        // Placeholder for actual implementation
        Console.WriteLine("FreeNativeResources start");
    }
}


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JNIEnv
{
    JNINativeInterface* NativeInterface;
    public string? GetStringUTFChars(JString str)
    {
        fixed (JNIEnv* thisptr = &this)
        {
            byte* chars = NativeInterface->GetStringUTFChars(thisptr, str, out byte isCopy);
            if (chars is null)
                return null;

            string result = Marshal.PtrToStringUTF8((nint)chars);
            if (isCopy != 0)
                NativeInterface->ReleaseStringUTFChars(thisptr, str, chars);

            return result;
        }
    }

    public JavaVM* GetJavaVM()
    {
        fixed (JNIEnv* thisptr = &this)
        {
            JavaVM* vm;
            int result = NativeInterface->GetJavaVM(thisptr, &vm);
            if (result != 0)
                return null;

            return vm;
        }
    }

    public JSize GetArrayLength(JObjectArray array)
    {
        fixed (JNIEnv* thisptr = &this)
        {
            return NativeInterface->GetArrayLength(thisptr, array);
        }
    }

    public JObject GetObjectArrayElement(JObjectArray array, JSize index)
    {
        fixed (JNIEnv* thisptr = &this)
        {
            return NativeInterface->GetObjectArrayElement(thisptr, array, index);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct JNINativeInterface
    {
        void* reserved0;
        void* reserved1;
        void* reserved2;

        void* reserved3;
        void* GetVersion;

        void* DefineClass;
        void* FindClass;

        void* FromReflectedMethod;
        void* FromReflectedField;

        void* ToReflectedMethod;

        void* GetSuperclass;
        void* IsAssignableFrom;

        void* ToReflectedField;

        void* Throw;
        void* ThrowNew;
        void* ExceptionOccurred;
        void* ExceptionDescribe;
        void* ExceptionClear;
        void* FatalError;

        void* PushLocalFrame;
        void* PopLocalFrame;

        void* NewGlobalRef;
        void* DeleteGlobalRef;
        void* DeleteLocalRef;
        void* IsSameObject;
        void* NewLocalRef;
        void* EnsureLocalCapacity;

        void* AllocObject;
        void* NewObject;
        void* NewObjectV;
        void* NewObjectA;

        void* GetObjectClass;
        void* IsInstanceOf;

        void* GetMethodID;

        void* CallObjectMethod;
        void* CallObjectMethodV;
        void* CallObjectMethodA;

        void* CallBooleanMethod;
        void* CallBooleanMethodV;
        void* CallBooleanMethodA;

        void* CallByteMethod;
        void* CallByteMethodV;
        void* CallByteMethodA;

        void* CallCharMethod;
        void* CallCharMethodV;
        void* CallCharMethodA;

        void* CallShortMethod;
        void* CallShortMethodV;
        void* CallShortMethodA;

        void* CallIntMethod;
        void* CallIntMethodV;
        void* CallIntMethodA;

        void* CallLongMethod;
        void* CallLongMethodV;
        void* CallLongMethodA;

        void* CallFloatMethod;
        void* CallFloatMethodV;
        void* CallFloatMethodA;

        void* CallDoubleMethod;
        void* CallDoubleMethodV;
        void* CallDoubleMethodA;

        void* CallVoidMethod;
        void* CallVoidMethodV;
        void* CallVoidMethodA;

        void* CallNonvirtualObjectMethod;
        void* CallNonvirtualObjectMethodV;
        void* CallNonvirtualObjectMethodA;

        void* CallNonvirtualBooleanMethod;
        void* CallNonvirtualBooleanMethodV;
        void* CallNonvirtualBooleanMethodA;

        void* CallNonvirtualByteMethod;
        void* CallNonvirtualByteMethodV;
        void* CallNonvirtualByteMethodA;

        void* CallNonvirtualCharMethod;
        void* CallNonvirtualCharMethodV;
        void* CallNonvirtualCharMethodA;

        void* CallNonvirtualShortMethod;
        void* CallNonvirtualShortMethodV;
        void* CallNonvirtualShortMethodA;

        void* CallNonvirtualIntMethod;
        void* CallNonvirtualIntMethodV;
        void* CallNonvirtualIntMethodA;

        void* CallNonvirtualLongMethod;
        void* CallNonvirtualLongMethodV;
        void* CallNonvirtualLongMethodA;

        void* CallNonvirtualFloatMethod;
        void* CallNonvirtualFloatMethodV;
        void* CallNonvirtualFloatMethodA;

        void* CallNonvirtualDoubleMethod;
        void* CallNonvirtualDoubleMethodV;
        void* CallNonvirtualDoubleMethodA;

        void* CallNonvirtualVoidMethod;
        void* CallNonvirtualVoidMethodV;
        void* CallNonvirtualVoidMethodA;

        void* GetFieldID;

        void* GetObjectField;
        void* GetBooleanField;
        void* GetByteField;
        void* GetCharField;
        void* GetShortField;
        void* GetIntField;
        void* GetLongField;
        void* GetFloatField;
        void* GetDoubleField;

        void* SetObjectField;
        void* SetBooleanField;
        void* SetByteField;
        void* SetCharField;
        void* SetShortField;
        void* SetIntField;
        void* SetLongField;
        void* SetFloatField;
        void* SetDoubleField;

        void* GetStaticMethodID;

        void* CallStaticObjectMethod;
        void* CallStaticObjectMethodV;
        void* CallStaticObjectMethodA;

        void* CallStaticBooleanMethod;
        void* CallStaticBooleanMethodV;
        void* CallStaticBooleanMethodA;

        void* CallStaticByteMethod;
        void* CallStaticByteMethodV;
        void* CallStaticByteMethodA;

        void* CallStaticCharMethod;
        void* CallStaticCharMethodV;
        void* CallStaticCharMethodA;

        void* CallStaticShortMethod;
        void* CallStaticShortMethodV;
        void* CallStaticShortMethodA;

        void* CallStaticIntMethod;
        void* CallStaticIntMethodV;
        void* CallStaticIntMethodA;

        void* CallStaticLongMethod;
        void* CallStaticLongMethodV;
        void* CallStaticLongMethodA;

        void* CallStaticFloatMethod;
        void* CallStaticFloatMethodV;
        void* CallStaticFloatMethodA;

        void* CallStaticDoubleMethod;
        void* CallStaticDoubleMethodV;
        void* CallStaticDoubleMethodA;

        void* CallStaticVoidMethod;
        void* CallStaticVoidMethodV;
        void* CallStaticVoidMethodA;

        void* GetStaticFieldID;
        void* GetStaticObjectField;
        void* GetStaticBooleanField;
        void* GetStaticByteField;
        void* GetStaticCharField;
        void* GetStaticShortField;
        void* GetStaticIntField;
        void* GetStaticLongField;
        void* GetStaticFloatField;
        void* GetStaticDoubleField;

        void* SetStaticObjectField;
        void* SetStaticBooleanField;
        void* SetStaticByteField;
        void* SetStaticCharField;
        void* SetStaticShortField;
        void* SetStaticIntField;
        void* SetStaticLongField;
        void* SetStaticFloatField;
        void* SetStaticDoubleField;

        void* NewString;
        void* GetStringLength;
        void* GetStringChars;
        void* ReleaseStringChars;

        void* NewStringUTF;
        delegate* unmanaged[Cdecl]<JNIEnv*, JString, int> GetStringUTFLength;
        public delegate* unmanaged[Cdecl]<JNIEnv*, JString, out byte, byte*> GetStringUTFChars;
        public delegate* unmanaged[Cdecl]<JNIEnv*, JString, byte*, void> ReleaseStringUTFChars;
        public delegate* unmanaged[Cdecl]<JNIEnv*, JObjectArray, JSize> GetArrayLength;

        void* NewObjectArray;
        public delegate* unmanaged[Cdecl]<JNIEnv*, JObjectArray, JSize, JObject> GetObjectArrayElement;
        void* SetObjectArrayElement;

        void* NewBooleanArray;
        void* NewByteArray;
        void* NewCharArray;
        void* NewShortArray;
        void* NewIntArray;
        void* NewLongArray;
        void* NewFloatArray;
        void* NewDoubleArray;

        void* GetBooleanArrayElements;
        void* GetByteArrayElements;
        void* GetCharArrayElements;
        void* GetShortArrayElements;
        void* GetIntArrayElements;
        void* GetLongArrayElements;
        void* GetFloatArrayElements;
        void* GetDoubleArrayElements;

        void* ReleaseBooleanArrayElements;
        void* ReleaseByteArrayElements;
        void* ReleaseCharArrayElements;
        void* ReleaseShortArrayElements;
        void* ReleaseIntArrayElements;
        void* ReleaseLongArrayElements;
        void* ReleaseFloatArrayElements;
        void* ReleaseDoubleArrayElements;

        void* GetBooleanArrayRegion;
        void* GetByteArrayRegion;
        void* GetCharArrayRegion;
        void* GetShortArrayRegion;
        void* GetIntArrayRegion;
        void* GetLongArrayRegion;
        void* GetFloatArrayRegion;
        void* GetDoubleArrayRegion;

        void* SetBooleanArrayRegion;
        void* SetByteArrayRegion;
        void* SetCharArrayRegion;
        void* SetShortArrayRegion;
        void* SetIntArrayRegion;
        void* SetLongArrayRegion;
        void* SetFloatArrayRegion;
        void* SetDoubleArrayRegion;

        void* RegisterNatives;
        void* UnregisterNatives;

        void* MonitorEnter;
        void* MonitorExit;

        public delegate* unmanaged[Cdecl]<JNIEnv*, JavaVM**, int> GetJavaVM;

        void* GetStringRegion;
        void* GetStringUTFRegion;

        void* GetPrimitiveArrayCritical;
        void* ReleasePrimitiveArrayCritical;

        void* GetStringCritical;
        void* ReleaseStringCritical;

        void* NewWeakGlobalRef;
        void* DeleteWeakGlobalRef;

        void* ExceptionCheck;

        void* NewDirectByteBuffer;
        void* GetDirectBufferAddress;
        void* GetDirectBufferCapacity;

        /* New JNI 1.6 Features */

        void* GetObjectRefType;

        /* Module Features */

        void* GetModule;

        /* Virtual threads */

        void* IsVirtualThread;

        /* Large UTF8 Support */

        void* GetStringUTFLengthAsLong;
    };
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JavaVM
{
    JNIInvokeInterface* InvokeInterface;

    [StructLayout(LayoutKind.Sequential)]
    struct JNIInvokeInterface
    {
        void* reserved0;
        void* reserved1;
        void* reserved2;
        void* DestroyJavaVM;
        void* AttachCurrentThread;
        void* DetachCurrentThread;
        void* GetEnv;
        void* AttachCurrentThreadAsDaemon;
    }
}

#pragma warning restore IDE0060 // Remove unused parameter
