using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Candado.Desktop.ViewModels
{
    public class AccountsViewModel : Screen, IView
    {
        private const string PasswordBoxControl = "PasswordBoxControl";
        private readonly IAccountService AccountService;
        private readonly ICryptoService CryptoService;
        private readonly IDialogService DialogService;
        private AccountViewModel _account;

        public AccountsViewModel(IAccountService accountService, IDialogService dialogService, ICryptoService cryptoService)
        {
            AccountService = accountService;
            DialogService = dialogService;
            CryptoService = cryptoService;

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
            var viewModel = new AccountViewModel();

            Accounts.Add(viewModel);

            Account = viewModel;
        }

        public override void CanClose(Action<bool> callback)
        {
            callback(DialogService.Confirm("Are you sure you want to exit? You might have unsaved changes."));
        }

        public void DeleteAccount()
        {
            try
            {
                if (Account == null) return;

                if (!DialogService.Confirm("Are you sure you want to delete this account?")) return;

                if (Account.IsReadOnlyName)
                {
                    AccountService.DeleteAccount(Account.Name);
                }

                Accounts.Remove(Account);

                Account = Accounts.FirstOrDefault();
            }
            catch (Exception e)
            {
                DialogService.Error(e.Message);
            }
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

                var duplicate = Accounts.GroupBy(vm => vm.Name).Any(grp => grp.Count() > 1);

                if (duplicate)
                {
                    DialogService.Error("Duplicate account names are not allowed.");

                    return;
                }

                Func<string, string> encrypt = text => CryptoService.Encrypt(AccountService.GetSecretKey(), text);

                foreach (var vm in Accounts)
                {
                    AccountService.SaveAccount(vm.ViewModelToModel(encrypt));

                    vm.SetReadOnlyName();
                }
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
                $"No password available for account '{Account.Name}'" :
                $"Password for account '{Account.Name}':\n\n{Account.Password}";

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
                Accounts.Clear();

                var items = AccountService.GetAccounts().OrderBy(x => x.Name);

                Func<string, string> dencrypt = text => CryptoService.Decrypt(AccountService.GetSecretKey(), text);

                foreach (var item in items)
                {
                    Accounts.Add(new AccountViewModel(item, dencrypt));
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
    }
}