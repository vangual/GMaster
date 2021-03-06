﻿namespace GMaster.Views.Commands
{
    using System.Threading.Tasks;
    using Models;
    using Tools;

    public class ConnectCommand : AbstractModelCommand<MainPageModel>
    {
        protected override bool InternalCanExecute() => Model.SelectedDevice != null;

        protected override Task InternalExecute()
        {
            Model.ConnectCamera(Model.SelectedDevice);
            return Task.CompletedTask;
        }
    }
}