namespace RevitLogProjectLocation
{
    using System;
    using System.Diagnostics;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    /// <summary>
    /// Application
    /// </summary>
    public class ApplicationLog : IExternalApplication
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
                application.ControlledApplication.FailuresProcessing += FailureProcessor.OnFailuresProcessing;
            }
            catch (Exception e)
            {
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
            try
            {
                application.ControlledApplication.FailuresProcessing -= FailureProcessor.OnFailuresProcessing;
            }
            catch (Exception e)
            {
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}