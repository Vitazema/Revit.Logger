namespace RevitLogProjectLocation
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Revit_Lib;

    /// <summary>
    /// Обработчик ошибок Ревит
    /// </summary>
    public class FailureProcessor : IFailuresProcessor
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
                /*FailuresAccessor failuresAccessor = e.GetFailuresAccessor();
                string transactionName = failuresAccessor.GetTransactionName();
                failuresAccessor.DeleteAllWarnings();
                IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();
                if (fmas.Count == 0)
                {
                    e.SetProcessingResult(FailureProcessingResult.Continue);
                    return;
                    Общие площадки в связи «link.rvt» были изменены, но не сохранены.
                    При повторном открытии экземпляры связи будут сброшены до последнего сохранения.
                    Вы можете сохранить связь позже в диалоговом окне «Диспетчер связей».
                  GetFailureDefinitionId =  cb11f811-2e36-4ab7-bce4-92eee381f058
                }

                foreach (FailureMessageAccessor fma in fmas)
                {
                    try
                    {
                        failuresAccessor.ResolveFailure(fma);
                    }
                    catch
                    {
                    }
                }*/

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

                if (!AccessHelper.IsBIM)
                {
                    foreach (FailureMessageAccessor fm in failures)
                    {
                        var description = fm.GetDescriptionText();
                        e.SetProcessingResult(FailureProcessingResult.WaitForUserInput);
                    }
                }
            }
            catch
            {
            }
        }

        public FailureProcessingResult ProcessFailures(FailuresAccessor data)
        {
            throw new System.NotImplementedException();
        }

        public void Dismiss(Document document)
        {
            throw new System.NotImplementedException();
        }
    }
}