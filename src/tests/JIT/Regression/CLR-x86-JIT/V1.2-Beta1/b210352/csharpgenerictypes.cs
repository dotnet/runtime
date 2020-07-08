// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

public class Utils
{
    public static int failures = 0;

    public static void Fail(string outputString)
    {
        failures++;
        // Console.WriteLine(outputString + new StackTrace(true).ToString());
        Console.WriteLine(outputString);
    }

    public static void CheckType<U>(U parameterToCheck, Type typeToCheckAgainst)
    {
        if (!typeToCheckAgainst.Equals(typeof(U)))
        {
            Fail("Expected typeof generic method Type Parameter for parameter, '" + parameterToCheck + "', to be '" + typeToCheckAgainst + "', but found '" + typeof(U) + "'");
        }

        if (!typeToCheckAgainst.Equals(parameterToCheck.GetType()))
        {
            Fail("Expected Type.GetType parameter, '" + parameterToCheck + "', to be '" + typeToCheckAgainst + "', but found '" + parameterToCheck.GetType() + "'");
        }
    }

    public static bool CompareArray<U>(U[] arrayOne, U[] arrayTwo)
    {
        for (int i = arrayOne.GetUpperBound(0); i >= 0; i--)
        {
            if (arrayOne[i].Equals(arrayTwo[i]))
            {
                return false;
            }
        }
        return true;
    }

    public static string BuildArrayString<U>(U[] arrayToPrint)
    {
        string stringToReturn = typeof(U) + " {";
        int i;

        for (i = 0; i <= arrayToPrint.GetUpperBound(0); i++)
        {
            stringToReturn = stringToReturn + arrayToPrint[i] + ", ";
        }

        stringToReturn.Remove(stringToReturn.Length - 2, 2);
        stringToReturn = stringToReturn + "}";
        return stringToReturn;
    }
}

public class GenericClass<T>
{
    public static Type classParameterType;
    public bool usingBaseVirtualProperty;

    public static U StaticGenericMethod<U>(U methodParameter, Type methodParameterType)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public static int StaticNonGenericMethodInt(int methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public static string StaticNonGenericMethodString(string methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public static int[] StaticNonGenericMethodIntArray(int[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public static string[] StaticNonGenericMethodStringArray(string[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public static U StaticGenericMethodUsesClassTypeParam<U>(T classParameter, U methodParameter, Type methodParameterType)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public static int StaticNonGenericMethodIntUsesClassTypeParam(T classParameter, int methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public static string StaticNonGenericMethodStringUsesClassTypeParam(T classParameter, string methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public static int[] StaticNonGenericMethodIntArrayUsesClassTypeParam(T classParameter, int[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public static string[] StaticNonGenericMethodStringArrayUsesClassTypeParam(T classParameter, string[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public U GenericMethod<U>(U methodParameter, Type methodParameterType)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public int NonGenericMethodInt(int methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public string NonGenericMethodString(string methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public int[] NonGenericMethodIntArray(int[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public string[] NonGenericMethodStringArray(string[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        return methodParameter;
    }

    public U GenericMethodUsesClassTypeParam<U>(T classParameter, U methodParameter, Type methodParameterType)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public int NonGenericMethodIntUsesClassTypeParam(T classParameter, int methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public string NonGenericMethodStringUsesClassTypeParam(T classParameter, string methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public int[] NonGenericMethodIntArrayUsesClassTypeParam(T classParameter, int[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public string[] NonGenericMethodStringArrayUsesClassTypeParam(T classParameter, string[] methodParameter, Type methodParameterType)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public virtual U VirtualGenericMethod<U>(U methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        return methodParameter;
    }

    public virtual int VirtualNonGenericMethodInt(int methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        return methodParameter;
    }

    public virtual string VirtualNonGenericMethodString(string methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        return methodParameter;
    }

    public virtual int[] VirtualNonGenericMethodIntArray(int[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        return methodParameter;
    }

    public virtual string[] VirtualNonGenericMethodStringArray(string[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        return methodParameter;
    }

    public virtual U VirtualGenericMethodUsesClassTypeParam<U>(T classParameter, U methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public virtual int VirtualNonGenericMethodIntUsesClassTypeParam(T classParameter, int methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public virtual string VirtualNonGenericMethodStringUsesClassTypeParam(T classParameter, string methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public virtual int[] VirtualNonGenericMethodIntArrayUsesClassTypeParam(T classParameter, int[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public virtual string[] VirtualNonGenericMethodStringArrayUsesClassTypeParam(T classParameter, string[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        if (isBase == false) { Utils.Fail("Expected to be true, but found it to be false."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public T genericField;
    public int nongenericIntField;
    public string nongenericStringField;
    public int[] nongenericIntArrayField;
    public string[] nongenericStringArrayField;

    public T genericProperty
    {
        get
        {
            Utils.CheckType<T>(genericField, classParameterType);
            return genericField;
        }

        set
        {
            Utils.CheckType<T>(value, classParameterType);
            genericField = value;
        }
    }

    public int nongenericIntProperty
    {
        get
        {
            return nongenericIntField;
        }

        set
        {
            nongenericIntField = value;
        }
    }

    public string nongenericStringProperty
    {
        get
        {
            return nongenericStringField;
        }

        set
        {
            nongenericStringField = value;
        }
    }

    public int[] nongenericIntArrayProperty
    {
        get
        {
            return nongenericIntArrayField;
        }

        set
        {
            nongenericIntArrayField = value;
        }
    }

    public string[] nongenericStringArrayProperty
    {
        get
        {
            return nongenericStringArrayField;
        }

        set
        {
            nongenericStringArrayField = value;
        }
    }

    public virtual T genericVirtualProperty
    {
        get
        {
            Utils.CheckType<T>(genericField, classParameterType);
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            return genericField;
        }

        set
        {
            Utils.CheckType<T>(value, classParameterType);
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            genericField = value;
        }
    }

    public virtual int nongenericIntVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            return nongenericIntField;
        }

        set
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            nongenericIntField = value;
        }
    }

    public virtual string nongenericStringVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            return nongenericStringField;
        }

        set
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            nongenericStringField = value;
        }
    }

    public virtual int[] nongenericIntArrayVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            return nongenericIntArrayField;
        }

        set
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            nongenericIntArrayField = value;
        }
    }

    public virtual string[] nongenericStringArrayVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            return nongenericStringArrayField;
        }

        set
        {
            if (usingBaseVirtualProperty == false) { Utils.Fail("Expected usingBaseVirtualProperty to be true, but found it to be false."); }
            nongenericStringArrayField = value;
        }
    }

    public delegate U genericDelegate<U>(U delegateParameter, Type typeOfDelegateParameter);
    public delegate int nongenericDelegateInt(int delegateParameter, Type typeOfDelegateParameter);
    public delegate string nongenericDelegateString(string delegateParameter, Type typeOfDelegateParameter);
    public delegate int[] nongenericDelegateIntArray(int[] delegateParameter, Type typeOfDelegateParameter);
    public delegate string[] nongenericDelegateStringArray(string[] delegateParameter, Type typeOfDelegateParameter);
    public delegate U genericDelegateUsesClassTypeParam<U>(T classParameter, U delegateParameter, Type typeOfDelegateParameter);
    public delegate int nongenericDelegateIntUsesClassTypeParam(T classParameter, int delegateParameter, Type typeOfDelegateParameter);
    public delegate string nongenericDelegateStringUsesClassTypeParam(T classParameter, string delegateParameter, Type typeOfDelegateParameter);
    public delegate int[] nongenericDelegateIntArrayUsesClassTypeParam(T classParameter, int[] delegateParameter, Type typeOfDelegateParameter);
    public delegate string[] nongenericDelegateStringArrayUsesClassTypeParam(T classParameter, string[] delegateParameter, Type typeOfDelegateParameter);
}

public class GenericClassInheritsFromGenericClass<T> : GenericClass<T>
{
    public override U VirtualGenericMethod<U>(U methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        return methodParameter;
    }

    public override int VirtualNonGenericMethodInt(int methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        return methodParameter;
    }

    public override string VirtualNonGenericMethodString(string methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        return methodParameter;
    }

    public override int[] VirtualNonGenericMethodIntArray(int[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        return methodParameter;
    }

    public override string[] VirtualNonGenericMethodStringArray(string[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        return methodParameter;
    }

    public override U VirtualGenericMethodUsesClassTypeParam<U>(T classParameter, U methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<U>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public override int VirtualNonGenericMethodIntUsesClassTypeParam(T classParameter, int methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public override string VirtualNonGenericMethodStringUsesClassTypeParam(T classParameter, string methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public override int[] VirtualNonGenericMethodIntArrayUsesClassTypeParam(T classParameter, int[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<int[]>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public override string[] VirtualNonGenericMethodStringArrayUsesClassTypeParam(T classParameter, string[] methodParameter, Type methodParameterType, bool isBase)
    {
        Utils.CheckType<string[]>(methodParameter, methodParameterType);
        if (isBase == true) { Utils.Fail("Expected to be false, but found it to be true."); }
        Utils.CheckType<T>(classParameter, classParameterType);
        return methodParameter;
    }

    public override T genericVirtualProperty
    {
        get
        {
            Utils.CheckType<T>(genericField, classParameterType);
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            return genericField;
        }

        set
        {
            Utils.CheckType<T>(value, classParameterType);
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            genericField = value;
        }
    }

    public override int nongenericIntVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            return nongenericIntField;
        }

        set
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            nongenericIntField = value;
        }
    }

    public override string nongenericStringVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            return nongenericStringField;
        }

        set
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            nongenericStringField = value;
        }
    }

    public override int[] nongenericIntArrayVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            return nongenericIntArrayField;
        }

        set
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            nongenericIntArrayField = value;
        }
    }

    public override string[] nongenericStringArrayVirtualProperty
    {
        get
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            return nongenericStringArrayField;
        }

        set
        {
            if (usingBaseVirtualProperty == true) { Utils.Fail("Expected usingBaseVirtualProperty to be false, but found it to be true."); }
            nongenericStringArrayField = value;
        }
    }
}
