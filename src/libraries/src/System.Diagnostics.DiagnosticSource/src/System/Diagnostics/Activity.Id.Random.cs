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
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);

            s_uniqPrefix = $"{s_rootIdPrefix}{BitConverter.ToUInt64(bytes, 0):x}";
        }

        #endregion
    }
}
