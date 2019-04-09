namespace RevitLogProjectLocation
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Revit_Lib;

    /// <summary>
    /// Обработчик сообщений Ревит
    /// </summary>
    public static class FailureProcessor
    {
        /// <summary>
        /// Обработка внутренних сообщений Revit
        /// </summary>
        /// <param name="sender">Отправитель</param>
        /// <param name="e">Сообщение</param>
        public static void OnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            try
            {
                var f = e.GetFailuresAccessor();
                var transName = f.GetTransactionName();
                if (!transName.Equals("Изменение исходных общих координат"))
                    return;
                if (!AccessHelper.IsBIM)
                    e.SetProcessingResult(FailureProcessingResult.ProceedWithRollBack);
            }
            catch
            {
                // ignored
            }
        }
    }
}