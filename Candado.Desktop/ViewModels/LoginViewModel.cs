using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using Candado.Desktop.Events;
using System;

namespace Candado.Desktop.ViewModels
{
    public class LoginViewModel : Screen, IView
    {
        private readonly IAccountService AccountService;
        private readonly IDialogService DialogService;
        private readonly IEventAggregator EventAggregator;
        private string _password;

        public LoginViewModel(IEventAggregator eventAggregator, IAccountService accountService, IDialogService dialogService)
        {
            EventAggregator = eventAggregator;
            AccountService = accountService;
            DialogService = dialogService;
        }

        public bool CanLogin => !string.IsNullOrEmpty(Password);

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanLogin));
            }
        }

        public void Login()
        {
            try
            {
                if (!CanLogin) return;

                if (!AccountService.Authenticate(Password))
                {
                    DialogService.Error("Password is invalid.");

                    return;
                }

                EventAggregator.PublishOnUIThread(new LoginEvent());
            }
            catch (Exception e)
            {
                DialogService.Exception(e);
            }
        }
    }
}