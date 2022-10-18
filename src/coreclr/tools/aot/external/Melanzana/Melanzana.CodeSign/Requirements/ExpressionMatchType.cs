namespace Melanzana.CodeSign.Requirements
{
    public enum ExpressionMatchType : int
    {
        Exists,
        Equal,
        Contains,
        BeginsWith,
        EndsWith,
        LessThan,
        GreaterThan,
        LessEqual,
        GreaterEqual,
        On,
        Before,
        After,
        OnOrBefore,
        OnOrAfter,
        Absent,
    }
}