using System;

class FieldLayoutOffsetsTest
{
    static ContainsGCPointers.NoPointers _fld1;
    static ContainsGCPointers.StillNoPointers _fld2;
    static ContainsGCPointers.ClassNoPointers _fld3 = new ContainsGCPointers.ClassNoPointers();
    static ContainsGCPointers.HasPointers _fld4;
    static ContainsGCPointers.FieldHasPointers _fld5;
}
