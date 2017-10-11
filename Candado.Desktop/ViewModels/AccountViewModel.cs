using Caliburn.Micro;
using Candado.Core;
using System;

namespace Candado.Desktop.ViewModels
{
    public class AccountViewModel : PropertyChangedBase
    {
        private string _description;
        private string _name;
        private string _password;
        private string _userName;

        public AccountViewModel(Account account, Func<string, string> decrypt)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.Name))
                throw new ArgumentNullException(nameof(account.Name));

            _name = account.Name;
            _userName = account.Key;
            _password = String.IsNullOrEmpty(account.Token) ? string.Empty : decrypt(account.Token);
            _description = account.Desc;
            CanEditName = false;
        }

        public AccountViewModel()
        {
            _name = "New Account";
            CanEditName = true;
        }

        public bool CanEditName { get; private set; }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsReadOnlyName => !CanEditName;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyOfPropertyChange();
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyOfPropertyChange();
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanSave() => !string.IsNullOrEmpty(Name);

        public Account ViewModelToModel(Func<string, string> encrypt)
        {
            var encryptedPassword = String.IsNullOrEmpty(Password) ? string.Empty : encrypt(Password);

            return new Account(Name, UserName, encryptedPassword, Description);
        }

        internal void SetReadOnlyName()
        {
            CanEditName = false;

            NotifyOfPropertyChange(nameof(IsReadOnlyName));
        }
    }
}