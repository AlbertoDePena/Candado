namespace Candado.Desktop.Contracts
{
    public interface ICryptoService
    {
        string Decrypt(string key, string encryptedText);

        string Encrypt(string key, string plainText);
    }
}