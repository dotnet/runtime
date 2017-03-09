namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private
        private string generateInstancePrefix()
        {
            int uniqNum = unchecked((int)Stopwatch.GetTimestamp());
            return $"{s_rootIdPrefix}{Environment.MachineName}-{uniqNum:x}";
        }
        #endregion
    }
}
