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

        public ShellViewModel(IAccountService accountService, IDialogService dialogService, ICryptoService cryptoService, IEventAggregator eventAggregator)
        {
            DisplayName = "Candado";

            AccountService = accountService;
            DialogService = dialogService;
            CryptoService = cryptoService;
            EventAggregator = eventAggregator;

            EventAggregator.Subscribe(this);

            DisplayLoginView();
        }

        public void Handle(LoginEvent message)
        {
            var vm = new AccountsViewModel(AccountService, DialogService, CryptoService);

            ActivateItem(vm);
        }

        private void DisplayLoginView()
        {
            var vm = new LoginViewModel(EventAggregator, AccountService, DialogService);

            ActivateItem(vm);
        }
    }
}