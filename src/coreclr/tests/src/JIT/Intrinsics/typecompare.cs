using System;

class Program
{
    static Type s_Object = typeof(object);
    static Type s_String = typeof(string);
    static Type s_ObjectArray = typeof(object[]);
    static Type s_StringArray = typeof(string[]);
    static Type s_intPointer = typeof(int*);
    static Type s_nullableDef = typeof(Nullable<>);

    static bool IsEqualShared1<T>(Type t)
    {
        return t == typeof(T);
    }

    static bool IsEqualShared2<T>(Type t)
    {
        return typeof(T) == t;
    }

    static bool IsNotEqualShared1<T>(Type t)
    {
        return t != typeof(T);
    }

    static bool IsNotEqualShared2<T>(Type t)
    {
        return typeof(T) != t;
    }

    static int Main()
    {
        if (typeof(object) != s_Object)
            return 1;

        if (s_String != typeof(string))
            return 2;

        if (typeof(string) == s_Object)
            return 3;

        if (s_String == typeof(object))
            return 4;

        if (!(typeof(object) == s_Object))
            return 5;

        if (!(s_String == typeof(string)))
            return 6;

        if (!(typeof(string) != s_Object))
            return 7;

        if (!(s_String != typeof(object)))
            return 8;

        if (s_StringArray != typeof(string[]))
            return 9;

        if (typeof(object[]) != s_ObjectArray)
            return 10;

        if (s_intPointer != typeof(int*))
            return 11;

        if (!IsEqualShared1<object>(typeof(object)))
            return 12;

        if (!IsEqualShared2<string>(typeof(string)))
            return 13;

        if (IsEqualShared1<string>(typeof(object)))
            return 14;

        if (IsNotEqualShared1<object>(typeof(object)))
            return 15;

        if (IsNotEqualShared2<string>(typeof(string)))
            return 16;

        if (s_nullableDef != typeof(Nullable<>))
            return 17;

        if (s_nullableDef == typeof(object))
            return 18;

        return 100;
    }
}
