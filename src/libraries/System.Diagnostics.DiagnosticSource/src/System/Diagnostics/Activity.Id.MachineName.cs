namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private
        private string generateUniquePrefix()
        {
            int uniqNum = (int)Stopwatch.GetTimestamp();
            return $"{s_rootIdPrefix}{Environment.MachineName}_{uniqNum:x}";
        }
        #endregion
    }
}
