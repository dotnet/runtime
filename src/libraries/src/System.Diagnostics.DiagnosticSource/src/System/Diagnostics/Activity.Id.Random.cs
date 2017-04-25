namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private

        private string GenerateInstancePrefix()
        {
            return GetRandomNumber().ToString("x");
        }

        #endregion
    }
}
