using Caliburn.Micro;
using Candado.Core;
using Candado.Desktop.Contracts;
using Candado.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Candado.Desktop
{
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void BuildUp(object instance)
            => container.BuildUp(instance);

        protected override void Configure()
        {
            container = new SimpleContainer()
                            .Singleton<IWindowManager, CustomWindowManager>()
                            .Singleton<IDialogService, DialogService>()
                            .Singleton<IAccountService, AccountService>()
                            .Singleton<ICryptoService, CryptoService>()
                            .PerRequest<IShell, ShellViewModel>();
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
            => container.GetAllInstances(service);

        protected override object GetInstance(Type service, string key)
            => container.GetInstance(service, key);

        protected override void OnStartup(object sender, StartupEventArgs e)
            => DisplayRootViewFor<IShell>();
    }
}