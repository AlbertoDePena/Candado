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
        private readonly ICryptoService CryptoService;
        private readonly IDialogService DialogService;
        private readonly IStorageService StorageService;
        private AccountViewModel _account;
        private string _filter;
        private string _status;

        public AccountsViewModel(
            IStorageService storageService, IDialogService dialogService,
            ICryptoService cryptoService, bool canEdit, string password)
        {
            StorageService = storageService;
            DialogService = dialogService;
            CryptoService = cryptoService;
            Password = password;
            CanEdit = canEdit;

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

        public bool CanEdit { get; }

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

        private string Password { get; }

        public void AddAccount()
        {
            var viewModel = new AccountViewModel();

            AccountViewModels.Add(viewModel);

            Account = viewModel;

            Status = string.Empty;
        }

        public override void CanClose(Action<bool> callback)
        {
            if (CanEdit && AccountViewModels.Any(vm => vm.HasChanges))
            {
                var canClose = DialogService.Confirm("You have unsaved changes. Are you sure you want to exit?");

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

                if (!DialogService.Confirm("Are you sure you want to delete this account even though it cannot be undone?")) return;

                if (Account.IsPersisted)
                {
                    StorageService.DeleteAccount(Password, Account.AccountName);
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

                string encrypt(string text) => CryptoService.Encrypt(StorageService.GetSecretKey(Password), text);

                foreach (var vm in AccountViewModels)
                {
                    var model = vm.ViewModelToModel(encrypt);

                    StorageService.UpsertAccount(Password, model);

                    vm.OnPostSave();
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
                string dencrypt(string text) => CryptoService.Decrypt(StorageService.GetSecretKey(Password), text);

                var items = StorageService.GetAllAccounts(Password).OrderBy(x => x.AccountName);

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