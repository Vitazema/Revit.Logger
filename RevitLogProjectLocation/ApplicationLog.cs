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
        /// Имя пользователя в Revit
        /// </summary>
        private string RevitUserName { get; set; }

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
            catch (Exception)
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
            catch (Exception)
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
            // Сохранение имени пользователя в Ревит для заполнения поля в логере
            RevitUserName = e.Document?.Application.Username;
        }

        /// <summary>
        /// Событие активации вида
        /// </summary>
        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            // Сохранение имени активного документа для использования в
            // валидации изменения площадки !текущего файла!
            ActiveDocumentTitle = e.CurrentActiveView.Document.Title;
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            try
            {
                // Выбираем текущий файл изменяемой площадки как родительский
                var parentDoc = e.GetDocument();

                // Пропускаем ивенты, не относящиеся к данному файлу
                if (parentDoc.Title != ActiveDocumentTitle)
                    return;

                var modifiedElementsId = e.GetModifiedElementIds();

                // Кэширование данных (маркеры изменения координат)
                // Данные о расположении файла
                ProjectLocation projectLocation = null;
                BasePoint basePoint = null;

                // Площадка с общими координатами
                SiteLocation siteLocation = null;

                // Возможный провайдер координат
                Instance positionProvider = null;

                foreach (var elementId in modifiedElementsId)
                {
                    var element = parentDoc.GetElement(elementId);

                    // Проверяем категорию изменённого элемента на принадлежность к маркерам изменения общих координат
                    switch (element)
                    {
                        case ProjectLocation loc:
                            projectLocation = loc.Document.ActiveProjectLocation;
                            break;
                        case BasePoint loc:
                            basePoint = loc;
                            break;
                        case Instance link:
                            positionProvider = link;
                            break;
                        case SiteLocation site:
                            siteLocation = site;
                            break;
                    }
                }

                // ИВЕНТ ПРИНЯТИЯ КООРДИНАТ ИЗ СВЯЗИ
                if (projectLocation != null &&
                    siteLocation != null &&
                    positionProvider != null)
                {
                    var providerFileName = string.Empty;
                    var providerFileFullName = string.Empty;

                    switch (positionProvider)
                    {
                        case RevitLinkInstance rvtPositionProvider:

                            // возможно будет производительнее, если не зайдействовать документ связанного файла
                            var linkDoc = rvtPositionProvider.GetLinkDocument();
                            providerFileName = linkDoc.Title;
                            providerFileFullName = linkDoc.GetDocFullPath();
                            break;

                        case ImportInstance dwgPositionProvider:
                            if (parentDoc.GetElement(dwgPositionProvider.GetTypeId()) is CADLinkType dwgTypeProvider)
                            {
                                providerFileName = dwgTypeProvider.Name;
                                var dwgRef = dwgTypeProvider.GetExternalFileReference();
                                providerFileFullName =
                                    ModelPathUtils.ConvertModelPathToUserVisiblePath(dwgRef.GetAbsolutePath());
                            }

                            break;
                    }
                    
                    var log = new LocationLogDto()
                    {
                        SiteName = projectLocation.Name,
                        ParentFileName = ActiveDocumentTitle,
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = providerFileName,
                        ProviderFileFullName = providerFileFullName,
                        LocationBase = basePoint?.ToXyzStr(),
                        ChangedDate = DateTime.Now,
                        Author = RevitUserName,
                        UserName = Environment.UserName,
                        Success = true
                    };
                    _logger.WriteLog(log);
                }

                // ИВЕНТ ИЗМЕНЕНИЯ КООРДИНАТ В ПРОЕКТЕ
                else if (projectLocation != null && siteLocation == null)
                {
                    var log = new LocationLogDto()
                    {
                        SiteName = projectLocation.Name,
                        ParentFileName = ActiveDocumentTitle,
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = null,
                        ProviderFileFullName = null,
                        LocationBase = basePoint?.ToXyzStr(),
                        ChangedDate = DateTime.Now,
                        Author = RevitUserName,
                        UserName = Environment.UserName,
                        Success = true
                    };

                    // Запись в БД
                    _logger.WriteLog(log);
                }
            }
            catch (Exception)
            {
                // Пропускаем
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