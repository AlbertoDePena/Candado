namespace Candado.Desktop.Contracts
{
    public interface ICryptoService
    {
        string Decrypt(string encryptedText);

        string Encrypt(string plainText);
    }
}