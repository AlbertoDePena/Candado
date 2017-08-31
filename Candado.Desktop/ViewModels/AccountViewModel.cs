using Caliburn.Micro;
using System;
using Candado.Core;

namespace Candado.Desktop.ViewModels
{
    public class AccountViewModel : PropertyChangedBase
    {
        private readonly int Id;
        private string _description;
        private string _name;
        private string _password;
        private string _userName;

        public AccountViewModel(Account account)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.Name))
                throw new ArgumentNullException(nameof(account.Name));

            Id = account.Id;
            _name = account.Name;
            _userName = account.UserName;
            _password = account.Password;
            _description = account.Description;
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

        public Account Model => new Account(Id, Name, UserName, Password, Description);

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
    }
}