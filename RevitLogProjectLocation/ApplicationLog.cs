namespace RevitLogProjectLocation
{
    using System;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;
    using Revit_Lib.Loger;
    using RevitLogSdk.Dto;

    /// <summary>
    /// Application
    /// </summary>
    public class ApplicationLog : IExternalApplication
    {
        private LocationLogger _logger;

        /// <summary>
        /// Имя активного документа
        /// </summary>
        private string ActiveDocumentTitle { get; set; }

        /// <summary>
        /// Имя файла без расширения из которого принимаются координаты
        /// </summary>
        private string ProviderFileName { get; set; }

        /// <summary>
        /// Полное имя файла с расширением из которого принимаются координаты
        /// </summary>
        private string ProviderFileFullName { get; set; }

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
                application.ViewActivated += OnViewActivated;
                application.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(AppDialogShowing);
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
                application.ViewActivated += OnViewActivated;
            }
            catch (Exception)
            {
                return Result.Failed;
            }

            return Result.Succeeded;
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
                // Функция установки путей к файлу из которого принимаются координаты
                void SetProviderInfo(Document parDoc, Instance posProvider)
                {
                    switch (posProvider)
                    {
                        case RevitLinkInstance rvtPositionProvider:

                            // возможно будет производительнее, если не зайдействовать документ связанного файла
                            var linkDoc = rvtPositionProvider.GetLinkDocument();
                            ProviderFileName = System.IO.Path.ChangeExtension(linkDoc.Title, null);
                            ProviderFileFullName = linkDoc.GetDocFullPath();
                            break;

                        case ImportInstance dwgPositionProvider:
                            if (parDoc.GetElement(dwgPositionProvider.GetTypeId()) is CADLinkType dwgTypeProvider)
                            {
                                ProviderFileName = dwgTypeProvider.Name;
                                var dwgRef = dwgTypeProvider.GetExternalFileReference();
                                ProviderFileFullName =
                                    ModelPathUtils.ConvertModelPathToUserVisiblePath(dwgRef.GetAbsolutePath());
                            }

                            break;
                    }
                }

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
                    positionProvider != null &&
                    basePoint != null)
                {
                    SetProviderInfo(parentDoc, positionProvider);
                    var log = new LocationLogDto()
                    {
                        SiteName = projectLocation.Name,
                        ParentFileName = ActiveDocumentTitle.RemoveUserFileMark(parentDoc.Application.Username),
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = ProviderFileName,
                        ProviderFileFullName = ProviderFileFullName,
                        LocationBase = basePoint.ToXyzStr(),
                        ChangedDate = DateTime.Now,
                        Author = parentDoc.Application.Username,
                        UserName = Environment.UserName,
                        Success = true
                    };

                    _logger.WriteLog(log);
                }

                // ИВЕНТ ИЗМЕНЕНИЯ КООРДИНАТ В ПРОЕКТЕ
                else if (projectLocation != null &&
                         siteLocation == null &&
                         basePoint != null)
                {
                    var log = new LocationLogDto()
                    {
                        SiteName = projectLocation.Name,
                        ParentFileName = ActiveDocumentTitle.RemoveUserFileMark(parentDoc.Application.Username),
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = null,
                        ProviderFileFullName = null,
                        LocationBase = basePoint.ToXyzStr(),
                        ChangedDate = DateTime.Now,
                        Author = parentDoc.Application.Username,
                        UserName = Environment.UserName,
                        Success = true
                    };

                    // Запись в БД
                    _logger.WriteLog(log);
                }
                else if (siteLocation != null &&
                         positionProvider != null)
                {
                    SetProviderInfo(parentDoc, positionProvider);
                    var locationPoints = new FilteredElementCollector(parentDoc)
                        .OfClass(typeof(BasePoint))
                        .ToElements();
                    foreach (var locationPoint in locationPoints)
                    {
                        if (locationPoint is BasePoint bp && !bp.IsShared)
                        {
                            basePoint = bp;
                        }
                    }

                    var log = new LocationLogDto()
                    {
                        SiteName = parentDoc.ActiveProjectLocation.Name,
                        ParentFileName = ActiveDocumentTitle.RemoveUserFileMark(parentDoc.Application.Username),
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = ProviderFileName,
                        ProviderFileFullName = ProviderFileFullName,
                        LocationBase = basePoint.ToXyzStr(),
                        ChangedDate = DateTime.Now,
                        Author = parentDoc.Application.Username,
                        UserName = Environment.UserName,
                        Success = true
                    };

                    // Запись в БД
                    _logger.WriteLog(log);
                }
            }
            catch (Exception exception)
            {
                Log.Error("Ошибка выполнения команды", exception);
            }
        }

        private void AppDialogShowing(object sender, DialogBoxShowingEventArgs e)
        {
            if (e is TaskDialogShowingEventArgs window)
            {
                string dialogId = e.DialogId;
                if (dialogId.Contains("Customer"))
                    return;
                string promptInfo = "Сообщение в окне";
                var td = new TaskDialog("Revit");
                td.Id = "Customer DialogId";
                td.MainContent = promptInfo;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Сохранить'.");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Не'.");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Отмена");
                var buttons = TaskDialogCommonButtons.Cancel;
                td.CommonButtons = buttons;
                var result = td.Show();
                if (result == TaskDialogResult.CommandLink1)
                {
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                }
                else if (result == TaskDialogResult.CommandLink3)
                {
                }
                else if (result == TaskDialogResult.Cancel)
                {
                    // Do not show the Revit dialog
                    e.OverrideResult(1);
                }
            }
        }
    }
}