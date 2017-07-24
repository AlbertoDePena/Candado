using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Candado.Desktop.ViewModels
{
    public class ShellViewModel : Screen, IShell
    {
        private const string PasswordBoxControl = "PasswordBoxControl";
        private readonly IAccountService AccountService;
        private readonly IDialogService DialogService;
        private AccountViewModel _account;

        public ShellViewModel(IAccountService accountService, IDialogService dialogService)
        {
            DisplayName = "Candado";
            AccountService = accountService;
            DialogService = dialogService;
            Accounts = new BindableCollection<AccountViewModel>();

            LoadAccounts();
        }

        public AccountViewModel Account
        {
            get { return _account; }
            set
            {
                _account = value;

                if (value != null && PasswordBox != null)
                {
                    PasswordBox.Password = value.Password;
                }

                NotifyOfPropertyChange();
            }
        }

        public BindableCollection<AccountViewModel> Accounts { get; }

        private PasswordBox PasswordBox { get; set; }

        public void AddAccount()
        {
            var viewModel = new AccountViewModel(new Account("New Account", "", "", ""));

            Accounts.Add(viewModel);

            Account = viewModel;
        }

        public void DeleteAccount()
        {
            if (Account == null) return;

            if (!DialogService.Confirm("Are you sure you want to delete this account?")) return;

            Accounts.Remove(Account);

            Account = Accounts.FirstOrDefault();
        }

        public void SaveChanges()
        {
            try
            {
                var canSave = Accounts.All(vm => vm.CanSave());

                if (!canSave)
                {
                    DialogService.Error("Not all accounts are valid. Account Name is required.");

                    return;
                }

                var duplicate = Accounts.GroupBy(vm => vm.AccountName).Any(grp => grp.Count() > 1);

                if (duplicate)
                {
                    DialogService.Error("Duplicate account names are not allowed.");

                    return;
                }

                var items = Accounts.Select(vm => vm.Model).ToArray();

                AccountService.SaveAll(items);
            }
            catch (Exception e)
            {
                DialogService.Error(e.Message);
            }
        }

        public void ViewAccount()
        {
            if (Account == null) return;

            var message = string.IsNullOrEmpty(Account.Password) ?
                $"No password available for {Account.AccountName}." :
                $"{Account.AccountName}: {Account.Password}";

            DialogService.Notify(message);
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

        private void LoadAccounts()
        {
            try
            {
                var items = AccountService.GetAll();

                foreach (var item in items)
                {
                    Accounts.Add(new AccountViewModel(item));
                }
            }
            catch (Exception e)
            {
                DialogService.Error(e.Message);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Account == null) return;

            Account.Password = PasswordBox.Password;
        }

        public override void CanClose(Action<bool> callback)
        {
            callback(DialogService.Confirm("Are you sure you want to exit? You might have unsaved changes."));
        }
    }
}