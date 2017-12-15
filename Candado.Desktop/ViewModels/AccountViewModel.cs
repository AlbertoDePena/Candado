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

        public AccountViewModel(Dtos.AccountDto account, Func<string, string> decrypt)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.AccountName))
                throw new ArgumentNullException(nameof(account.AccountName));

            _accountName = account.AccountName;
            _userName = account.UserName;
            _password = String.IsNullOrEmpty(account.Password) ? string.Empty : decrypt(account.Password);
            _description = account.Description;

            CanEditName = false;
            HasChanges = false;
        }

        public AccountViewModel()
        {
            _accountName = "New Account";
            CanEditName = true;
            HasChanges = true;
        }

        public string AccountName
        {
            get { return _accountName; }
            set
            {
                _accountName = value;
                HasChanges = true;
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
                HasChanges = true;
                NotifyOfPropertyChange();
            }
        }

        public bool HasChanges { get; private set; }

        public bool IsPersisted => !CanEditName;

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                HasChanges = true;
                NotifyOfPropertyChange();
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                HasChanges = true;
                NotifyOfPropertyChange();
            }
        }

        public bool CanSave() => !string.IsNullOrEmpty(AccountName);

        public Dtos.AccountDto ViewModelToModel(Func<string, string> encrypt)
        {
            var password = String.IsNullOrEmpty(Password) ? string.Empty : encrypt(Password);

            return new Dtos.AccountDto(AccountName, UserName, password, Description);
        }

        internal void OnPostSave()
        {
            CanEditName = false;
            HasChanges = false;

            NotifyOfPropertyChange(nameof(IsPersisted));
        }
    }
}