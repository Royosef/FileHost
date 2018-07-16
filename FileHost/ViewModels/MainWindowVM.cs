using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FileHost.FilesManagement;
using FileHost.Infra;
using FileHost.Models;
using FileHost.Views;
using Microsoft.Win32;

namespace FileHost.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        private bool _isEmpty;
        private bool _isMainFolder;
        private string _folderName;
        private FolderItem _currentFolder;

        private FileUploader FileUploader { get; } = new FileUploader();
        private FolderCreator FolderCreator { get; } = new FolderCreator();
        private ItemsLoader ItemsLoader { get; } = new ItemsLoader();
        private ItemsDeleter ItemsDeleter { get; } = new ItemsDeleter();
        private ItemsDownloader ItemsDownloader { get; } = new ItemsDownloader();

        private FolderItem CurrentFolder
        {
            get => _currentFolder;
            set
            {
                _currentFolder = value;
                FolderName = _currentFolder?.Name;
                IsMainFolder = value == null;
            }
        }

        public bool IsMainFolder
        {
            get => _isMainFolder;
            set
            {
                if (_isMainFolder == value) return;
                _isMainFolder = value;
                OnPropertyChanged();
            }
        }

        public string FolderName
        {
            get => _folderName;
            set
            {
                if (_folderName == value) return;
                _folderName = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ItemPreviewVM> Items { get; } = new ObservableCollection<ItemPreviewVM>();

        public DelegateCommand UploadFileCommand { get; }
        public DelegateCommand CreateFolderCommand { get; }
        public DelegateCommand SyncCommand { get; }
        public DelegateCommand DeleteItemCommand { get; }
        public DelegateCommand DeleteSelectedCommand { get; }
        public DelegateCommand DownloadItemCommand { get; }
        public DelegateCommand DownloadSelectedCommand { get; }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if(value == _isEmpty) return;
                _isEmpty = value;
                OnPropertyChanged();
            }
        }

        public MainWindowVM()
        {
            CurrentFolder = null;

            UploadFileCommand = new DelegateCommand(Upload);
            CreateFolderCommand = new DelegateCommand(CreateFolder);
            SyncCommand = new DelegateCommand(Sync);
            DeleteItemCommand = new DelegateCommand(async obj => await DeleteItem(obj));
            DeleteSelectedCommand = new DelegateCommand(DeleteSelected);
            DownloadItemCommand = new DelegateCommand(DownloadItem);
            DownloadSelectedCommand = new DelegateCommand(DownloadSelected);
        }

        private async void DownloadSelected()
        {
            var zipName = GetSavedFileName();

            if (zipName == string.Empty) return;

            var selectedItems = Items.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            await ItemsDownloader.DownloadItems(selectedItems, zipName);

            foreach (var itemPreviewVM in Items.Where(x => x.IsSelected))
            {
                itemPreviewVM.IsSelected = false;
            }
        }

        private async void DownloadItem(object itemObj)
        {
            switch (itemObj)
            {
                case FilePreviewVM filePreviewVM:
                    var fileName = GetSavedFileName(filePreviewVM.Item.Name, false);
                    if(fileName == string.Empty) return;
                    await ItemsDownloader.DownloadFile((FileItem)filePreviewVM.Item, fileName);
                    break;
                case FolderPreviewVM folderPreviewVM:
                    var zipName = GetSavedFileName(folderPreviewVM.Item.Name);
                    if (zipName == string.Empty) return;
                    await ItemsDownloader.DownloadFolder((FolderItem)folderPreviewVM.Item, zipName);
                    break;
            }
        }

        private string GetSavedFileName(string name = null, bool isZip = true)
        {
            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".zip",
                Filter = "Zip Files|*.zip",
                FileName = name ?? string.Empty
            };

            if (!isZip && name != null)
            {
                var ext = Path.GetExtension(name);

                saveFileDialog.DefaultExt = $".{ext}";
                saveFileDialog.Filter = $"{ext.ToUpperInvariant()} Files|*{ext}";
            }

            var result = saveFileDialog.ShowDialog();

            if (result ?? false)
            {
                return saveFileDialog.FileName;
            }

            return string.Empty;
        }

        private async void DeleteSelected()
        {
            var selectedItems = Items.Where(x => x.IsSelected).ToList();

            foreach (var item in selectedItems)
            {
                await DeleteItem(item);
            }
        }

        private async Task DeleteItem(object itemObj)
        {
            ItemPreviewVM itemVM = null;

            switch (itemObj)
            {
                case FilePreviewVM filePreviewVM:
                    await ItemsDeleter.DeleteItem(filePreviewVM.Item);
                    itemVM = filePreviewVM;
                    break;
                case FolderPreviewVM folderPreviewVM:
                    await ItemsDeleter.DeleteFolder((FolderItem)folderPreviewVM.Item);
                    itemVM = folderPreviewVM;
                    break;
            }

            Items.Remove(itemVM);
            UpdateIsEmpty();
        }

        private async void Sync(object obj)
        {
            UpdateCurrentFolder(obj);

            Items.Clear();

			await LoadItems();
			UpdateIsEmpty();
		}

        private void UpdateCurrentFolder(object obj)
        {
            switch (obj)
            {
                case FolderItem folderItem:
                    CurrentFolder = folderItem;
                    break;
                case null:
                    CurrentFolder = null;
                    break;
            }
        }

        private void UpdateIsEmpty()
		{
			IsEmpty = !Items.Any();
		}

		private async Task LoadItems()
		{
            if (IsMainFolder)
            {
                (await ItemsLoader.GetFolders()).ForEach(x => Items.Add(new FolderPreviewVM(x)));
            }
            else
            {
                (await ItemsLoader.GetFolderFiles(CurrentFolder)).ForEach(x => Items.Add(new FilePreviewVM(x)));
            }
		}

        private async void CreateFolder()
        {
            var folderName = GetFolderName();
            if(folderName == string.Empty) return;

            try
            {
                var folderItem = await FolderCreator.CreateFolder(folderName);

                Items.Add(new FolderPreviewVM(folderItem));

				UpdateIsEmpty();

			}
            catch (Exception)
            {
                MessageBox.Show($"Error: Some error occurred, could not create folder.\nFolder: {folderName}");
            }
        }

        private string GetFolderName()
        {
            var existingFoldersNames = GetExistingFoldersNames();
            bool isNameSelected;

            do
            {
                var dialog = new FolderNameWindow();
                var dialogResult = dialog.ShowDialog();
                isNameSelected = dialogResult ?? false;

                if (isNameSelected)
                {
                    var name = (dialog.DataContext as FolderNameWindowVM)?.FolderName;
                    var alreadyExist = existingFoldersNames.Any(x => string.CompareOrdinal(x, name) == 0);

                    if (!alreadyExist)
                    {
                        return name;
                    }

                    MessageBox.Show("Error: Folder with the same name already exist.");
                }

            } while (isNameSelected);


            return string.Empty;
        }

        private List<string> GetExistingFoldersNames()
        {
            return Items.Where(x => x is FolderPreviewVM).Select(x => x.Item.Name).ToList();
        }

        private async void Upload()
        {
            var files = GetUploadingFiles();
            var fileItems = await FileUploader.Upload(files, CurrentFolder?.Id ?? string.Empty,
                Items.Where(x => x is FilePreviewVM).Select(x => x.Item.Name).ToList());

            if (!fileItems.Any()) return;

            fileItems.ForEach(x => Items.Add(new FilePreviewVM(x)));
            UpdateIsEmpty();
            MessageBox.Show("Upload finished.");
        }

        private List<string> GetUploadingFiles()
        {
            var fileDialog = new OpenFileDialog { Multiselect = true };
            var isFilesSelected = fileDialog.ShowDialog();

            return isFilesSelected != null && isFilesSelected == true ? fileDialog.FileNames.ToList() : new List<string>();
        }
    }
}
