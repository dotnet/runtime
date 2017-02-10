namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private
        private string generateUniquePrefix()
        {
            byte[] bytes = new byte[8];
            s_random.Value.NextBytes(bytes);

            return $"{s_rootIdPrefix}{BitConverter.ToUInt64(bytes, 0):x}";
        }
        #endregion
    }
}
