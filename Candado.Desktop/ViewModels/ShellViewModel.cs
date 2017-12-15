using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using Candado.Desktop.Events;
using System;
using System.Linq;

namespace Candado.Desktop.ViewModels
{
    public class ShellViewModel : Conductor<IView>, IShell, IHandle<LoginEvent>
    {
        private const string CommandLineFlag = "-EditMode";
        private readonly IAuthenticationService AuthenticationService;
        private readonly IStorageService StorageService;
        private readonly ICryptoService CryptoService;
        private readonly IDialogService DialogService;
        private readonly IEventAggregator EventAggregator;

        public ShellViewModel(
            IStorageService storageService, IDialogService dialogService,
            ICryptoService cryptoService, IEventAggregator eventAggregator,
            IAuthenticationService authenticationService)
        {
            DisplayName = "Candado";

            StorageService = storageService;
            DialogService = dialogService;
            CryptoService = cryptoService;
            EventAggregator = eventAggregator;
            AuthenticationService = authenticationService;

            EventAggregator.Subscribe(this);

            ActivateItem(new LoginViewModel(EventAggregator, AuthenticationService, DialogService));
        }

        private bool CanEdit => Environment.GetCommandLineArgs().Any(a => a == CommandLineFlag);

        public void Handle(LoginEvent @event)
            => ActivateItem(new AccountsViewModel(StorageService, DialogService, CryptoService, CanEdit, @event.Password));

        protected override void OnDeactivate(bool close)
        {
            EventAggregator.Unsubscribe(this);

            base.OnDeactivate(close);
        }
    }
}