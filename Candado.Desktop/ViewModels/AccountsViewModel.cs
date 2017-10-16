using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Candado.Desktop.ViewModels
{
    public class AccountsViewModel : Screen, IView
    {
        private const string CommandLineFlag = "-EditMode";
        private readonly IAccountService AccountService;
        private readonly ICryptoService CryptoService;
        private readonly IDialogService DialogService;
        private readonly ISecretKeyProvider SecretKeyProvider;
        private AccountViewModel _account;
        private string _filter;
        private string _status;

        public AccountsViewModel(
            IAccountService accountService, IDialogService dialogService,
            ICryptoService cryptoService, ISecretKeyProvider secretKeyProvider)
        {
            AccountService = accountService;
            DialogService = dialogService;
            CryptoService = cryptoService;
            SecretKeyProvider = secretKeyProvider;

            AccountViewModels = new BindableCollection<AccountViewModel>();
            AccountViewSource = new CollectionViewSource
            {
                Source = AccountViewModels
            };
            AccountViewSource.Filter -= AccountViewSource_Filter;
            AccountViewSource.Filter += AccountViewSource_Filter;
        }

        public AccountViewModel Account
        {
            get { return _account; }
            set
            {
                _account = value;
                NotifyOfPropertyChange();
            }
        }

        public ICollectionView Accounts => AccountViewSource.View;

        public bool CanEdit { get; private set; }

        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                AccountViewSource.View.Refresh();
                NotifyOfPropertyChange();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyOfPropertyChange();
            }
        }

        internal BindableCollection<AccountViewModel> AccountViewModels { get; }

        private CollectionViewSource AccountViewSource { get; }

        public void AddAccount()
        {
            var viewModel = new AccountViewModel();

            AccountViewModels.Add(viewModel);

            Account = viewModel;

            Status = string.Empty;
        }

        public override void CanClose(Action<bool> callback)
        {
            if (CanEdit)
            {
                var canClose = DialogService.Confirm("Are you sure you want to exit? You might have unsaved changes.");

                callback(canClose);

                return;
            }

            callback(true);
        }

        public void DeleteAccount()
        {
            try
            {
                if (Account == null) return;

                if (!DialogService.Confirm("Are you sure you want to delete this account?")) return;

                if (Account.IsReadOnlyName)
                {
                    AccountService.Delete(Account.AccountName);
                }

                Status = $"'{Account.AccountName}' account deleted!";

                AccountViewModels.Remove(Account);

                Account = AccountViewModels.FirstOrDefault();
            }
            catch (Exception e)
            {
                DialogService.Exception(e);
            }
        }

        public void SaveChanges()
        {
            try
            {
                var canSave = AccountViewModels.All(vm => vm.CanSave());

                if (!canSave)
                {
                    DialogService.Error("Not all accounts are valid. Account Name is required.");

                    return;
                }

                var duplicate = AccountViewModels.GroupBy(vm => vm.AccountName).Any(grp => grp.Count() > 1);

                if (duplicate)
                {
                    DialogService.Error("Duplicate account names are not allowed.");

                    return;
                }

                Func<string, string> encrypt = text => CryptoService.Encrypt(SecretKeyProvider.GetSecretKey(), text);

                foreach (var vm in AccountViewModels)
                {
                    var model = vm.ViewModelToModel(encrypt);

                    AccountService.Upsert(model);

                    vm.SetReadOnlyName();
                }

                Status = "Accounts saved...";
            }
            catch (Exception e)
            {
                DialogService.Exception(e);
            }
        }

        public void ViewAccount()
        {
            if (Account == null) return;

            var message = string.IsNullOrEmpty(Account.Password) ?
                $"No password available for account '{Account.AccountName}'" :
                $"Password for account '{Account.AccountName}':\n\n{Account.Password}";

            DialogService.Notify(message);
        }

        protected override void OnDeactivate(bool close)
        {
            AccountViewSource.Filter -= AccountViewSource_Filter;

            base.OnDeactivate(close);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);

            try
            {
                CanEdit = Environment.GetCommandLineArgs().Any(a => a == CommandLineFlag);

                NotifyOfPropertyChange(nameof(CanEdit));

                Func<string, string> dencrypt = text => CryptoService.Decrypt(SecretKeyProvider.GetSecretKey(), text);

                var items = AccountService.GetAll().OrderBy(x => x.Name);

                foreach (var item in items)
                {
                    AccountViewModels.Add(new AccountViewModel(item, dencrypt));
                }

                Account = AccountViewModels.FirstOrDefault();
            }
            catch (Exception e)
            {
                DialogService.Exception(e);
            }
        }

        private void AccountViewSource_Filter(object sender, FilterEventArgs e)
        {
            var viewModel = e.Item as AccountViewModel;

            if (String.IsNullOrEmpty(Filter) || Filter.Length == 0 || viewModel.AccountName.Length == 0)
            {
                e.Accepted = true;

                return;
            }

            e.Accepted = viewModel.AccountName.ToLower().Contains(Filter.ToLower());
        }
    }
}