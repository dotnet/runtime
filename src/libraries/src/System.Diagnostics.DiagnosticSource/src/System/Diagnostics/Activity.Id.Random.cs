namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private

        private string GenerateInstancePrefix()
        {
            string suffix = GetRandomNumber().ToString("x");
            return RootIdPrefix + suffix;
        }

        #endregion
    }
}
