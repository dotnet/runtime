namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private

        private string GenerateInstancePrefix()
        {
            int uniqNum = unchecked((int)Stopwatch.GetTimestamp());
            return $"{uniqNum:x}-{Environment.MachineName}";
        }

        #endregion
    }
}
