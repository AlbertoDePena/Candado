namespace Candado.Desktop.Events
{
    public class LoginEvent
    {
        public LoginEvent(string password)
        {
            Password = password;
        }

        public string Password { get; }
    }
}