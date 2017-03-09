namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private

        static Activity()
        {
            Random random = new Random();

            //Randomized on different process instances
            s_currentRootId = random.Next();

            //generate unique instance prefix 
            int uniqNum = unchecked((int)Stopwatch.GetTimestamp());
            s_uniqPrefix = $"{s_rootIdPrefix}{Environment.MachineName}-{uniqNum:x}";
        }

        #endregion
    }
}
