using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using Candado.Desktop.Events;

namespace Candado.Desktop.ViewModels
{
    public class ShellViewModel : Conductor<IView>, IShell, IHandle<LoginEvent>
    {
        private readonly IAccountService AccountService;
        private readonly ICryptoService CryptoService;
        private readonly IDialogService DialogService;
        private readonly IEventAggregator EventAggregator;
        private readonly ISecretKeyProvider SecretKeyProvider;

        public ShellViewModel(
            IAccountService accountService, IDialogService dialogService, 
            ICryptoService cryptoService, IEventAggregator eventAggregator, 
            ISecretKeyProvider secretKeyProvider)
        {
            DisplayName = "Candado";

            AccountService = accountService;
            DialogService = dialogService;
            CryptoService = cryptoService;
            EventAggregator = eventAggregator;
            SecretKeyProvider = secretKeyProvider;

            EventAggregator.Subscribe(this);

            DisplayLoginView();
        }

        public void Handle(LoginEvent message)
        {
            var viewModel = new AccountsViewModel(AccountService, DialogService, CryptoService, SecretKeyProvider);

            ActivateItem(viewModel);
        }

        protected override void OnDeactivate(bool close)
        {
            EventAggregator.Unsubscribe(this);

            base.OnDeactivate(close);
        }

        private void DisplayLoginView()
        {
            var viewModel = new LoginViewModel(EventAggregator, AccountService, DialogService);

            ActivateItem(viewModel);
        }
    }
}