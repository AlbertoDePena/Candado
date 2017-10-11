using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using Candado.Desktop.Events;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Candado.Desktop.ViewModels
{
    public class LoginViewModel : Screen, IView
    {
        private const string PasswordBoxControl = "PasswordBoxControl";
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
                NotifyOfPropertyChange(() => Password);
                NotifyOfPropertyChange(() => CanLogin);
            }
        }

        private PasswordBox PasswordBox { get; set; }

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
                DialogService.Error(e.Message);
            }
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            var frameworkElement = view as FrameworkElement;

            if (frameworkElement == null) return;

            PasswordBox = frameworkElement.FindName(PasswordBoxControl) as PasswordBox;

            if (PasswordBox == null)
            {
                DialogService.Notify("PasswordBox input not found.");

                return;
            }

            PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
        }
    }
}