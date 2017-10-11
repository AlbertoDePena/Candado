using System;

namespace Candado.Desktop.Contracts
{
    public interface IDialogService
    {
        bool Confirm(string message);

        void Error(string message);

        void Notify(string message);

        void Exception(Exception e);
    }
}