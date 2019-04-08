namespace RevitLogProjectLocation
{
    using System;
    using System.Diagnostics;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    /// <summary>
    /// Application
    /// </summary>
    public class LogApplication : IExternalApplication
    {
        /// <summary>
        /// Событие загрузки приложения
        /// </summary>
        /// <param name="application">Апп</param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.DialogBoxShowing +=
                    new EventHandler<DialogBoxShowingEventArgs>(AppDialogShowing);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Событие закрытия приложения
        /// </summary>
        /// <param name="application">Апп</param>
        /// <returns></returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void AppDialogShowing(object sender, DialogBoxShowingEventArgs e)
        {
            if (!(e is TaskDialogShowingEventArgs window)) return;
            var type = window.DialogId;
            var msg = window.Message;
        }
    }
}