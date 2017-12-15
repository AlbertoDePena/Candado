using Candado.Desktop.Contracts;
using System.Windows;
using System;
using Candado.Core;

namespace Candado.Desktop
{
    public class DialogService : IDialogService
    {
        private readonly string Title = "Candado";

        public bool Confirm(string message)
            => MessageBox.Show(message, Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        public void Error(string message)
            => MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Error);

        public void Exception(Exception e) 
            => Error(DataTypes.GetInnerMessage(e));

        public void Notify(string message)
            => MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}