using Caliburn.Micro;
using Candado.Core;
using System;

namespace Candado.Desktop.ViewModels
{
    public class AccountViewModel : PropertyChangedBase
    {
        private const string DefaultName = "New Account";
        private bool _canEditName;
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
            _canEditName = false;
        }

        public AccountViewModel()
        {
            _canEditName = true;
            _name = DefaultName;
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyOfPropertyChange();
            }
        }

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

        public bool ReadOnlyName => !_canEditName;

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
            _canEditName = false;

            NotifyOfPropertyChange(nameof(ReadOnlyName));
        }
    }
}