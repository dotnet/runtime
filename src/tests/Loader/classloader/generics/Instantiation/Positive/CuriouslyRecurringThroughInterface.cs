namespace CuriouslyRecurringPatternThroughInterface
{
    interface IGeneric<T_IGeneric>
    {
    }
    interface ICuriouslyRecurring<T_ICuriouslyRecurring> : IGeneric<CuriouslyRecurringThroughInterface<T_ICuriouslyRecurring>>
    {
    }
    class CuriouslyRecurringThroughInterface<T_CuriouslyRecurringThroughInterface> : ICuriouslyRecurring<T_CuriouslyRecurringThroughInterface>
    {
    }

    class Program
    {
        static object _o;
        static int Main(string[] args)
        {
            // Test that the a generic using a variant of the curiously recurring pattern involving an interface can be loaded.
            _o = typeof(CuriouslyRecurringThroughInterface<int>);
            return 100;
        }
    }
}