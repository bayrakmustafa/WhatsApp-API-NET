namespace Tr.Com.Eimza.LibAxolotl
{
    public interface DecryptionCallback
    {
        void HandlePlaintext(byte[] plaintext);
    }
}