using Caliburn.Micro;
using System;
using Candado.Core;

namespace Candado.Desktop.ViewModels
{
    public class AccountViewModel : PropertyChangedBase
    {
        private string _accountName;
        private string _userName;
        private string _password;
        private string _memo;

        public AccountViewModel(Account account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.AccountName)) throw new ArgumentNullException(nameof(account.AccountName));

            _accountName = account.AccountName;
            _userName = account.UserName;
            _password = account.Password;
            _memo = account.Memo;
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

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
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

        public string Memo
        {
            get { return _memo; }
            set
            {
                _memo = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanSave() => !string.IsNullOrEmpty(AccountName);

        public Account Model => new Account(AccountName, UserName, Password, Memo);
    }
}