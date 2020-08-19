using Maxci.Runner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Maxci.Runner.ViewModels
{
    /// <summary>
    /// ViewModel for RunnerView
    /// </summary>
    internal class RunnerViewModel : INotifyPropertyChanged
    {
        private string _infoStep;
        private bool _progressVisible;
        private int _percentLoading;
        private int _changedItemsCount;
        private string _folderSource;
        private string _folderTarget;
        private string _executeFile;


        #region PROPERTIES

        /// <summary>
        /// Information message about current step 
        /// </summary>
        public string InfoStep
        {
            get { return _infoStep; }
            set
            {
                if (_infoStep != value)
                {
                    _infoStep = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// ProgressBar Visiblity
        /// </summary>
        public bool ProgressBarVisible
        {
            get { return _progressVisible; }
            set
            {
                if (_progressVisible != value)
                {
                    _progressVisible = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Percent Loading
        /// </summary>
        public int PercentLoading
        {
            get { return _percentLoading; }
            set
            {
                if (_percentLoading != value)
                {
                    _percentLoading = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Changed items count
        /// </summary>
        public int ChangedItemsCount
        {
            get { return _changedItemsCount; }
            set
            {
                if (_changedItemsCount != value)
                {
                    _changedItemsCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion
        
        
        /// <summary>
        /// Constructor
        /// </summary>
        public RunnerViewModel()
        {
            var appSettings = ConfigurationManager.AppSettings;

            _folderSource = appSettings["FolderSource"];
            _folderTarget = appSettings["FolderTarget"];
            _executeFile = appSettings["ExecuteFile"];

            App.Log.Info("Config: FolderSource = '{0}', FolderTarget='{1}', ExecuteFile='{2}'"
                , _folderSource ?? "null", _folderTarget ?? "null", _executeFile ?? "null");

            if (String.IsNullOrWhiteSpace(_folderTarget))
                _folderTarget = Environment.CurrentDirectory;

            if (_folderSource != null)
                _folderSource = _folderSource.TrimEnd('\\');

            if (_folderTarget != null)
                _folderTarget = _folderTarget.TrimEnd('\\');

            if (!String.IsNullOrWhiteSpace(_executeFile))
                _executeFile = String.Format("{0}\\{1}", _folderTarget, _executeFile);

            App.Log.Info("Current params: FolderSource = '{0}', FolderTarget='{1}', ExecuteFile='{2}'"
                , _folderSource ?? "null", _folderTarget ?? "null", _executeFile ?? "null");

            RunnerAsync();
        }


        /// <summary>
        /// Async method for runner 
        /// </summary>
        private async void RunnerAsync()
        {
            try
            {
                var updates = await CheckUpdatesAsync();

                if (updates.Length > 0)
                    await UpdateApplicationAsync(updates);
            }
            catch (Exception ex)
            {
                App.Log.Error(ex, "Update error");
            }

            LaunchingAppication();

            App.Log.Info("Finish program");
            App.Current.MainWindow.Close();
        }

        /// <summary>
        /// Check for file/folder updates
        /// </summary>
        /// <returns>List of changes</returns>
        private async Task<ChangedItem[]> CheckUpdatesAsync()
        {
            if (Directory.Exists(_folderSource))
            {
                App.Log.Info("Start check updates");
                InfoStep = "Проверка обновлений...";

                var itemsChanged = await Task.Factory.StartNew(() =>
                {
                    if (!Directory.Exists(_folderSource))
                        throw new Exception("Папка с обновлениями не найдена!");

                    if (!Directory.Exists(_folderTarget))
                        Directory.CreateDirectory(_folderTarget);

                    var items = new List<ChangedItem>();

                    DiffDirectory(items);
                    DiffFiles(items);

                    return items.ToArray();
                });

                App.Log.Info("Items Changes count = {0}:", itemsChanged.Length.ToString());

                foreach (var item in itemsChanged)
                {
                    App.Log.Info("- {0} {1}", item.TypeOfChanges.ToString(), item.Path);
                }

                App.Log.Info("Finish check updates");

                return itemsChanged;
            }
            else
                return new ChangedItem[0];
        }

        /// <summary>
        /// Updating application
        /// </summary>
        /// <param name="updates">List of changes</param>
        /// <returns></returns>
        private Task UpdateApplicationAsync(ChangedItem[] updates)
        {
            return Task.Factory.StartNew(() =>
            {
                var tempFolder = String.Format("{0}MaxciRunner_{1}", Path.GetTempPath(), Guid.NewGuid().ToString());

                try
                {
                    Directory.CreateDirectory(tempFolder);
                    App.Log.Info("Create temp folder: {0}", tempFolder);

                    DownloadUpdates(updates, tempFolder);
                    InstallingUpdates(updates, tempFolder);
                }
                finally
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                        App.Log.Info("Delete temp folder: {0}", tempFolder);
                    }
                }
            });
        }

        /// <summary>
        /// Launching the application
        /// </summary>
        private void LaunchingAppication()
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(_executeFile))
                {
                    App.Log.Info("Start openning file");

                    if (File.Exists(_executeFile))
                    {
                        InfoStep = "Запуск приложения...";
                        System.Diagnostics.Process.Start(_executeFile);
                    }
                    else
                        App.Log.Warn("File not found: {0}", _executeFile);

                    App.Log.Info("Finish openning file");
                }
            }
            catch (Exception ex)
            {
                App.Log.Error(ex, "Openning file error");
            }
        }

        /// <summary>
        /// Difference in the folder structure
        /// </summary>
        /// <param name="items">Reference to thi list of changed files/folders</param>
        private void DiffDirectory(List<ChangedItem> items)
        {
            var directoriesTarget = new HashSet<string>();
            var lenNameSource = _folderSource.Length;
            var lenNameTarget = _folderTarget.Length;

            foreach (var directory in Directory.EnumerateDirectories(_folderTarget, "*", SearchOption.AllDirectories))
            {
                directoriesTarget.Add(directory.Substring(lenNameTarget));
            }

            foreach (var directorySource in Directory.EnumerateDirectories(_folderSource, "*", SearchOption.AllDirectories))
            {
                var directory = directorySource.Substring(lenNameSource);

                if (directoriesTarget.Contains(directory))
                    directoriesTarget.Remove(directory);
                else
                    items.Add(new ChangedItem(directory, TypesOfChanges.NewFolder));
            }

            foreach (var directory in directoriesTarget)
            {
                items.Add(new ChangedItem(directory, TypesOfChanges.DeleteFolder));
            }
        }

        /// <summary>
        /// Difference in files
        /// </summary>
        /// <param name="items">Reference to thi list of changed files/folders</param>
        private void DiffFiles(List<ChangedItem> items)
        {
            var filesTarget = new Dictionary<string, string>();
            var lenNameSource = _folderSource.Length;
            var lenNameTarget = _folderTarget.Length;

            foreach (var file in Directory.EnumerateFiles(_folderTarget, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".md5")))
            {
                filesTarget.Add(file.Substring(lenNameTarget), GetMD5Hash(file));
            }

            foreach (var fileSource in Directory.EnumerateFiles(_folderSource, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".md5")))
            {
                var file = fileSource.Substring(lenNameSource);

                if (filesTarget.ContainsKey(file))
                {
                    if (filesTarget[file] != GetMD5Hash(fileSource))
                        items.Add(new ChangedItem(file, TypesOfChanges.UpdateFile));

                    filesTarget.Remove(file);
                }
                else
                    items.Add(new ChangedItem(file, TypesOfChanges.NewFile));
            }

            foreach (var file in filesTarget.Keys)
            {
                items.Add(new ChangedItem(file, TypesOfChanges.DeleteFile));
            }
        }

        /// <summary>
        /// Get md5 for file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>MD5 hash</returns>
        private static string GetMD5Hash(string fileName)
        {
            if (File.Exists(fileName + ".md5"))
            {
                var md5 = File.ReadAllText(fileName + ".md5");

                if (md5.IndexOf(" ") > 0)
                    return md5.Substring(0, md5.IndexOf(" ")).ToLower();
                else
                    return md5.ToLower();
            }
            else
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        /// <summary>
        /// Download updates
        /// </summary>
        /// <param name="updates">List of changes</param>
        /// <param name="tempFolder">Path to temp folder</param>
        private void DownloadUpdates(ChangedItem[] updates, string tempFolder)
        {
            App.Log.Info("Start loading files");

            InfoStep = "Загрузка обновлений...";
            PercentLoading = 0;
            ChangedItemsCount = updates.Length;
            ProgressBarVisible = true;

            foreach (var item in updates)
            {
                var typeOfChanges = item.TypeOfChanges;

                if (typeOfChanges == TypesOfChanges.NewFile || typeOfChanges == TypesOfChanges.UpdateFile)
                    LoadFile(item.Path, tempFolder);

                PercentLoading++;
            }

            ProgressBarVisible = false;

            App.Log.Info("Finish loading files");
        }

        /// <summary>
        /// Loading file from update folder to temp folder
        /// </summary>
        /// <param name="fileName">Relative path to file</param>
        /// <param name="tempFolder">Path to temp folder</param>
        private void LoadFile(string fileName, string tempFolder)
        {
            var fileSource = _folderSource + fileName;


            if (File.Exists(fileSource))
            {
                var fileTemp = tempFolder + fileName;
                var directoryTemp = Path.GetDirectoryName(fileTemp);

                if (!Directory.Exists(directoryTemp))
                    Directory.CreateDirectory(directoryTemp);

                File.Copy(fileSource, fileTemp);
                App.Log.Info("- file downloaded: {0}", fileSource);
            }
            else
                App.Log.Warn("File not found for download: {0}{1}", fileSource);
        }

        /// <summary>
        /// Installing updates
        /// </summary>
        /// <param name="updates">List of changes</param>
        /// <param name="tempFolder">Path to temp folder</param>
        private void InstallingUpdates(ChangedItem[] updates, string tempFolder)
        {
            App.Log.Info("Start installing updates");

            InfoStep = "Установка обновлений...";
            PercentLoading = 0;
            ChangedItemsCount = updates.Length;
            ProgressBarVisible = true;

            var foldersAdded = new List<string>();
            var itemsDeleted = new List<ChangedItem>();

            foreach (var item in updates)
            {
                switch (item.TypeOfChanges)
                {
                    case TypesOfChanges.DeleteFile:
                    case TypesOfChanges.DeleteFolder:
                        itemsDeleted.Add(item);
                        break;

                    case TypesOfChanges.NewFolder:
                        foldersAdded.Add(item.Path);
                        break;
                }
            }

            foreach (var folder in foldersAdded)
            {
                CreateFolder(_folderTarget + folder);
                PercentLoading++;
            }

            foreach (var item in updates)
            {
                var typeOfChanges = item.TypeOfChanges;
                var file = item.Path;

                if (typeOfChanges == TypesOfChanges.NewFile || typeOfChanges == TypesOfChanges.UpdateFile)
                {
                    MoveFileFromTempFolder(tempFolder + file, _folderTarget + file);
                    PercentLoading++;
                }
            }

            foreach (var item in itemsDeleted)
            {
                switch (item.TypeOfChanges)
                {
                    case TypesOfChanges.DeleteFile:
                        DeleteFile(_folderTarget + item.Path);
                        break;

                    case TypesOfChanges.DeleteFolder:
                        DeleteFolder(_folderTarget + item.Path);
                        break;
                }

                PercentLoading++;
            }

            App.Log.Info("Finish installing updates");
        }

        /// <summary>
        /// Create folder
        /// </summary>
        /// <param name="folder">Path to folder</param>
        private void CreateFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                App.Log.Info("- create folder: {0}", folder);
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="file">Path to file</param>
        private void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
                App.Log.Info("- delete file: {0}", file);
            }
        }

        /// <summary>
        /// Delete folder
        /// </summary>
        /// <param name="folder">Path to folder</param>
        private void DeleteFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
                App.Log.Info("- delete folder: {0}", folder);
            }
        }

        /// <summary>
        /// Move files from temp folder to target folder
        /// </summary>
        /// <param name="fileSource">Path to file in temp folder</param>
        /// <param name="fileTarget">Path to file in target folder</param>
        /// <returns></returns>
        private void MoveFileFromTempFolder(string fileSource, string fileTarget)
        {
            if (File.Exists(fileSource))
            {
                if (File.Exists(fileTarget))
                    File.Copy(fileSource, fileTarget, true);
                else
                    File.Move(fileSource, fileTarget);

                var fileMD5 = fileTarget + ".md5";

                if (File.Exists(fileMD5))
                    File.Delete(fileMD5);

                var hash = GetMD5Hash(fileTarget);

                File.WriteAllText(fileMD5, hash);

                App.Log.Info(" - file copied: [{0}] {1}]", hash, fileTarget);
            }
            else
                App.Log.Warn("File not found for move: {0}", fileSource);
        }


        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <param name="property">Name property</param>
        private void RaisePropertyChanged([CallerMemberName] string property = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #endregion

    }
}
