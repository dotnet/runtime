namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private
        private string generateInstancePrefix()
        {
            byte[] bytes = new byte[8];
            s_random.NextBytes(bytes);

            return $"{s_rootIdPrefix}{BitConverter.ToUInt64(bytes, 0):x}";
        }
        #endregion
    }
}
