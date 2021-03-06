namespace GMaster.Views.Commands
{
    using System.Threading.Tasks;
    using Models;
    using Tools;

    public class CaptureCommand : AbstractModelCommand<CameraViewModel>
    {
        protected override bool InternalCanExecute() => Model?.SelectedCamera?.Camera != null;

        protected override async Task InternalExecute()
        {
            var lumix = Model.SelectedCamera;
            if (lumix == null)
            {
                return;
            }

            await lumix.Camera.Capture();
        }
    }
}