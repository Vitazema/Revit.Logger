namespace RevitLogProjectLocation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.ApplicationServices;
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
        /// Язык интерфейса Ревит
        /// </summary>
        private LanguageType _language;

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
                application.DialogBoxShowing += OnDialogShowing;

                // application.ControlledApplication.FailuresProcessing += FailureProcessor.OnFailuresProcessing;
                _language = application.ControlledApplication.Language;
                _logger = new LocationLogger();
            }
            catch (Exception) {
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
                // application.ControlledApplication.FailuresProcessing -= FailureProcessor.OnFailuresProcessing;
                application.DialogBoxShowing -= OnDialogShowing;
                application.ControlledApplication.DocumentChanged -= OnDocumentChanged;
                application.ViewActivated += OnViewActivated;
            }
            catch (Exception) {
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

        private void OnDialogShowing(object sender, DialogBoxShowingEventArgs e)
        {
            if (e is TaskDialogShowingEventArgs window &&
                window.DialogId == "TaskDialog_Location_Position_Changed")
            {
                try
                {
                    // Находим имя файла в тексте сообщения запроса на сохранение площадки
                    var message = window.Message;
                    var prefix = _language == LanguageType.Russian ? "файле " : "file ";
                    int iStart = message.IndexOf(prefix, 0) + prefix.Length;
                    int iEnd = message.IndexOf(message.Contains(".rvt") ? ".rvt" : ".dwg", iStart);
                    var fileName = message.Substring(iStart, iEnd - iStart + 4);
                    
                    // Генерируем и запускаем подменное окошко
                    var td = new TaskDialog("Изменено местоположение") {
                        TitleAutoPrefix = false,
                        Id = "Custom",
                        MainContent = string.Empty,
                        MainInstruction = $"Изменено положение \"текущее\" в файле {fileName}. Выберите одну из следующих возможностей.",
                        CommonButtons = TaskDialogCommonButtons.Cancel,
                        FooterText = "<a href=\"https://docs.google.com/document/d/11SsSeIzcpLhwrB5gEscB38-YTVzQJMoguiQcgfiz2Ms\">Щёлкните здесь для получения дополнительных сведений</a>",
                        AllowCancellation = true,
                        DefaultButton = TaskDialogResult.Cancel
                    };
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Сохранить", "Сохранение нового положения в связи");
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Не сохранять", "Возврат к ранее сохраненному положению при обновлении или повтором открытии связи.");
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Отменить совместный доступ к размещению", "Сохранение текущего положения связи и удаление значения параметра \"Общее положение\"");

                    var result = td.Show();

                    switch (result) {

                        // Пользователь выбрал команду записи координат - мы её отслеживаем
                        case TaskDialogResult.CommandLink1:

                            // Находим наш документ и вычислям параметры для логировния
                            if (sender is UIApplication uiapp)
                            {
                                var uidoc = uiapp.ActiveUIDocument;
                                var doc = uidoc.Document;

                                var parentFileFullName = "Undefined";
                                var siteName = string.Empty;
                                BasePoint linkDocBasePoint = null;
                                
                                // Сценарий записи в файл .rvt
                                if (fileName.EndsWith(".rvt"))
                                {
                                    try
                                    {
                                        var rvtLinkInstances = new FilteredElementCollector(doc)
                                            .OfClass(typeof(RevitLinkInstance));
                                        foreach (var rvtLinkInstance in rvtLinkInstances)
                                        {
                                            if (rvtLinkInstance.Name.Contains(fileName.RemoveFileExtension()))
                                            {
                                                string siteFullName = rvtLinkInstance.Name;
                                                siteName = siteFullName
                                                    .Split(new string[] { " : " }, StringSplitOptions.RemoveEmptyEntries)
                                                    .LastOrDefault();

                                                siteName = siteName?.Substring(siteName.IndexOf(' ') + 1);

                                                break;
                                            }
                                        }

                                        foreach (Document linkDoc in uiapp.Application.Documents)
                                        {
                                            if (linkDoc.Title.RemoveFileExtension() == fileName.RemoveFileExtension())
                                            {
                                                linkDocBasePoint = linkDoc.GetProjectBasePoint();
                                                parentFileFullName = linkDoc.GetDocFullPath();
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        Log.Error($"Не удалось обнаружить rvt ссылку {fileName} и путь к ней в документе {doc.Title}", exception);
                                    }
                                }

                                // Сценарий записи в файл .dwg
                                else if (fileName.EndsWith(".dwg"))
                                {
                                    var dwgLinks = new FilteredElementCollector(doc)
                                        .OfClass(typeof(ImportInstance));
                                    foreach (Element element in dwgLinks)
                                    {
                                        try
                                        {
                                            ImportInstance dwgLinkInstance = doc.GetElement(element.Id) as ImportInstance;
                                            if (dwgLinkInstance == null)
                                                continue;
                                            var linkName = dwgLinkInstance.get_Parameter(BuiltInParameter.IMPORT_SYMBOL_NAME)
                                                .AsString();
                                            if (linkName == fileName)
                                            {
                                                var siteFullName = dwgLinkInstance.Name;
                                                siteName = siteFullName
                                                    .Split(new string[] { " : " }, StringSplitOptions.RemoveEmptyEntries)
                                                    .LastOrDefault();
                                                siteName = siteName?.Substring(siteName.IndexOf(' ') + 1);

                                                var dwgLinkType = doc.GetElement(dwgLinkInstance.GetTypeId());
                                                if (dwgLinkType.IsExternalFileReference())
                                                {
                                                    var exRef = dwgLinkType.GetExternalFileReference();
                                                    var path = exRef.GetAbsolutePath();
                                                    parentFileFullName = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);
                                                }

                                                break;
                                            }
                                        }
                                        catch (Exception exception)
                                        {
                                            Log.Error($"Не удалось обнаружить dwg ссылку {fileName} и путь к ней в документе {doc.Title}", exception);
                                        }
                                    }
                                }

                                // Запись в БД
                                var log = new LocationLogDto() {
                                    SiteName = siteName,
                                    ParentFileName = fileName.RemoveFileExtension(),
                                    ParentFileFullName = parentFileFullName,
                                    ProviderFileName = doc.Title.RemoveUserLocalFileMark(doc.Application.Username),
                                    ProviderFileFullName = doc.GetDocFullPath(),
                                    LocationBase = linkDocBasePoint.ToXyzStr(),
                                    ChangedDate = DateTime.Now,
                                    Author = doc.Application.Username,
                                    UserName = Environment.UserName,
                                    Success = true
                                };
                                _logger.WriteLog(log);
                            }

                            e.OverrideResult(1001);

                            break;
                        case TaskDialogResult.CommandLink2:
                            e.OverrideResult(1002);
                            break;
                        case TaskDialogResult.CommandLink3:
                            e.OverrideResult(1003);
                            break;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("Подмена окошка провалилась", exception);
                }
            }
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            try
            {
#if DEBUG
                var debugger = new List<string>();
#endif

                // Выбираем текущий файл изменяемой площадки как родительский
                var parentDoc = e.GetDocument();

                // Пропускаем ивенты, не относящиеся к данному файлу
                if (parentDoc.Title != ActiveDocumentTitle)
                    return;

                // Упрощаем выборку ивентов до касающихся координат
                if (e.GetTransactionNames().FirstOrDefault() != "Изменение исходных общих координат")
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

                foreach (var elementId in modifiedElementsId) {
                    var element = parentDoc.GetElement(elementId);

                    // Проверяем категорию изменённого элемента на принадлежность к маркерам изменения общих координат
                    switch (element) {
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
                    #if DEBUG
                    try
                    {
                        debugger.Add($"{element.Document.Title} -> {element.Name} == {element.Category.Name}");
                    }
                    catch (Exception)
                    {
                        debugger.Add($"что-то пошло не так с {element.Document.Title} -> {element.Name}");
                    }
                    #endif
                }

                // ИВЕНТ ПРИНЯТИЯ КООРДИНАТ ИЗ СВЯЗИ
                if (projectLocation != null &&
                    siteLocation != null &&
                    positionProvider != null &&
                    basePoint != null) {

                    SetProviderInfo(parentDoc, positionProvider);

                    var log = new LocationLogDto() {
                        SiteName = projectLocation.Name,
                        ParentFileName = ActiveDocumentTitle.RemoveUserLocalFileMark(parentDoc.Application.Username),
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
                        ParentFileName = ActiveDocumentTitle.RemoveUserLocalFileMark(parentDoc.Application.Username),
                        ParentFileFullName = parentDoc.GetDocFullPath(),
                        ProviderFileName = "Не участвует",
                        ProviderFileFullName = "Не участвует",
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
                           positionProvider != null) {
                    SetProviderInfo(parentDoc, positionProvider);
                    var locationPoints = new FilteredElementCollector(parentDoc)
                        .OfClass(typeof(BasePoint))
                        .ToElements();
                    foreach (var locationPoint in locationPoints) {
                        if (locationPoint is BasePoint bp && !bp.IsShared) {
                            basePoint = bp;
                        }
                    }

                    var log = new LocationLogDto() {
                        SiteName = parentDoc.ActiveProjectLocation.Name,
                        ParentFileName = ActiveDocumentTitle.RemoveUserLocalFileMark(parentDoc.Application.Username),
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
            catch (Exception exception) {

                Log.Error("Не удалось вычислить лог", exception);
            }
        }

        private void SetProviderInfo(Document parDoc, Instance posProvider)
        {
            try
            {
                switch (posProvider)
                {
                    case RevitLinkInstance rvtPositionProvider:
                        
                        // возможно будет производительнее, если не зайдействовать документ связанного файла
                        var linkDoc = rvtPositionProvider.GetLinkDocument();
                        if (linkDoc == null)
                        {
                            Log.Error("документ rvt-провайдера не получилось взять. Возможно, что выгружет или сломан либо взят невалидный файл");
                            ProviderFileName = "Exception";
                            ProviderFileFullName = "Exception";
                            break;
                        }

                        ProviderFileName = linkDoc.Title.RemoveFileExtension();
                        ProviderFileFullName = linkDoc.GetDocFullPath();
                        break;

                    case ImportInstance dwgPositionProvider:
                        if (parDoc.GetElement(dwgPositionProvider.GetTypeId()) is CADLinkType dwgTypeProvider)
                        {
                            ProviderFileName = dwgTypeProvider.Name;
                            var dwgRef = dwgTypeProvider.GetExternalFileReference();
                            ProviderFileFullName = ModelPathUtils.ConvertModelPathToUserVisiblePath(dwgRef.GetAbsolutePath());
                        }

                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Error("Не удалось вычислить путь к файлу-провайдеру", exception);
            }
        }
    }
}