using Caliburn.Micro;
using Candado.Core;
using System;

namespace Candado.Desktop.ViewModels
{
    public class AccountViewModel : PropertyChangedBase
    {
        private string _accountName;
        private string _description;
        private string _password;
        private string _userName;

        public AccountViewModel(Account account, Func<string, string> decrypt)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.Name))
                throw new ArgumentNullException(nameof(account.Name));

            _accountName = account.Name;
            _userName = account.Key;
            _password = String.IsNullOrEmpty(account.Psw) ? string.Empty : decrypt(account.Psw);
            _description = account.Desc;
            CanEditName = false;
        }

        public AccountViewModel()
        {
            _accountName = "New Account";
            CanEditName = true;
        }

        public string AccountName
        {
            get { return _accountName; }
            set
            {
                _accountName = value;
                NotifyOfPropertyChange();
            }
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

        public bool CanSave() => !string.IsNullOrEmpty(AccountName);

        public Account ViewModelToModel(Func<string, string> encrypt)
        {
            var password = String.IsNullOrEmpty(Password) ? string.Empty : encrypt(Password);

            return new Account(AccountName, UserName, password, Description);
        }

        internal void SetReadOnlyName()
        {
            CanEditName = false;

            NotifyOfPropertyChange(nameof(IsReadOnlyName));
        }
    }
}