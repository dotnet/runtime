namespace TestMethodDebugInformation
{
    public partial class SimpleMath
    {
        public int Pow(int n, int d)
        {
            int i = 0;
            int j = 0;
            int res = 1;
            for (j = 0; j < d; j++)
            {
                res *= n;
                i++;
            }
            return res;
        }
    }
}
