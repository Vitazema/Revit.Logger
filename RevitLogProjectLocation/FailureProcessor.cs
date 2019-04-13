namespace RevitLogProjectLocation
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Revit_Lib;

    /// <summary>
    /// Обработчик ошибок Ревит
    /// </summary>
    public class FailureProcessor
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
                if (!transName.Equals("Изменение исходных общих координат")
                    && !transName.Equals("Перетаскивание")
                    && !transName.Equals("Перенести"))
                    return;

                var failures = f.GetFailureMessages();
                if (failures.Count == 0)
                {
                    e.SetProcessingResult(FailureProcessingResult.Continue);
                    return;
                }

                if (AccessHelper.IsBIM)
                    return;
                foreach (FailureMessageAccessor fm in failures)
                {
                    var description = fm.GetDescriptionText();
                    e.SetProcessingResult(FailureProcessingResult.WaitForUserInput);
                }
            }
            catch
            {
            }
        }
    }
}