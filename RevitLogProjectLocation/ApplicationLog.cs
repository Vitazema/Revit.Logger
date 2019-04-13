namespace RevitLogProjectLocation
{
    using System;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;
    using Revit_Lib.IO;
    using RevitLogSdk.Dto;

    /// <summary>
    /// Application
    /// </summary>
    public class ApplicationLog : IExternalApplication
    {
        private LocationLogger _logger;

        /// <summary>
        /// Полное название активного документа
        /// </summary>
        public string ActiveDocumentFullPath { get; set; }

        /// <summary>
        /// Имя пользователя в Revit
        /// </summary>
        public string RevitUserName { get; set; }

        /// <summary>
        /// Имя активного документа
        /// </summary>
        private string ActiveDocumentTitle { get; set; }

        /// <summary>
        /// Событие загрузки приложения
        /// </summary>
        /// <param name="application">Апп</param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.ControlledApplication.DocumentChanged += OnDocumentChanged;
                application.ControlledApplication.DocumentOpened += OnDocumentOpened;
                application.ViewActivated += OnViewActivated;
                /*application.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(AppDialogShowing);*/
                application.ControlledApplication.FailuresProcessing += FailureProcessor.OnFailuresProcessing;
                _logger = new LocationLogger();
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
                application.ControlledApplication.DocumentChanged -= OnDocumentChanged;
                application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
                application.ViewActivated += OnViewActivated;
            }
            catch (Exception e)
            {
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Событие открытия документа
        /// </summary>
        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            var currentActiveDoc = e.Document;
            RevitUserName = currentActiveDoc?.Application.Username;
        }

        /// <summary>
        /// Событие активации вида
        /// </summary>
        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            var activeView = e.CurrentActiveView;
            var currentActiveDoc = activeView.Document;

            // Сохранение имени активного документа для использования в
            // валидации изменения площадки !текущего файла!
            RevitUserName = currentActiveDoc.Application.Username;
            ActiveDocumentFullPath = currentActiveDoc.GetSharingModelPath();
            ActiveDocumentTitle = currentActiveDoc.Title;
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            var doc = e.GetDocument();
            var modifiedElementsId = e.GetModifiedElementIds();

            // Кэширование данных (маркеры изменения координат)
            // Данные о расположении файла
            ProjectLocation projectLocation = null;
            BasePoint basePoint = null;

            // Площадка с общими координатами
            SiteLocation siteLocation = null;

            // Возможные провайдеры координат
            RevitLinkInstance rvtPositionProvider = null;
            ImportInstance dwgPositionProvider = null;

            foreach (var elementId in modifiedElementsId)
            {
                var element = doc.GetElement(elementId);

                // Пропускаем элементы, не относящиеся к данному файлу
                if (element == null || element.Document.Title != ActiveDocumentTitle)
                    continue;

                // Проверяем категорию изменённого элемента на принадлежность к маркерам изменения общих координат
                switch (element)
                {
                    case ProjectLocation loc:
                        projectLocation = loc.Document.ActiveProjectLocation;
                        break;
                    case BasePoint loc:
                        basePoint = loc;
                        break;
                    case RevitLinkInstance link:
                        rvtPositionProvider = link;
                        break;
                    case ImportInstance link:
                        dwgPositionProvider = link;
                        break;
                    case SiteLocation site:
                        siteLocation = site;
                        break;
                }
            }

            // Фильтруем события, не относящиеся к изменению площадок
            if (projectLocation == null)
                return;

            // ИВЕНТ ПРИНЯТИЯ КООРДИНАТ ИЗ СВЯЗИ
            if (siteLocation != null && (rvtPositionProvider != null || dwgPositionProvider != null))
            {
                var linkDoc = rvtPositionProvider?.GetLinkDocument();

                // не самый рабочий вариант, т.к. некрасивый и в случае с dwg - не предоставляет имя площадки провайдера
                var providerFileFullName = dwgPositionProvider != null
                    ? dwgPositionProvider.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString()
                    : linkDoc.GetSharingModelPath();

                // Извлечение имени площадки файла-провайдера
                string extractedSiteName = null;
                var providerSiteName = providerFileFullName.Split(':');
                if (providerSiteName.Count() > 1)
                {
                    var providerFileName = providerSiteName.Last().Trim();
                    var spaceIdx = providerFileName.IndexOf(" ", StringComparison.Ordinal) + 1;
                    extractedSiteName = providerFileName.Substring(spaceIdx);
                }

                // todo: Извлечение полного пути к файлу
                string positionProviderName = $"...типо путь к файлу/{providerFileFullName}";

/*/                if (rvtPositionProvider != null)
//                {
//                    providerFileFullName = rvtPositionProvider
//                        .GetExternalFileReference()
//                        .GetAbsolutePath()
//                        .CentralServerPath;
//                }
//                else
//                {
//                    providerFileFullName = dwgPositionProvider
//                        .GetExternalFileReference()
//                        .GetAbsolutePath()
//                        .CentralServerPath;
//                }
/*/

                var log = new LocationLogDto()
                {
                    SiteName = projectLocation.Name,
                    ParentFileName = ActiveDocumentTitle,
                    ParentFileFullName = ActiveDocumentFullPath,
                    ProviderFileName = positionProviderName,
                    ProviderFileFullName = providerFileFullName,
                    LocationBase = basePoint?.Location.ToXyzStr(),
                    ChangedDate = DateTime.Now,
                    Author = RevitUserName,
                    UserName = Environment.UserName,
                    Success = true
                };
            }

            // ИВЕНТ ИЗМЕНЕНИЯ КООРДИНАТ В ПРОЕКТЕ
            if (siteLocation == null)
            {
                var log = new LocationLogDto()
                {
                    SiteName = projectLocation.Name,
                    ParentFileName = ActiveDocumentTitle,
                    ParentFileFullName = ActiveDocumentFullPath,
                    ProviderFileName = null,
                    ProviderFileFullName = null,
                    LocationBase = basePoint?.Location.ToXyzStr(),
                    ChangedDate = DateTime.Now,
                    Author = RevitUserName,
                    UserName = Environment.UserName,
                    Success = true
                };

                // Запись в БД
                // _logger.WriteLog(log);
            }
        }

        private void AppDialogShowing(object sender, DialogBoxShowingEventArgs e)
        {
            if (e is TaskDialogShowingEventArgs window)
            {
                /*/*var type = window.DialogId;
                var msg = window.Message;#1#
            
                // Get the string id of the showing dialog
                string dialogId = e.DialogId;
                if (dialogId.Contains("Customer"))
                    return;
            
                // Format the prompt information string
                string promptInfo = "A Revit dialog will be opened.\n";
                promptInfo += "The DialogId of this dialog is " + dialogId + "\n";
                promptInfo += "If you don't want the dialog to open, please press cancel button";
            
                // Show the prompt message, and allow the user to close the dialog directly.
                TaskDialog taskDialog = new TaskDialog("Revit");
                taskDialog.Id = "Customer DialogId";
                taskDialog.MainContent = promptInfo;
                TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok |
                                                  TaskDialogCommonButtons.Cancel;
                taskDialog.CommonButtons = buttons;
                TaskDialogResult result = taskDialog.Show();
                if (result == TaskDialogResult.Cancel)
                {
                    // Do not show the Revit dialog
                    e.OverrideResult(1);
                }
                else
                {
                    // Continue to show the Revit dialog
                    e.OverrideResult(0);
                }*/
            }
        }
    }
}