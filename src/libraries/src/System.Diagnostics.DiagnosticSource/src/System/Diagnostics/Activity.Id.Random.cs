namespace System.Diagnostics
{
    public partial class Activity
    {
        #region private

        private string GenerateInstancePrefix()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            return $"{RootIdPrefix}{BitConverter.ToUInt64(bytes, 8):x}";
        }

        #endregion
    }
}
